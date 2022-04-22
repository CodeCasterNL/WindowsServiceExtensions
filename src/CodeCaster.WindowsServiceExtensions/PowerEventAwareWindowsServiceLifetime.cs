using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CodeCaster.WindowsServiceExtensions
{
    /// <summary>
    /// Basically the same as <see cref="HostApplicationStartupLifetime"/>, but states it can handle power events, and forwards those to the <see cref="IHostedService"/>s that claim to be capable of the same (<see cref="IPowerEventAwareHostedService"/>).
    /// </summary>
    public class PowerEventAwareWindowsServiceLifetime : HostApplicationStartupLifetime
    {
        private IReadOnlyCollection<IPowerEventAwareHostedService>? _hostedServices;

        /// <inheritdoc />
        public PowerEventAwareWindowsServiceLifetime(IServiceProvider serviceProvider, IHostEnvironment environment, IHostApplicationLifetime applicationLifetime, ILoggerFactory loggerFactory, IOptions<HostOptions> optionsAccessor)
            : base(serviceProvider, environment, applicationLifetime, loggerFactory, optionsAccessor)
        {
            if (OperatingSystem.IsWindows() && WindowsServiceHelpers.IsWindowsService())
            {
                CanHandlePowerEvent = true;
            }
        }

        /// <summary>
        /// Store a copy of all registered <see cref="IHostedService"/>s that implement our interface
        /// </summary>
        protected override Task ConfigureLifetimeAsync()
        {
            _hostedServices = _serviceProvider.GetService<IEnumerable<IHostedService>>()?
                                              .OfType<IPowerEventAwareHostedService>()
                                              .ToList()
                              ?? new List<IPowerEventAwareHostedService>(0);

            Logger.LogDebug(!_hostedServices.Any()
                ? $"No {nameof(IHostedService)}s were registered"
                : $"Found {_hostedServices.Count} hosted service(s): '{string.Join("', '", _hostedServices.Select(s => s.GetType().FullName))}'");

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        protected override bool OnPowerEvent(PowerBroadcastStatus powerStatus)
        {
            Logger.LogInformation($"Windows Service power event: {powerStatus}");

            // Should not happen, here to keep the code analysis happy and the intention explicit.
            if (!OperatingSystem.IsWindows() || !WindowsServiceHelpers.IsWindowsService() || _hostedServices == null)
            {
                return true;
            }

            // Forward the event to all registered I(PowerEventAware)HostedServices.
            foreach (var service in _hostedServices)
            {
                service.OnPowerEvent(powerStatus);
            }

            // Ignored anyway.
            return true;
        }
    }
}
