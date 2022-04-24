---
title: Documentation Home
layout: cayman-with-menu
order: 1
---
# Windows Service Extensions
This package is relevant to developers who want to write reliable background tasks running under a Windows Service. The .NET BackgroundService is nice, but is abstracted away from a Windows Service, because it's not designed to be one.

Running as a _Windows Service_ and _running BackgroundServices_ are two separate things though, and they are not connected in any way. A non-service console app can run background services, and so can and do web applications. Each of those are different hosting environments, with their own lifetimes.

This project exists of a few classes that make building reliable Windows Services easier, to gap this disconnect. 

## Installation
Through [NuGet](https://www.nuget.org/packages/CodeCaster.WindowsServiceExtensions/):

    > Install-Package CodeCaster.WindowsServiceExtensions

## Why should you use this?
Using .NET's [`UseWindowsService()`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.windowsservicelifetimehostbuilderextensions.usewindowsservice?view=dotnet-plat-ext-6.0) (from the Platform Extensions package `Microsoft.Extensions.Hosting.WindowsServices`) and [`Microsoft.Extensions.Hosting.BackgroundService`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.backgroundservice?view=dotnet-plat-ext-6.0) from `Microsoft.Extensions.Hosting.Abstractions`, a usual Windows Service program hosting some long-running background tasks could look like this:

```csharp
var hostBuilder = new HostBuilder()
    .ConfigureLogging(l => l.AddConsole())
    .ConfigureServices((s) =>
    {
        // Add our IHostedService
        s.AddHostedService<MyCoolBackgroundService>();
        s.AddHostedService<MyShortRunningBackgroundService>();
    })
    .UseWindowsService();

var host = hostBuilder.Build;

await host.RunAsync();
```

And then a service would look like this:

```csharp
public class MyCoolBackgroundService : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Do your continuous or periodic background work, or just a short task and return.
        await SomeLongRunningTaskAsync();
    }
}
```

As long as your `ExecuteAsync()` runs, you have one or more _.NET_ (not Widows!) background services (`IHostedService`) running inside your executable hosting the .NET application. When the service start request immediately throws an exception (from dependency injection errors to immediate errors in `ExecuteAsync()`), that will stop the .NET Host that runs your application, and an event will be logged (as long as it exists and/or permisions are adequate).

## Exception handling
This library used to contain exception handling code in a base service, which is no longer needed for .NET Platform Extensions 6, see [Docs / .NET / .NET fundamentals / Breaking changes / Unhandled exceptions from a BackgroundService](https://docs.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/6.0/hosting-exception-handling):

> In previous versions, when a BackgroundService throws an unhandled exception, the exception is lost and the service appears unresponsive. .NET 6 fixes this behavior by logging the exception and stopping the host.

With the [retirement of .NET 5 on May 8, 2022](https://docs.microsoft.com/en-us/lifecycle/products/microsoft-net-and-net-core), this WindowsServiceExtensions library targets .NET (Platform Extensions) 6 going forward from v3.0.0.

However, in the case of a background service excption, the service doesn't report an error to the Service Control Manager, who will think the process exited nicely. This library fixes that.

## Host Builder (dependency injection)
To receive session or power events, call `UseWindowsServiceExtensions()` on your Host Builder:

```csharp
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

## Events
If you let your service inherit `CodeCaster.WindowsServiceExtensions.Service.WindowsServiceBackgroundService` (or implement `IWindowsServiceAwareHostedService`) instead of [`Microsoft.Extensions.Hosting.BackgroundService`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.backgroundservice?view=dotnet-plat-ext-5.0) (the former indirectly inherits the latter, see above), you get two new methods:

```csharp
public class MyCoolBackgroundService : WindowsServiceBackgroundService
{
    public MyCoolBackgroundService(
        ILogger<MyFaultyWindowsServiceBackgroundService> logger,
        IHostLifetime hostLifetime
    )
        : base(logger, hostLifetime)
    {
    }

    // This still runs your long-running background job
    protected override async Task TryExecuteAsync(CancellationToken stoppingToken)
    {
        // Do your continuous or periodic background work.
        await SomeLongRunningTaskAsync();

        // We're done, let the service stop.
        ServiceLifetime.ExitCode = 0;

        // This kills the process about immediately, you can also inject `IHostApplication` 
        // and call StopAsync() on that.
        await ServiceLifetime.StopAsync();
    }

    // This one tells you when we're shutting down or resuming from semi-hibernation
    protected override void OnPowerEvent(PowerBroadcastStatus powerStatus)
    {
        // The lifetime will log "OnPowerEvent: {powerStatus}"

        if (powerStatus == PowerBroadcastStatus.Suspend)
        {
            // Cancel a request, flush a cache, ...
            _thingYoureRunning.Suspend();
        }

        if (powerStatus.In(PowerBroadcastStatus.ResumeSuspend, PowerBroadcastStatus.ResumeAutomatic))
        {
            // Trigger some tokens to continue work...
            _thingYoureRunning.Resume();
        }
    }

    // React to logon/logoff/...
    protected override void OnSessionChange(SessionChangeDescription changeDescription)
    {
        // The lifetime will log "OnSessionChange: {changeDescription.SessionId}, {changeDescription.Reason}"

        if (changeDescription.Reason == SessionChangeReason.SessionLogon)
        {
            // Send a message to our notifier...
            _thingYoureRunning.TryToNotifyUserApp();
        }
    }
}
```

You might receive multiple `OnPowerEvent()`/`OnSessionChange()` calls in succession, be sure to lock and/or debounce where appropriate.

Do note that the statuses received can vary. You get either `ResumeSuspend`, `ResumeAutomatic` or both, never neither, after a machine wake, reboot or boot.
