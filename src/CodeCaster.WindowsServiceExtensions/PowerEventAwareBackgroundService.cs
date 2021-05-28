using System.ServiceProcess;
using Microsoft.Extensions.Hosting;

namespace CodeCaster.WindowsServiceExtensions
{
    /// <summary>
    /// Base class for implementing a long running <see cref="IHostedService"/> as a Windows Service that can react to power state changes.
    /// </summary>
    public abstract class PowerEventAwareBackgroundService : BackgroundService, IPowerEventAwareHostedService
    {
        /// <summary>
        /// Override this method to react to power state changes.
        /// </summary>
        public virtual void OnPowerEvent(PowerBroadcastStatus powerStatus)
        {
        }
    }
}
