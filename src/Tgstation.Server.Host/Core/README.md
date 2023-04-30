# Core Services

- [Application](./Application.cs) is our main [composition root](https://freecontent.manning.com/dependency-injection-in-net-2nd-edition-understanding-the-composition-root/).
- [IRestartHandler](./IRestartHandler.cs) and [IRestartRegistration](./IRestartRegistration.cs) are a set of interface services use when they want to be aware of a TGS restart/update (i.e. This is how the watchdog know to detach instead of shutdown).
- [IServerControl](./IServerControl.cs) is an interface used to initiate a restart, shutdown, or update the server.
- [IServerPortProvider](./IServerPortProvider.cs) and [implementation](./ServerPortProvider.cs) is used by services to determine the local TGS API port. Used mainly for telling DreamDaemon where to make bridge requests.
- [IServerUpdater](./IServerUpdater.cs), [IServerUpdateExecutor](./IServerUpdateExecutor.cs) and their implementation [implementation](./ServerUpdater.cs) handles the process of downloading and unzipping update packages while respecting the Swarm protocol.
- [IServerUpdateInitiator](./IServerUpdateInitiator.cs) and [implementation](./ServerUpdateInitiator.cs) handles bridging the gap between the Controllers and the update process.
- [ServerUpdateOperation](./ServerUpdateOperation.cs) is a utility struct used by the update process.
- [ServerUpdateResult](./ServerUpdateOperation.cs) is an enumeration that indicates the result of trying to start an update operation.
