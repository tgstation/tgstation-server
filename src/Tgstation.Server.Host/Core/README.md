# Core Services

This is a bag of classes used throughout TGS that don't quite belong anywhere else.

- [Application](./Application.cs) is our main [composition root](https://freecontent.manning.com/dependency-injection-in-net-2nd-edition-understanding-the-composition-root/).
- [IAsyncDelayer](./IAsyncDelayer.cs) and [implementation](./AsyncDelayer.cs) is a class used to sleep code. It's generally a no-op in test scenarios.
- [IGitHubClientFactory](./IGitHubClientFactory.cs) and [implementation](./GitHubClientFactory.cs) is a class used to create GitHub API clients using [ocktokit.net](https://github.com/octokit/octokit.net).
- [IRestartHandler](./IRestartHandler.cs) and [IRestartRegistration](./IRestartRegistration.cs) are a set of interface services use when they want to be aware of a TGS restart/update (i.e. This is how the watchdog know to detach instead of shutdown).
- [IServerControl](./IServerControl.cs) is an interface used to initiate a restart or update the server.
- [IServerPortProvider](./IServerPortProvider.cs) and [implementation](./ServerPortProvider.cs) is used by services to determine the local TGS API port. Used mainly for telling DreamDaemon where to make bridge requests.
- [OpenApiEnumVarNamesExtension](./OpenApiEnumVarNamesExtension) implements the [x-var-names OpenAPI 3.0 extension](https://github.com/OpenAPITools/openapi-generator/blob/master/docs/templating.md#enum) in our generated API json.
- [SemaphoreSlimContext](./SemaphoreSlimContext.cs) is a helper class for working with [.NET asynchronous sempahores](https://docs.microsoft.com/en-us/dotnet/api/system.threading.semaphoreslim?view=netcore-6.0).
- [SwaggerConfiguration](./SwaggerConfiguration.cs) configures [Swashbuckle](https://github.com/domaindrivendev/Swashbuckle.AspNetCore) to generate our OpenAPI specification.
