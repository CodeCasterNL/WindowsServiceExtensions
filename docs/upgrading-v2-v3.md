---
title: Upgrading from v2 to v3
order: 2
---
# Upgrade Guide

## Dependency injection
The DI extension method `IHostBuilder.UsePowerEventAwareWindowsService()` is now called `UseWindowsServiceExtensions()` because we do more than power events now.

## BackgroundService base class
The long-running hosted service base class `CodeCaster.WindowsServiceExtensions.PowerEventAwareBackgroundService` was renamed to `CodeCaster.WindowsServiceExtensions.Service.WindowsServiceBackgroundService`, because the former didn't have enough "Service" in its name.

## Exception handling
Instead of `BackgroundService.ExecuteAsync()`, which is now sealed, override `WindowsServiceBackgroundService.TryExecuteAsync()` to do your long-running work.

