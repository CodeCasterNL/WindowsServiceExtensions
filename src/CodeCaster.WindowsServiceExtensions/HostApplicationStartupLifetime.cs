using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// ReSharper disable MemberCanBeProtected.Global, MemberCanBePrivate.Global - public API
namespace CodeCaster.WindowsServiceExtensions
{
    /// <summary>
    /// Properly reports an error when DI of a background service fails, instead of having a service running zero hosted tasks.
    /// </summary>
    public class HostApplicationStartupLifetime : WindowsServiceLifetime, IHostLifetime
    {
        private readonly CancellationTokenSource _starting = new();
        private readonly ManualResetEventSlim _started = new();

        /// <summary>
        /// Logs things to the configured logger(s) of the application calling us.
        /// </summary>
        protected readonly ILogger Logger;

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
            Logger = loggerFactory.CreateLogger(typeof(PowerEventAwareWindowsServiceLifetime).FullName ?? nameof(PowerEventAwareWindowsServiceLifetime));
            ApplicationLifetime = applicationLifetime;
            ServiceProvider = serviceProvider;
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
            // because the DI isn't complete and the logger (file, event log, ...) not available while we're being disposed.
            if (!ApplicationLifetime.ApplicationStarted.IsCancellationRequested)
            {
                const string errorString = "Windows Service failed to start: some part reported it started, the other didn't";
                
                Logger.LogError(errorString);

                // Prevent the service from happily reporting successful startup, while the .NET Core ApplicationHost isn't started at all.
                throw new InvalidOperationException(errorString);
            }

            base.OnStart(args);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _starting.Dispose();
                _started.Set();
            }

            base.Dispose(disposing);
        }
    }
}
