using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CodeCaster.WindowsServiceExtensions
{
    /// <summary>
    /// Basically the same as <see cref="WindowsServiceLifetime"/>, but responds to 
    /// </summary>
    public class PowerEventAwareWindowsServiceLifetime : WindowsServiceLifetime, IHostLifetime
    {
        private readonly CancellationTokenSource _starting = new();
        private readonly ManualResetEventSlim _started = new();

        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IHostApplicationLifetime _applicationLifetime;
        private IReadOnlyCollection<IPowerEventAwareHostedService>? _hostedServices;

        public PowerEventAwareWindowsServiceLifetime(IServiceProvider serviceProvider, IHostEnvironment environment, IHostApplicationLifetime applicationLifetime, ILoggerFactory loggerFactory, IOptions<HostOptions> optionsAccessor)
            : base(environment, applicationLifetime, loggerFactory, optionsAccessor)
        {
            _logger = loggerFactory.CreateLogger(typeof(PowerEventAwareWindowsServiceLifetime).FullName ?? nameof(PowerEventAwareWindowsServiceLifetime));
            _applicationLifetime = applicationLifetime;
            _serviceProvider = serviceProvider;

            if (OperatingSystem.IsWindows() && WindowsServiceHelpers.IsWindowsService())
            {
                CanHandlePowerEvent = true;
            }
        }

        public new async Task WaitForStartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Windows Service start requested");
            _applicationLifetime.ApplicationStarted.Register(() => _started.Set());

            try
            {
                // Store a copy of all registered IHostedServices that implement our interface
                _hostedServices = _serviceProvider.GetService<IEnumerable<IHostedService>>()?
                                                  .OfType<IPowerEventAwareHostedService>()
                                                  .ToList() 
                                  ?? new List<IPowerEventAwareHostedService>();

                _logger.LogDebug(!_hostedServices.Any()
                    ? $"No {nameof(IHostedService)}s were registered"
                    : $"Found {_hostedServices.Count} hosted service(s): '{string.Join("', '", _hostedServices.Select(s => s.GetType().FullName))}'");

                // Fail service start when an exception occurs during startup, and wait until OnStart() has been called before reporting success
                // From https://github.com/dotnet/extensions/issues/2831#issuecomment-678658133
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(_starting.Token, cancellationToken);
                await base.WaitForStartAsync(cts.Token);
            }
            catch (OperationCanceledException) when (_starting.IsCancellationRequested)
            {
                // This is expected: OnStart() has been called, meaning the application host has started.
                _logger.LogTrace("OperationCanceledException: OnStart() has canceled _starting.");
            }
        }

        protected override bool OnPowerEvent(PowerBroadcastStatus powerStatus)
        {
            _logger.LogInformation($"Windows Service power event: {powerStatus}");

            // Should not happen, here to keep the code analysis happy and the intention explicit.
            if (!OperatingSystem.IsWindows() || !WindowsServiceHelpers.IsWindowsService() || _hostedServices == null)
            {
                return true;
            }

            var acceptEvent = true;

            foreach (var service in _hostedServices)
            {
                if (!service.OnPowerEvent(powerStatus))
                {
                    // If any of the hosted services returns false, we deny the power status change request by returning false from this method (does that even work?)
                    acceptEvent = false;
                }
            }

            return acceptEvent;
        }

        protected override void OnStart(string[] args)
        {
            _logger.LogInformation("Windows Service OnStart() was called");
            
            // Flag that OnStart has been called.
            _starting.Cancel();

            // Make sure the application host actually started successfully.
            _started.Wait(_applicationLifetime.ApplicationStopping);

            if (!_applicationLifetime.ApplicationStarted.IsCancellationRequested)
            {
                // Usually very early in the startup process, so this may not be logged nor reported at all.
                const string errorString = "Windows Service failed to start";
                _logger.LogError(errorString);
                throw new Exception(errorString);
            }

            base.OnStart(args);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _starting.Dispose();
                _started.Set();
            }

            base.Dispose(disposing);
        }
    }
}
