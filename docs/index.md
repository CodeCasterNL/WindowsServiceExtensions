# WindowsServiceExtensions
This package is relevant to developers who want to write reliable background tasks running under a Windows Service.

## Installation
Through [NuGet](https://www.nuget.org/packages/CodeCaster.WindowsServiceExtensions/):

    > Install-Package CodeCaster.WindowsServiceExtensions

## Why should you use this?
A usual Windows Service program might look like this:

```C#
var hostBuilder = new HostBuilder()
    .ConfigureLogging(l => l.AddConsole())
    .ConfigureServices((s) =>
    {
        // Add our IHostedService
        s.AddHostedService<MyCoolBackgroundService>();
    })
    .UseWindowsService();

var host = hostBuilder.Build;

await host.RunAsync();
```

And then your service would look like this:

```C#
public class MyCoolBackgroundService : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Do your continuous or periodic background work.
        await SomeLongRunningTaskAsync();
    }
}
```

As long as your `ExecuteAsync()` runs, you have a _.NET_ (not Widows!) background service (`IHostedService`) running. When a hosted service throws an exception, that will stop the .NET Host that runs your application, and an event will be logged (as long as it exists and/or permisions are adequate).

## Lifetime
TODO: explain.

## Host Builder (dependency injection)
On your Host Builder, call `UsePowerEventAwareWindowsService()`:

```C#
using CodeCaster.WindowsServiceExtensions;

// ...

var hostBuilder = new HostBuilder()
    .ConfigureLogging(l => l.AddConsole())
    .ConfigureServices((s) =>
    {
        // Add our IHostedService
        s.AddHostedService<MyCoolBackgroundService>();
    })
    // instead of .UseWindowsService():    
    .UsePowerEventAwareWindowsService();
```

## Power events
If you let your service inherit `PowerEventAwareBackgroundService` instead of [`Microsoft.Extensions.Hosting.BackgroundService`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.backgroundservice?view=dotnet-plat-ext-5.0) (the former inherits the latter), you get a new method:

public class MyCoolBackgroundService : PowerEventAwareBackgroundService
{
    // This still runs your long-running background job
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Do your continuous or periodic background work.
        await SomeLongRunningTaskAsync();
    }

    // This one tells you when we're shutting down or resuming from semi-hibernation
    public override void OnPowerEvent(PowerBroadcastStatus powerStatus)
    {
        _logger.LogDebug("OnPowerEvent: {powerStatus}", powerStatus);

        if (powerStatus == PowerBroadcastStatus.Suspend)
        {
            _thingYoureRunning.Suspend();
        }

        if (powerStatus.In(PowerBroadcastStatus.ResumeSuspend, PowerBroadcastStatus.ResumeAutomatic))
        {
            _thingYoureRunning.Resume();
        }
    }
}

You might receive multiple `OnPowerEvent()` calls in succession, be sure to lock and/or debounce where appropriate.

TODO: we can do that.

Do note that the statuses received can vary. You get either `ResumeSuspend`, `ResumeAutomatic` or both, never neither, after a machine wake, reboot or boot.

## TODO
When the task returns, the host stays up. This might be a problem if you start multiple background services that should shut down the application when the last one has done its work.

## Docs demo

[See docs demo](/demo.html)
