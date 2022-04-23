using System;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
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
        /// Logs.
        /// </summary>
        protected readonly ILogger<IHostedService> Logger;

        /// <summary>
        /// To ask nicely to stop the host when cancellation is requested. We're just a BackgroundService, returning from ExecuteAsync() won't stop the host application. Better us than Windows.
        /// </summary>
        protected readonly IHostApplicationLifetime ApplicationLifetime;

        /// <inheritdoc />
        protected WindowsServiceBackgroundService(ILogger<IHostedService> logger, IHostApplicationLifetime applicationLifetime)
        {
            Logger = logger;
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
        protected sealed override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                Logger.LogDebug("{serviceType}.{execute}() called, calling {tryExecute}()", GetType().FullName, nameof(ExecuteAsync), nameof(TryExecuteAsync));

                return TryExecuteAsync(stoppingToken);
            }
            catch (Exception)
            {
                // The host will shut down with code 0 if we don't do this.
                const int exitCode = -1;

                Logger.LogWarning("Setting exit code to {exitCode}", exitCode);

                Environment.ExitCode = exitCode;

                // TODO: the above does not work, returns 0 on console. Does Environment.Exit(-1) work and still log to the event log?
                Environment.Exit(-1);

                // Let the WindowsServiceLifetime handle the exception.
                throw;
            }
        }

        /// <summary>
        /// Implement this method instead of <see cref="ExecuteAsync"/> to do your work.
        /// </summary>
        protected abstract Task TryExecuteAsync(CancellationToken stoppingToken);
    }
}
