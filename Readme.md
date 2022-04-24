# Windows Service Extensions
Building a basic Windows Service that does some long-running background work is trivial, using .NET's [`UseWindowsService()`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.windowsservicelifetimehostbuilderextensions.usewindowsservice?view=dotnet-plat-ext-6.0) (from the Platform Extensions package `Microsoft.Extensions.Hosting.WindowsServices`) and [`Microsoft.Extensions.Hosting.BackgroundService`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.backgroundservice?view=dotnet-plat-ext-6.0) from `Microsoft.Extensions.Hosting.Abstractions`. 

Running as a _Windows Service_ and _running BackgroundServices_ are two separate things though, and they are not connected in any way. A non-service console app can run background services, and so can and do web applications. Each of those are different hosting environments, with their own lifetimes.

This project exists of a few classes that make building reliable Windows Services easier, to gap this disconnect. 

## Lifetime
The following improvements are included in this library:

* `OnStart()` can fail because of invalid service configuration, quit the application when that happens (by [kmcclellan](https://github.com/dotnet/runtime/issues/50019#issuecomment-678658133)).
* On consumer OS Windows 10+, shutting down the computer will actually hibernate the OS. Services won't get another `OnStart()` call when the computer starts again, nor will your background services be notified. Now they will.
* This library makes an `IHostedService` running inside a .NET Windows Service that aware of the user logging in and out, and the computer shutting down and starting up.
* When an exception occurs during your BackgroundService's lifetime, .NET Platform Extensions < 6 didn't stop the application host. Now it does, but it doesn't report an error to the Service Control Manager. With this extension, it does, as well as setting a process exit code: 13 in both cases ("invalid data").

## Installation
Through [NuGet](https://www.nuget.org/packages/CodeCaster.WindowsServiceExtensions/):

    > Install-Package CodeCaster.WindowsServiceExtensions
    
## Documentation
For examples and more specific documentation, see https://codecasternl.github.io/WindowsServiceExtensions/.

## Upgrading from v2 to v3
If you're one of the souls that use this library (who _are_ you?), you'll want to upgrade to v3.0 after upgrading your projects to .NET 6. 

Changes:

* The DI extension method `IHostBuilder.UsePowerEventAwareWindowsService()` is now called `UseWindowsServiceExtensions()` because we do more than power events now.
* The long-running hosted service base class `CodeCaster.WindowsServiceExtensions.PowerEventAwareBackgroundService` was renamed to `CodeCaster.WindowsServiceExtensions.Service.WindowsServiceBackgroundService`, because the former didn't have enough "Service" in its name.
* Instead of `BackgroundService.ExecuteAsync()`, which is now sealed, override `WindowsServiceBackgroundService.TryExecuteAsync()` to do your long-running work.

Extended upgrading docs: see https://codecasternl.github.io/WindowsServiceExtensions/upgrading-v2-v3.

## Usage
These methods from this package allow your `IHostedService`s to respond to Windows Service events relating to sessions (user logon/logoff) and power state (shutdown/hibernate/resume):

* On your Host Builder, call `UseWindowsServiceExtensions()` instead of `UseWindowsService()`.
* Instead of letting your service inherit `BackgroundService`, inherit from `CodeCaster.WindowsServiceExtensions.WindowsServiceBackgroundService`.
* Implement the method `public override bool OnPowerEvent(PowerBroadcastStatus powerStatus) { ... }` and do your thing when it's called with a certain status.
* Implement the method `public override bool OnSessionChange(SessionChangeDescription changeDescription) { ... }` and do your thing when it's called with a certain status.

Do note that the statuses received can vary. You get either `ResumeSuspend`, `ResumeAutomatic` or both reported to `OnPowerEvent()`, never neither, after a machine wake, reboot or boot.

# Contributing
Please file an issue or PR. Even if you use this and are happy with it.

