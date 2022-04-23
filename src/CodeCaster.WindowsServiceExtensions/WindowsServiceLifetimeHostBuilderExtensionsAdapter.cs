using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using System;
using System.Linq;
using CodeCaster.WindowsServiceExtensions.Lifetime;

// ReSharper disable MemberCanBePrivate.Global - public API
namespace CodeCaster.WindowsServiceExtensions
{
    /// <summary>
    /// Extension method for setting up <see cref="ExtendedWindowsServiceLifetime"/>.
    /// </summary>
    public static class WindowsServiceLifetimeHostBuilderExtensionsAdapter
    {
        /// <summary>
        /// Sets the host lifetime to <see cref="ExtendedWindowsServiceLifetime"/> and does whatever <see cref="WindowsServiceLifetimeHostBuilderExtensions.UseWindowsService(IHostBuilder)"/> does.
        /// </summary>
        /// <param name="hostBuilder">The Microsoft.Extensions.Hosting.IHostBuilder to operate on.</param>
        /// <returns>The same instance of the Microsoft.Extensions.Hosting.IHostBuilder for chaining.</returns>
        /// <remarks>This is context aware and will only activate if it detects the process is running as a Windows Service.</remarks>
        public static IHostBuilder UseWindowsServiceExtensions(this IHostBuilder hostBuilder)
        {
            return UseWindowsServiceExtensions(hostBuilder, _ => { });
        }

        /// <summary>
        /// Sets the host lifetime to <see cref="ExtendedWindowsServiceLifetime"/> and does whatever <see cref="WindowsServiceLifetimeHostBuilderExtensions.UseWindowsService(IHostBuilder)"/> does.
        /// </summary>
        /// <param name="hostBuilder">The Microsoft.Extensions.Hosting.IHostBuilder to operate on.</param>
        /// <param name="configure">An action to configure the lifetime's options.</param>
        /// <returns>The same instance of the Microsoft.Extensions.Hosting.IHostBuilder for chaining.</returns>
        /// <remarks>This is context aware and will only activate if it detects the process is running as a Windows Service.</remarks>
        public static IHostBuilder UseWindowsServiceExtensions(this IHostBuilder hostBuilder, Action<WindowsServiceLifetimeOptions> configure)
        {
            if (!WindowsServiceHelpers.IsWindowsService())
            {
                return hostBuilder;
            }

            // Call MS's one
            hostBuilder.UseWindowsService(configure);

            return hostBuilder.ConfigureServices(services =>
            {
                // Replace UseWindowsService()'s IHostLifetime lifetime with our own
                var lifetime = services.FirstOrDefault(s => s.ImplementationType == typeof(WindowsServiceLifetime));
                if (lifetime == null)
                {
                    throw new InvalidOperationException($"Expecting a registration of type {typeof(WindowsServiceLifetime).FullName}, did .NET Platform Extensions (Microsoft.Extensions.Hosting.Abstractions)'s .UseWindowsService() do a breaking change?");
                }
                
                services.Remove(lifetime);
                services.AddSingleton<IHostLifetime, ExtendedWindowsServiceLifetime>();
            });
        }
    }
}
