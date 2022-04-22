using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// ReSharper disable MemberCanBePrivate.Global - public API
namespace CodeCaster.WindowsServiceExtensions
{
    /// <summary>
    /// Terminates the host application when an exception occurs in <see cref="ExecuteAsync"/>.
    /// </summary>
    public abstract class HostTerminatingBackgroundService : BackgroundService
    {
        /// <summary>
        /// Logs.
        /// </summary>
        protected readonly ILogger<HostTerminatingBackgroundService> Logger;

        /// <summary>
        /// To ask nicely to stop the host when cancellation is requested. We're just a BackgroundService, returning from ExecuteAsync() won't stop the host application. Better us than Windows.
        /// </summary>
        protected readonly IHostApplicationLifetime ApplicationLifetime;

        /// <inheritdoc/>
        protected HostTerminatingBackgroundService(ILogger<HostTerminatingBackgroundService> logger, IHostApplicationLifetime applicationLifetime)
        {
            Logger = logger;
            ApplicationLifetime = applicationLifetime;
        }

        /// <inheritdoc />
        protected sealed override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await TryExecuteAsync(stoppingToken);
            }
            catch (Exception e)
            {
                var errorString = $"Unhandled exception in {GetType().FullName}.ExecuteAsync()";

                Logger.LogError(e, errorString);

                // .NET 6+ terminates the host on exception, 5 doesn't. Do it ourselves.
                // https://docs.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/6.0/hosting-exception-handling
                Environment.ExitCode = -1;

                ApplicationLifetime.StopApplication();

                throw new InvalidOperationException(errorString, e);
            }
        }

        /// <summary>
        /// Calls <see cref="BackgroundService.ExecuteAsync"/> in a try-catch block, stopping the host application when it throws.
        /// </summary>
        protected abstract Task TryExecuteAsync(CancellationToken stoppingToken);
    }
}
