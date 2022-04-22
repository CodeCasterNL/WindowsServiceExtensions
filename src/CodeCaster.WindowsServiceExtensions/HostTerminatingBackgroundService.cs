using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CodeCaster.WindowsServiceExtensions
{
    /// <summary>
    /// Terminates the host application when an exception occurs in <see cref="ExecuteAsync"/>.
    /// </summary>
    public abstract class HostTerminatingBackgroundService : BackgroundService
    {
        private readonly ILogger<HostTerminatingBackgroundService> _logger;
        private readonly IHostApplicationLifetime _applicationLifetime;

        /// <inheritdoc/>
        protected HostTerminatingBackgroundService(ILogger<HostTerminatingBackgroundService> logger, IHostApplicationLifetime applicationLifetime)
        {
            _logger = logger;
            _applicationLifetime = applicationLifetime;
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

                _logger.LogError(e, errorString);

                // .NET 6+ terminates the host on exception, 5 doesn't. Do it ourselves.
                // https://docs.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/6.0/hosting-exception-handling
                Environment.ExitCode = -1;

                _applicationLifetime.StopApplication();

                throw new InvalidOperationException(errorString, e);
            }
        }

        /// <summary>
        /// Calls <see cref="BackgroundService.ExecuteAsync"/> in a try-catch block, stopping the host application when it throws.
        /// </summary>
        protected abstract Task TryExecuteAsync(CancellationToken stoppingToken);
    }
}
