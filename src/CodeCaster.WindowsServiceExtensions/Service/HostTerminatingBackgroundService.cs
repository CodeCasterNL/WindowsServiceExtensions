using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// ReSharper disable MemberCanBePrivate.Global - public API
namespace CodeCaster.WindowsServiceExtensions.Service
{
    /// <summary>
    /// Terminates the host application when an exception occurs in <see cref="TryExecuteAsync"/>.
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

        /// <summary>
        /// .NET Platform Extensions 5 fallback.
        /// </summary>
        private readonly bool _needsExceptionHandling;

        /// <summary>
        /// Base class for implementing a long running <see cref="IHostedService"/> that can shut down the host application on exception.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="applicationLifetime">Used to control lifetime, available through <see cref="ApplicationLifetime"/>.</param>
        protected HostTerminatingBackgroundService(ILogger<HostTerminatingBackgroundService> logger, IHostApplicationLifetime applicationLifetime)
        {
            Logger = logger;
            ApplicationLifetime = applicationLifetime;

            // When the property exists, we're running against .NET Platform Extensions 6 which will log and handle the exception itself (unless the user opted out).
            _needsExceptionHandling = typeof(HostOptions).GetProperty("BackgroundServiceExceptionBehavior") == null;
        }

        /// <inheritdoc />
        protected sealed override async Task ExecuteAsync(CancellationToken serviceStopToken)
        {
            try
            {
                await TryExecuteAsync(serviceStopToken);
            }
            // Let it throw when .NET Platform Extensions 6.
            catch (Exception e) when (_needsExceptionHandling)
            {
                var errorString = $"Unhandled exception in {GetType().FullName}.ExecuteAsync()";

                Logger.LogError(e, errorString);

                Environment.ExitCode = -1;

                ApplicationLifetime.StopApplication();

                throw new InvalidOperationException(errorString, e);
            }
        }

        /// <summary>
        /// Calls <see cref="BackgroundService.ExecuteAsync"/> in a try-catch block, stopping the host application when it throws.
        /// </summary>
        protected abstract Task TryExecuteAsync(CancellationToken serviceStopToken);
    }
}
