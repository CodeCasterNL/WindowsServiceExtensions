using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CodeCaster.WindowsServiceExtensions;
using CodeCaster.WindowsServiceExtensions.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TestServiceThatThrows
{
    public static class Program
    {
        public static async Task Main()
        {
            // Hint: Reattach to Process (Shift+Alt+P)
            if (!Debugger.IsAttached)
            {
                Console.WriteLine("Waiting 5s for debugger to be attached and pretending the service starts up slowly...");
                Thread.Sleep(5000);
                Debugger.Break();
            }

            await new HostBuilder()
                .ConfigureLogging(l => l.AddConsole())
                .ConfigureServices((s) =>
                {
                    // Services to run are defined below.

                    // These three run successfully.
                    //s.AddHostedService<MyHappyService>();
                    //s.AddHostedService<MyHappyBackgroundService>();
                    //s.AddHostedService<QuicklyQuittingBackgroundService>();

                    // This one breaks in OnStart() (should give startup error).
                    //s.AddHostedService<MyFaultyService>();

                    // This one throws after 500ms and should take down the host (.NET PE 6+), but does not trigger an error response (exit code 0).
                    //s.AddHostedService<MyFaultyBackgroundService>();

                    // This one should stop the application with exit code -1, triggering an error.
                    s.AddHostedService<MyFaultyWindowsServiceBackgroundService>();

                    // Should give startup error.
                    //throw new InvalidOperationException("Heh");
                })
                .UseWindowsService()
                .UseWindowsServiceExtensions()
                //.UseWindowsServiceExtensions(o => 
                //{
                //    //o.ServiceName = ...
                //})
                .Build()
                .RunAsync();
        }
    }

    public class MyHappyService : IHostedService
    {
        private readonly ILogger<MyHappyService> _logger;

        public MyHappyService(ILogger<MyHappyService> logger)
        {
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting MyHappyService");

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping MyHappyService");

            return Task.CompletedTask;
        }
    }
    public class MyFaultyService : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(5000, cancellationToken);

            // This works, kills the host (awaits startup).
            throw new InvalidOperationException("This service is not supposed to start");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Should not be called");
        }
    }

    public class MyFaultyBackgroundService : BackgroundService
    {
        private readonly ILogger<MyFaultyBackgroundService> _logger;

        public MyFaultyBackgroundService(ILogger<MyFaultyBackgroundService> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Fake doing at least some work...
            await Task.Delay(500, stoppingToken);

            _logger.LogError("This service is not supposed to start, but this error won't kill the host");

            // Because it doesn't return immediately, this does not kill the host.
            throw new InvalidOperationException("This service is not supposed to start");
        }
    }

    public class MyFaultyWindowsServiceBackgroundService : WindowsServiceBackgroundService
    {
        public MyFaultyWindowsServiceBackgroundService(
            ILogger<MyFaultyWindowsServiceBackgroundService> logger,
            IHostLifetime hostLifetime,
            IHostApplicationLifetime applicationLifetime
        )
            : base(logger, hostLifetime, applicationLifetime)
        {
        }

        protected override async Task TryExecuteAsync(CancellationToken stoppingToken)
        {
            Logger.LogInformation("Sleeping, then throwing");
            
            // Fake doing at least some work...
            await Task.Delay(1000, stoppingToken);

            // This will now stop the host application.
            throw new InvalidOperationException("This service is not supposed to start");
        }
    }

    public class MyHappyBackgroundService : BackgroundService
    {
        private readonly ILogger _logger;

        public MyHappyBackgroundService(ILogger<MyHappyBackgroundService> logger)
        {
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // This will be printed _before_ "Microsoft.Hosting.Lifetime Application started".
            _logger.LogInformation("Happy Background Service started!");

            return Task.Delay(-1, stoppingToken);
        }
    }

    public class QuicklyQuittingBackgroundService : BackgroundService
    {
        private readonly ILogger _logger;

        public QuicklyQuittingBackgroundService(ILogger<QuicklyQuittingBackgroundService> logger)
        {
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // This will be printed _before_ "Microsoft.Hosting.Lifetime Application started".
            _logger.LogInformation("Quickly Quitting Background Service started!");

            _logger.LogInformation("Not surprisingly, quitting.");

            return Task.CompletedTask;
        }
    }
}
