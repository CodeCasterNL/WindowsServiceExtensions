using System.ServiceProcess;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CodeCaster.WindowsServiceExtensions
{
    /// <summary>
    /// Base class for implementing a long running <see cref="IHostedService"/> as a Windows Service that can react to power state changes.
    /// </summary>
    public abstract class PowerEventAwareBackgroundService : HostTerminatingBackgroundService, IPowerEventAwareHostedService
    {
        /// <inheritdoc />
        protected PowerEventAwareBackgroundService(ILogger<HostTerminatingBackgroundService> logger, IHostApplicationLifetime applicationLifetime)
            : base(logger, applicationLifetime)
        {
        }

        /// <summary>
        /// Override this method to react to power state changes.
        /// </summary>
        public virtual void OnPowerEvent(PowerBroadcastStatus powerStatus)
        {
        }
    }
}
