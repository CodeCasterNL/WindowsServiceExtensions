using System.ServiceProcess;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// ReSharper disable MemberCanBePrivate.Global - public API
namespace CodeCaster.WindowsServiceExtensions.Service
{
    /// <summary>
    /// Base class for implementing a long running <see cref="IHostedService"/> as a Windows Service that can react to power state changes.
    /// </summary>
    public abstract class PowerEventAwareBackgroundService : BackgroundService, IPowerEventAwareHostedService
    {
        /// <summary>
        /// Logs.
        /// </summary>
        protected readonly ILogger<BackgroundService> Logger;

        /// <summary>
        /// To ask nicely to stop the host when cancellation is requested. We're just a BackgroundService, returning from ExecuteAsync() won't stop the host application. Better us than Windows.
        /// </summary>
        protected readonly IHostApplicationLifetime ApplicationLifetime;

        /// <inheritdoc />
        protected PowerEventAwareBackgroundService(ILogger<BackgroundService> logger, IHostApplicationLifetime applicationLifetime)
        {
            Logger = logger;
            ApplicationLifetime = applicationLifetime;
        }

        /// <summary>
        /// Override this method to react to power state changes.
        /// </summary>
        public virtual void OnPowerEvent(PowerBroadcastStatus powerStatus)
        {
        }
    }
}
