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

        /// <summary>
        /// To interact with the Service Control Manager.
        /// </summary>
        protected readonly ExtendedWindowsServiceLifetime? ServiceLifetime;

        /// <summary>
        /// To ask nicely to stop the host when cancellation is requested. We're just a BackgroundService, returning from ExecuteAsync() won't stop the host application. Better us than Windows.
        /// </summary>
        protected readonly IHostApplicationLifetime ApplicationLifetime;

        /// <inheritdoc />
        protected WindowsServiceBackgroundService(
            ILogger<IHostedService> logger,
            IHostLifetime hostLifetime,
            IHostApplicationLifetime applicationLifetime
        )
        {
            Logger = logger;
            ServiceLifetime = hostLifetime as ExtendedWindowsServiceLifetime;
            ApplicationLifetime = applicationLifetime;
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
            catch (Exception)
            {
                Logger.LogDebug("Setting process exit code to {exitCode}", ErrorInvalidData);

                // The host will shut down with code 0 if we don't do this.
                Environment.ExitCode = ErrorInvalidData;

#pragma warning disable CA1416 // Validate platform compatibility - we are a Windows Service.
                if (ServiceLifetime != null)
                {
                    Logger.LogDebug("Setting service exit code to {exitCode}", ErrorInvalidData);

                    // To report to the Service Control Manager on failure, is uint so > 0.
                    ServiceLifetime.ExitCode = ErrorInvalidData;
                }
#pragma warning restore CA1416 // Validate platform compatibility

                // Let the BackgroundService handle the exception.
                throw;
            }
        }

        /// <summary>
        /// Implement this method instead of <see cref="ExecuteAsync"/> to do your work.
        /// </summary>
        protected abstract Task TryExecuteAsync(CancellationToken stoppingToken);
    }
}
