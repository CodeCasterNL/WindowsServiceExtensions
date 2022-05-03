using System;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// ReSharper disable MemberCanBeProtected.Global, MemberCanBePrivate.Global - public API
namespace CodeCaster.WindowsServiceExtensions.Lifetime
{
    /// <summary>
    /// Properly reports an error when DI of a background service fails, instead of having a service running zero hosted tasks.
    /// </summary>
    public class HostApplicationStartupLifetime : WindowsServiceLifetime, IHostLifetime
    {
        private readonly CancellationTokenSource _starting = new();
        private readonly ManualResetEventSlim _started = new();

        /// <summary>
        /// Logs things to the configured logger(s) of the calling application, using "Microsoft.Hosting.Lifetime" as channel.
        /// </summary>
        protected readonly ILogger Logger;

        /// <summary>
        /// The host environment.
        /// </summary>
        protected readonly IHostEnvironment Environment;

        /// <summary>
        /// Manipulate the container after it's built. Use only from <see cref="ConfigureLifetimeAsync"/>.
        /// </summary>
        protected readonly IServiceProvider ServiceProvider;
        /// <summary>
        /// Application lifetime. Probably shouldn't touch.
        /// </summary>
        protected readonly IHostApplicationLifetime ApplicationLifetime;

        /// <inheritdoc/>
        public HostApplicationStartupLifetime(IServiceProvider serviceProvider, IHostEnvironment environment, IHostApplicationLifetime applicationLifetime, ILoggerFactory loggerFactory, IOptions<HostOptions> optionsAccessor)
            : base(environment, applicationLifetime, loggerFactory, optionsAccessor)
        {
            Logger = loggerFactory.CreateLogger("Microsoft.Hosting.Lifetime");

            Environment = environment;
            ServiceProvider = serviceProvider;
            ApplicationLifetime = applicationLifetime;
        }

        /// <inheritdoc />
        public new async Task WaitForStartAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("Windows Service start requested");

            ApplicationLifetime.ApplicationStarted.Register(() => _started.Set());

            try
            {
                await ConfigureLifetimeAsync();

                // Fail service start when an exception occurs during startup, and wait until OnStart() has been called before reporting success
                // From https://github.com/dotnet/extensions/issues/2831#issuecomment-678658133
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(_starting.Token, cancellationToken);

                await base.WaitForStartAsync(cts.Token);
            }
            catch (OperationCanceledException) when (_starting.IsCancellationRequested)
            {
                // This is expected: OnStart() has been called, meaning the application host has started.
                Logger.LogTrace("OperationCanceledException: OnStart() has canceled _starting.");
            }
        }

        /// <summary>
        /// Do not call ServiceBase.Stop() on error.
        /// </summary>
        public new Task StopAsync(CancellationToken cancellationToken)
        {
            Logger.LogDebug("StopAsync() was called, exit code {exitCode}", ExitCode);

            if (ExitCode == 0)
            {
                base.StopAsync(cancellationToken);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Sets the exit code and reports that back to the Service Control Manager.
        ///
        /// <see cref="ServiceBase.ExitCode"/> only gets flushed on Stop(), which we don't want to call on error.
        /// </summary>
        public new int ExitCode
        {
            get => base.ExitCode;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException("ExitCode must be 0 or greater");
                }

                Logger.LogDebug("Setting ExitCode to {exitCode}", value);

                base.ExitCode = value;

                /* So here's the thing: we cannot let StopAsync() be called when we're stopping due to an exception, because that will report the error _and_ stop the service.
                 * When a dying service calls StopAsync(), the service does not enter recovery and stays down after logging an error.
                 *
                 * So on exception, we want to set the exit code and prevent calling Stop(). This works, but now you can't set an exit code _and_ exit successfully.
                 *
                 * But ServiceBase only calls SetServiceStatus() on Stop() and Start() calls... so we need to do that ourselves. We have to use their exact parameters, and the
                 * P/Invoke libs () are all internal, so time for some reflection magic to copy the _status out of our ServiceBase to an own struct and call our own SetServiceStatus
                 * using the copied status field. Seems brittle, but probably as stable as the advapi32.
                 */

                const string statusFieldName = "_status";

                var privateStatus = typeof(ServiceBase).GetField(statusFieldName, BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(this);

                if (privateStatus == null)
                {
                    throw new InvalidOperationException($"Expecting a private field {nameof(ServiceBase)}.{statusFieldName}, did .NET do a breaking change?");
                }

                var finalStatus = new Interop.Advapi32.ServiceStatus
                {
                    win32ExitCode = value,

                    // Event Log interprets this as Win32 code anyway (when win32ExitCode = 0x0000042A, ERROR_SERVICE_SPECIFIC_ERROR)
                    //serviceSpecificExitCode = 42,
                    //https://social.msdn.microsoft.com/Forums/azure/en-US/6e675b34-82e8-4f89-b4b9-2f4ea31aa751/intended-use-of-dwservicespecificexitcode-event-viewer-treats-it-as-a-win32-error-code?forum=windowsgeneraldevelopmentissues

                    // Copy the properties from ServiceBase._status into a struct that we know.
                    serviceType = GetStatusField(privateStatus, "serviceType"),
                    checkPoint = GetStatusField(privateStatus, "checkPoint"),
                    controlsAccepted = GetStatusField(privateStatus, "controlsAccepted"),
                    waitHint = GetStatusField(privateStatus, "waitHint"),
                    currentState = GetStatusField(privateStatus, "currentState"),
                };

                Interop.Advapi32.SetServiceStatus(ServiceHandle, ref finalStatus);

                // Again, now that we've set the error status, when we don't call Stop(), this will be treated as an error and recovery will start. When you call Stop(), it will stay down.
            }
        }

        /// <summary>
        /// Override to execute startup code, no need to call base. Async because who knows.
        /// </summary>
        /// <returns></returns>
        protected virtual Task ConfigureLifetimeAsync()
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        protected override void OnStart(string[] args)
        {
            Logger.LogInformation("Windows Service OnStart() was called");

            // Flag that OnStart has been called.
            _starting.Cancel();

            // Make sure the application host actually started successfully, or throw when the host is shut down while we start.
            _started.Wait(ApplicationLifetime.ApplicationStopping);

            // This can happen very early in the startup process (even before ApplicationStopping is cancelled), so this may not be logged nor reported at all,
            // because the DI isn't complete and the logger (file, event log, ...) not available while we're being disposed. Probably corrupt config file?
            if (!ApplicationLifetime.ApplicationStarted.IsCancellationRequested)
            {
                const string errorString = "Windows Service failed to start: some part reported it started, the other didn't";

                Logger.LogError(errorString);

                // Prevent the service from happily reporting successful startup, while the .NET Core ApplicationHost isn't started at all.
                throw new InvalidOperationException(errorString);
            }

            base.OnStart(args);
        }

        private int GetStatusField(object privateStatus, string fieldName)
        {
            var field = privateStatus.GetType().GetField(fieldName);
            return (int)field!.GetValue(privateStatus)!;
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // When we get disposed, cancel whatever we were waiting for to start.
                _starting.Dispose();
                _started.Set();
            }

            base.Dispose(disposing);
        }
    }
}
