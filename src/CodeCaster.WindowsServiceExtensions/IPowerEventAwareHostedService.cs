using System.ServiceProcess;
using Microsoft.Extensions.Hosting;

namespace CodeCaster.WindowsServiceExtensions
{
    public interface IPowerEventAwareHostedService : IHostedService
    {
        bool OnPowerEvent(PowerBroadcastStatus powerStatus);
    }
}
