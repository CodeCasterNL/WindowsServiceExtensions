using System.ServiceProcess;
using Microsoft.Extensions.Hosting;

namespace CodeCaster.WindowsServiceExtensions.Service
{
    /// <summary>
    /// An <see cref="IHostedService"/> that runs as a Windows Service that can react to power state changes.
    /// </summary>
    public interface IPowerEventAwareHostedService : IHostedService
    {
        /// <summary>
        /// Called when a power event is sent by the OS.
        /// </summary>
        void OnPowerEvent(PowerBroadcastStatus powerStatus);
    }
}
