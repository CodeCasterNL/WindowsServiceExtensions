using System;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using CodeCaster.WindowsServiceExtensions.Lifetime;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// ReSharper disable MemberCanBePrivate.Global - public API
namespace CodeCaster.WindowsServiceExtensions.Service
{
    /// <summary>
    /// Base class for implementing a long running <see cref="IHostedService"/> as a Windows Service that can react to session and power state changes.
    /// </summary>
    public abstract class WindowsServiceBackgroundService : BackgroundService, IWindowsServiceAwareHostedService
    {
        /// <summary>
        /// Used for error reporting.
        /// </summary>
        private const int ErrorInvalidData = 13;

        /// <summary>
        /// Logs.
        /// </summary>
        protected readonly ILogger<IHostedService> Logger;

#pragma warning disable CS0419, CS1574 // Ambiguous reference in cref attribute
        /// <summary>
        /// To interact with the Service Control Manager. <c>null</c> when <see cref="WindowsServiceLifetimeHostBuilderExtensionsAdapter.UseWindowsServiceExtensions"/> wasn't called. 
        /// </summary>
#pragma warning restore CS0419, CS1574 // Ambiguous reference in cref attribute
        protected readonly ExtendedWindowsServiceLifetime? ServiceLifetime;

        /// <inheritdoc />
        protected WindowsServiceBackgroundService(ILogger<IHostedService> logger, IHostLifetime hostLifetime)
        {
            Logger = logger;
            ServiceLifetime = hostLifetime as ExtendedWindowsServiceLifetime;
        }

        /// <summary>
        /// Override this method to react to power state changes. No need to call base.
        /// </summary>
        public virtual void OnPowerEvent(PowerBroadcastStatus powerStatus)
        {
        }

        /// <summary>
        /// Override this method to react to user login/logout. No need to call base.
        /// </summary>
        public virtual void OnSessionChange(SessionChangeDescription changeDescription)
        {
        }

        /// <summary>
        /// Overridden and sealed from <see cref="BackgroundService.ExecuteAsync "/> to set the exit code on exception.
        /// </summary>
        protected sealed override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                Logger.LogInformation("{serviceType}.{execute}() called, calling {tryExecute}()", GetType().FullName, nameof(ExecuteAsync), nameof(TryExecuteAsync));

                await TryExecuteAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                Logger.LogInformation(ex, "Unhandled exception in {serviceType}, setting process exit code to {exitCode}:", GetType().FullName, ErrorInvalidData);

                // The .NET host will shut down with code 0 if we don't do this.
                Environment.ExitCode = ErrorInvalidData;

                if (ServiceLifetime != null)
                {
                    Logger.LogDebug("Setting service exit code to {exitCode}", ErrorInvalidData);

                    // To report to the Service Control Manager on failure, is uint so > 0.
                    // Do _not_ call ServiceBase.Stop() after this, or it'll think we exited successfully.
                    ServiceLifetime.ExitCode = ErrorInvalidData;
                }

                // Let the BackgroundService handle and log the exception.
                throw;
            }
        }

        /// <summary>
        /// Implement this method instead of <see cref="ExecuteAsync"/> to do your work.
        /// </summary>
        protected abstract Task TryExecuteAsync(CancellationToken stoppingToken);
    }
}
