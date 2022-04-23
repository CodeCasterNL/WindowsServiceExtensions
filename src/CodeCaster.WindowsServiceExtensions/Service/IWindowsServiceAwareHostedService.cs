using System.ServiceProcess;
using Microsoft.Extensions.Hosting;

namespace CodeCaster.WindowsServiceExtensions.Service
{
    /// <summary>
    /// An <see cref="IHostedService"/> that knows it runs inside a Windows Service, so it can react to power and session state changes.
    /// </summary>
    public interface IWindowsServiceAwareHostedService : IHostedService
    {
        /// <summary>
        /// Called when a power event is sent by the OS.
        /// </summary>
        void OnPowerEvent(PowerBroadcastStatus powerStatus);

        /// <summary>
        /// Called when a user logs on or off.
        /// </summary>
        void OnSessionChange(SessionChangeDescription changeDescription);
    }
}
