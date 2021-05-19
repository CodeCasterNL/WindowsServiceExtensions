# WindowsServiceExtensions
Make a .NET Core Windows Service that runs `IHostedService` background services power event aware.

On consumer OS Windows 10, shutting down the computer will actually hibernate the OS. Services won't get another OnStart call.

These extensions allow your IHostedServices to respond to this power state change:

* On your Host Builder, call `UsePowerEventAwareWindowsService()` instead of [`UseWindowsService()`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.windowsservicelifetimehostbuilderextensions.usewindowsservice?view=dotnet-plat-ext-3.1).
* Instead of letting your service inherit [`Microsoft.Extensions.Hosting.BackgroundService`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.backgroundservice?view=dotnet-plat-ext-5.0), inherit from `CodeCaster.WindowsServiceExtensions.PowerEventAwareBackgroundService`.
* Implement the method `public override bool OnPowerEvent(PowerBroadcastStatus powerStatus)`
