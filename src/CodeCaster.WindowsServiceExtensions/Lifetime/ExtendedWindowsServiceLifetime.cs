using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;
using CodeCaster.WindowsServiceExtensions.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CodeCaster.WindowsServiceExtensions.Lifetime
{
    /// <summary>
    /// Basically the same as <see cref="HostApplicationStartupLifetime"/>, but states it can handle power events, and forwards those to the <see cref="IHostedService"/>s that claim to be capable of the same (<see cref="IWindowsServiceAwareHostedService"/>).
    /// </summary>
    public class ExtendedWindowsServiceLifetime : HostApplicationStartupLifetime
    {
        private IReadOnlyCollection<IWindowsServiceAwareHostedService>? _hostedServices;

        /// <inheritdoc />
        public ExtendedWindowsServiceLifetime(IServiceProvider serviceProvider, IHostEnvironment environment, IHostApplicationLifetime applicationLifetime, ILoggerFactory loggerFactory, IOptions<HostOptions> optionsAccessor)
            : base(serviceProvider, environment, applicationLifetime, loggerFactory, optionsAccessor)
        {
            // Should not happen, here to keep the code analysis happy and the intention explicit.
            if (!OperatingSystem.IsWindows() || !WindowsServiceHelpers.IsWindowsService())
            {
                const string methodName = nameof(WindowsServiceLifetimeHostBuilderExtensionsAdapter.UseWindowsServiceExtensions);

                throw new PlatformNotSupportedException($"Windows Service needs to run on Windows. Remove the call to {methodName}()");
            }

            CanHandlePowerEvent = true;
            CanHandleSessionChangeEvent = true;
        }

#pragma warning disable CA1416 // Validate platform compatibility - constructor handles that
        /// <summary>
        /// Store a copy of all registered <see cref="IHostedService"/>s that implement our interface
        /// </summary>
        protected override Task ConfigureLifetimeAsync()
        {
            _hostedServices = ServiceProvider.GetService<IEnumerable<IHostedService>>()?
                                              .OfType<IWindowsServiceAwareHostedService>()
                                              .ToList()
                              ?? new List<IWindowsServiceAwareHostedService>(0);

            if (!_hostedServices.Any())
            {
                Logger.LogDebug("No instances of {IHostedService} were registered", typeof(IHostedService).FullName);
            }
            else
            {
                Logger.LogDebug("Found {serviceCount} hosted service{plural}: '{serviceNames}'",
                    _hostedServices.Count,
                    _hostedServices.Count == 1 ? "" : "s",
                    string.Join("', '", _hostedServices.Select(s => s.GetType().FullName))
                );
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            Logger.LogInformation("Windows Service session change event: {sessionId}, {reason}", changeDescription.SessionId, changeDescription.Reason);

            // This can happen if someone injects us without having any services.                             
            if (_hostedServices == null)
            {
                Logger.LogDebug("No hosted services, returning");
                
                return;
            }

            // Forward the event to all registered I(PowerEventAware)HostedServices.
            foreach (var service in _hostedServices)
            {
                Logger.LogDebug("Notifying service {service} about session change event: {sessionId}, {reason}", 
                    service.GetType().FullName, 
                    changeDescription.SessionId, 
                    changeDescription.Reason.ToString()
                );
                
                service.OnSessionChange(changeDescription);
            }
        }

        /// <inheritdoc />
        protected override bool OnPowerEvent(PowerBroadcastStatus powerStatus)
        {
            Logger.LogInformation("Windows Service power event: {powerStatus}", powerStatus);

            // This can happen if someone injects us without having any services.                             
            if (_hostedServices == null)
            {
                Logger.LogDebug("No hosted services, returning");
                
                return true;
            }

            // Forward the event to all registered I(PowerEventAware)HostedServices.
            foreach (var service in _hostedServices)
            {
                Logger.LogDebug("Notifying service {service} about power event: {powerStatus}", service.GetType().FullName, powerStatus);
                
                service.OnPowerEvent(powerStatus);
            }

            // Ignored anyway.
            return true;
        }
    }
}
#pragma warning restore CA1416 // Validate platform compatibility - constructor handles that
