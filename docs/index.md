# Windows Service Extensions
This package is relevant to developers who want to write reliable background tasks running under a Windows Service. The .NET BackgroundService is nice, but is abstracted away from a Windows Service, because it's not designed to be one.

This is meant as a utility library that glues BackgroundServices and Windows Services together.

**NOTE**: these docs are currently for the develop branch. Once the according version is released, it'll be from main.

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

As long as your `ExecuteAsync()` runs, you have a _.NET_ (not Widows!) background service (`IHostedService`) running. When service start immediately throws an exception, that will stop the .NET Host that runs your application, and an event will be logged (as long as it exists and/or permisions are adequate).

## Exception handling
This library used to contain exception handling code in a base service, which is no longer needed for .NET Platform Extensions 6, see [Docs / .NET / .NET fundamentals / Breaking changes / Unhandled exceptions from a BackgroundService](https://docs.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/6.0/hosting-exception-handling):

> In previous versions, when a BackgroundService throws an unhandled exception, the exception is lost and the service appears unresponsive. .NET 6 fixes this behavior by logging the exception and stopping the host.

With the [retirement of .NET 5 on May 8, 2022](https://docs.microsoft.com/en-us/lifecycle/products/microsoft-net-and-net-core), this WindowsServiceExtensions library target .NET (Platform Extensions) 6 going forward from v3.0.0.

## Power events
If you let your service inherit `CodeCaster.WindowsServiceExtensions.Service.PowerEventAwareBackgroundService` instead of [`Microsoft.Extensions.Hosting.BackgroundService`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.backgroundservice?view=dotnet-plat-ext-5.0) (the former indirectly inherits the latter, see above), you get a new method:

```C#
public class MyCoolBackgroundService : PowerEventAwareBackgroundService
{
    // This still runs your long-running background job
    protected override Task TryExecuteAsync(CancellationToken stoppingToken)
    {
        // Do your continuous or periodic background work.
        await SomeLongRunningTaskAsync();

        // We're done, let the service stop.
        ApplicationLifetime.StopApplication();
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
```

You might receive multiple `OnPowerEvent()` calls in succession, be sure to lock and/or debounce where appropriate.

**TODO**: we can do that.

Do note that the statuses received can vary. You get either `ResumeSuspend`, `ResumeAutomatic` or both, never neither, after a machine wake, reboot or boot.


## Host Builder (dependency injection)
To receive power events, call `UseWindowsServiceExtensions()` on your Host Builder:

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
    .UseWindowsServiceExtensions();
```


## TODO
When the task returns, the host stays up. This might be a problem if you start multiple background services that should shut down the application when the last one has done its work.

## Docs demo

[See docs demo](demo)
