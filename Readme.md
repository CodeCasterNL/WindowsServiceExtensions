﻿# WindowsServiceExtensions
Make a .NET Core Windows Service that runs `IHostedService` background services power event aware. On consumer OS Windows 10, shutting down the computer will actually hibernate the OS. Services won't get another OnStart call.

This project exists of a few classes that make building reliable Windows Services easier. The Lifetime class also include [kmcclellan's fixes](https://github.com/dotnet/runtime/issues/50019#issuecomment-678658133) that make the service throw when something fails on startup, instead of reprorting it started successfully.

## Installation
Through [NuGet](https://www.nuget.org/packages/CodeCaster.WindowsServiceExtensions/):

    > Install-Package CodeCaster.WindowsServiceExtensions

## Usage
These extensions allow your IHostedServices to respond to this power state change.:

* On your Host Builder, call `UsePowerEventAwareWindowsService()` instead of [`UseWindowsService()`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.windowsservicelifetimehostbuilderextensions.usewindowsservice?view=dotnet-plat-ext-3.1).
* Instead of letting your service inherit [`Microsoft.Extensions.Hosting.BackgroundService`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.backgroundservice?view=dotnet-plat-ext-5.0), inherit from `CodeCaster.WindowsServiceExtensions.PowerEventAwareBackgroundService`.
* Implement the method `public override bool OnPowerEvent(PowerBroadcastStatus powerStatus)` and do your thing when it's called with a certain status.

Do note that the statuses received can vary. You get either `ResumeSuspend`, `ResumeAutomatic` or both, never neither, after a machine wake, reboot or boot. 
