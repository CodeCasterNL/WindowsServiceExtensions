using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using System;
using System.Linq;

namespace CodeCaster.WindowsServiceExtensions
{
    public static class WindowsServiceLifetimeHostBuilderExtensionsAdapter
    {
        public static IHostBuilder UsePowerEventAwareWindowsService(this IHostBuilder builder)
        {
            // Call MS's one
            builder.UseWindowsService();

            return builder.ConfigureServices(services =>
            {
                // Replace UseWindowsService()'s IHostLifetime lifetime with our own
                var lifetime = services.FirstOrDefault(s => s.ImplementationType == typeof(WindowsServiceLifetime));
                if (lifetime == null)
                {
                    throw new InvalidOperationException($"Expecting a registration of type {typeof(WindowsServiceLifetime).FullName}, did .NET do a breaking change?");
                }
                
                services.Remove(lifetime);
                services.AddSingleton<IHostLifetime, PowerEventAwareWindowsServiceLifetime>();
            });
        }
    }
}
