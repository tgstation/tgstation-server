# Service Components

Component code is where the magic and tears of TGS are made. There are six main TGS components that map to namespaces in this directory.

- [The git repository](./Repository)
- [The BYOND manager](./Byond)
- [The Compiler](./Deployment)
- [The Watchdog](./Watchdog)
- [The configuration system](./StaticFiles)

There exist two more namespaces in here that don't directly fit in these 6 components.

- [Events](./Events) deals with the TGS event system.
- [Interop](./Interop) deals with the bulk of DMAPI communication (Though it's not all contained here).
- [Session](./Session) contains the classes used for actually executing DreamDaemon, sending topic requests, receiving bridge requests, among other things.

Each of these is tied under the roof of an [IInstance](./IInstance.cs) ([implementation](./Instance.cs)).

While the database represents stored instance data, in component code an instance is online, or doesn't exist.

`IInstance`s ([implementation](./Instance.cs)) are created via the [IInstanceFactory](./IInstanceFactory.cs) ([implementation](./InstanceFactory.cs)) and are generally controlled via the [IInstanceOperations](./IInstanceOperations.cs) interface (implemented in the `InstanceManager`).

Many classes in here implement [IHostedService](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-10.0&tabs=visual-studio), `InstanceManager` being the only one that is called by the ASP.NET runtime. In the case of instances `StartAsync()` is called when an `Instance` is being brought online (from server startup or user request). The `Instance` handles calling `StartAsync()` on its various subcomponents that need it. When an `Instance` is being brought offline (from server shutdown/restart/update or user request) the same pattern is followed calling `StopAsync()`.
