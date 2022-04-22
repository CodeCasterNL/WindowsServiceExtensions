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
    /// 
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

            // Make sure the application host actually started successfully.
            _started.Wait(ApplicationLifetime.ApplicationStopping);

            if (!ApplicationLifetime.ApplicationStarted.IsCancellationRequested)
            {
                // Usually very early in the startup process, so this may not be logged nor reported at all.
                // But it's here to prevent the service from happily reporting successful startup, while the .NET Core ApplicationHost isn't started at all.
                const string errorString = "Windows Service failed to start";
                Logger.LogError(errorString);
                throw new Exception(errorString);
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
