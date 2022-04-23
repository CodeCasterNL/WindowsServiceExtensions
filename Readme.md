# WindowsServiceExtensions
This project exists of a few classes that make building reliable Windows Services easier. 

Make a .NET Core Windows Service that runs as `IHostedService` aware of the computer shutting down and starting up. On consumer OS Windows 10+, shutting down the computer will actually hibernate the OS. Services won't get another `OnStart()` call when the computer starts again.

The Lifetime classes also include [kmcclellan's fixes](https://github.com/dotnet/runtime/issues/50019#issuecomment-678658133) that make the service throw when something fails on .NET Host startup, instead of reprorting it started successfully, and code to react to user session changes.

## Installation
Through [NuGet](https://www.nuget.org/packages/CodeCaster.WindowsServiceExtensions/):

    > Install-Package CodeCaster.WindowsServiceExtensions

## Usage
These methods from this package allow your IHostedServices to respond to this power state change:

* On your Host Builder, call `UseWindowsServiceExtensions()` instead of [`UseWindowsService()`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.windowsservicelifetimehostbuilderextensions.usewindowsservice?view=dotnet-plat-ext-3.1).
* Instead of letting your service inherit [`Microsoft.Extensions.Hosting.BackgroundService`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.backgroundservice?view=dotnet-plat-ext-5.0), inherit from `CodeCaster.WindowsServiceExtensions.WindowsServiceBackgroundService`.
* Implement the method `public override bool OnPowerEvent(PowerBroadcastStatus powerStatus) { ... }` and do your thing when it's called with a certain status.
* Implement the method `public override bool OnSessionChange(SessionChangeDescription changeDescription) { ... }` and do your thing when it's called with a certain status.

Do note that the statuses received can vary. You get either `ResumeSuspend`, `ResumeAutomatic` or both reported to `OnPowerEvent()`, never neither, after a machine wake, reboot or boot.

## Documentation
For examples and more specific documentation, see https://codecasternl.github.io/WindowsServiceExtensions/.
