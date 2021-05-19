using System.ServiceProcess;
using Microsoft.Extensions.Hosting;

namespace CodeCaster.WindowsServiceExtensions
{
    public abstract class PowerEventAwareBackgroundService : BackgroundService, IPowerEventAwareHostedService
    {
        /// <summary>
        /// Override this method to react to power state changes.
        /// </summary>
        /// <returns>Return false to deny the power change status request.</returns>
        public virtual bool OnPowerEvent(PowerBroadcastStatus powerStatus)
        {
            return true;
        }
    }
}
