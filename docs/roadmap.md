---
title: Roadmap
order: 3
---
# v3+
Wishlist for the next versions:

* Debounce the power events.
* Expose more relevant service methods/events to BackgroundServices, use events/CancellationTokens?
* When the last BackgroundService's `ExecuteAsync()` returns, the host stays up. This might be a problem if you start multiple background services that should shut down the application when the last one has done its work.
