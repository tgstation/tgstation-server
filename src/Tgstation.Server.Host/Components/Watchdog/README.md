# The Watchdog

The watchdog is what keeps DreamDaemon alive and well. It's responsible for starting, stopping, rebooting it, sending TGS events to the DMAPI, running health checks, and applying patches.

There are three main implementations of the interface [IWatchdog](./IWatchdog.cs) that deal with how it handles the last of those jobs.

- The [BasicWatchdog](./BasicWatchdog.cs) kills DreamDaemon when the world reboots and immediately restarts it with the newly compiled .dmb.
- The [WindowsWatchdog](./WindowsWatchdog.cs) uses a quirk of the interaction of DD with the Windows file system. A symlink to the game folder is created. From here, we launch the game. When a new .dmb is available, this symlink is deleted and then immediately recreated pointing to the new game directory. When DreamDaemon reboots it will load the new game.

[WatchdogBase](./WatchdogBase.cs) is the parent of all these implementations and contains the bulk of the monitoring code. More on that later.

`IWatchdog`s are created via the [IWatchdogFactory](./IWatchdogFactory.cs) interface. The two implementations, [WatchdogFactory](./WatchdogFactory.cs) and [WindowsWatchdogFactory](./WindowsWatchdogFactory.cs), differ in that the latter creates `WindowsWatchdog`s as opposed to `BasicWatchdog`s.

## Startup

From the perspective of `WatchdogBase`

- When an instance is onlined, `StartAsync()` is called on `WatchdogBase`. If the instance is configured to autostart the server, or if reattach information is available from the `IReattachInfoHandler` the watchdog will be launched here. Otherwise, it waits to be activated from the controller.
- We first do some sanity checks such as if the watchdog is already running and if the `IDmbFactory` has a IDmbProvider for us.
- We send out the annoucement of launch/reattach to the chat system.
- The `ActiveLaunchParameters` are made the `LastLaunchParameters` for reference.
- `InitControllers()` is called, which abstractly creates the `ISessionController`(s) for the watchdog.
  - For the `BasicWatchdog`, the controller is created normally.
  - For the `WindowsWatchdog`, we replace the `IDmbProvider` with a `WindowsSwappableDmbProvider` and setup the symlinking before deferring to the `BasicWatchdog`
  - For reattaching, all three work similarly in that they use `ISessionControllerFactory` to reattach their sessions.
- `MonitorLifetimes()` is called which is the "main loop" of the watchdog.

## Monitoring

Watchdog monitoring takes place in the `MonitorLifetimes()` functiona and is entirely event based. It responds to a set of [MonitorActivationReason](./MonitorActivationReason.cs)s and takes the apporprate action. This includes:

- When a server crashes.
- When a server calls `/world/prod/Reboot()`.
- When a new .dmb is deployed.
- When DreamDaemon settings are changed via the API.
- When it is time to make a health check.

Here's how this works.

- A [MonitorState](./MonitorState.cs) object is created
- `Task`s are created for all possible activation reasons and an asynchrounous wait is made on them.
- Each activation reason is processed in a particular order via a call to `HandleMonitorWakeup()`. This function is allowed to mutate `MonitorState`.
  - How each activation reason is handled depends on the watchdog type. For example, the `BasicWatchdog` will queue a graceful reboot when a new .dmb becomes available, wheras the `WindowsWatchdog` will immediately swap the symlink to it.
    - Some actions are universal. i.e. if the server crashes, a reboot will occur. Or the server will shutdown on reboot if a graceful stop was queued.
  - If the state's [MonitorAction](./MonitorAction.cs) changes, what happens after each activation may change

This continues until `StopMonitor` is called, which triggers the `CancellationToken` in `MonitorLifetimes()` and terminates all `ISessionController`s.

## Detaching

`WatchdogBase` maintains a `IRestartRegistration` with TGS, meaning that, when TGS is rebooted, `HandleRestart()` will be called before `StopAsync()`. This has the effect of adjusting what happens when the monitor exits. Instead of terminating the server(s) it will instead call `ISessionController.Release()` on them. This generates the `ReattachInformation` which will be saved to the database in `StopAsync()`.
