using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CodeCaster.WindowsServiceExtensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TestServiceThatThrows
{
    public class MyFaultyService : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("This service is not supposed to start.");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    public class MyFaultyBackgroundService : BackgroundService
    {
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // This works: gets logged in the event log, and prevents service startup.
            throw new InvalidOperationException("This service is not supposed to start.");
        }
    }

    public static class Program
    {
        public static async Task Main(string[] args)
        {
            Thread.Sleep(5000);
            Debugger.Break();

            await new HostBuilder()
                .ConfigureLogging(l => l.AddConsole())
                .ConfigureServices((s) =>
                {
                    //throw new InvalidOperationException("Heh");
                    //s.AddHostedService<MyFaultyService>();
                    s.AddHostedService<MyFaultyBackgroundService>();
                })
                .UseWindowsService()
                //.UseWindowsServiceExtensions()
                .Build()
                .RunAsync();
        }
    }
}
