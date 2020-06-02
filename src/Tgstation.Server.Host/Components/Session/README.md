# Session Management

"Session" refers to a single invocation of DreamDaemon. The code in here is meant for creating and managing sessions. This is different from the watchdog as it is more low level.

[ISessionController](./ISessionController.cs) ([implementation](SessionController.cs)) is the representation of DreamDaemon. It handles most of the DMAPI interop, such as DMAPI validation, and raising events for things such as the world rebooting, or the process ending. These are created by the [ISessionControllerFactory](./ISessionControllerFactory.cs) ([implementation](./SessionControllerFactory.cs)) which handles actually launching DreamDaemon and linking together all the components it needs. It also runs some sanity checks, such as if the port requested is avaiable or if the BYOND pager is running (which can cause issues). Sessions give out a [LaunchResult](./LaunchResult.cs) which indicates how long it took for DreamDaemon to become responsive or if it exited before it did.

Sessions can be detached and reattached. When a session is detached, it generates [ReattachInformation](./ReattachInformation.cs). This is stored in and the database as [DualReattachInformation](./DualReattachInformation.cs) by passing it into and out of [IReattachInfoHandler](./IReattachInfoHandler.cs) ([implementation](./ReattachInfoHandler.cs)). This data can be passed back into the `ISessionControllerFactory` to reattach the session.

Other classes:

- [ApiValidationStatus](./ApiValidationStatus.cs) is an indicator of DMAPI validation.
- [CombinedTopicResponse](./CombinedTopicResponse.cs) is a wrapper class around the external [BYOND.TopicSender] library's raw topic response (i.e. string or float + raw bytes) and our internal interop response.
- [DeadSessionController](./DeadSessionController.cs) dummy implementation of `ISessionController`.
- [RebootState](./RebootState.cs) indicates the action that should be taken when the server's world reboots. Just an indicator though, action is take elsewhere.
