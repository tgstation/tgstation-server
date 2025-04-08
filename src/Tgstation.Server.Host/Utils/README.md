# Util classes

This is a bag of classes used throughout TGS that don't quite belong anywhere else.

- [IAsyncDelayer](./IAsyncDelayer.cs) and [implementation](./AsyncDelayer.cs) is a class used to sleep code. It's generally a no-op in test scenarios.
- [IGitHubClientFactory](./IGitHubClientFactory.cs) and [implementation](./GitHubClientFactory.cs) is a class used to create GitHub API clients using [ocktokit.net](https://github.com/octokit/octokit.net).
- [OpenApiEnumVarNamesExtension](./OpenApiEnumVarNamesExtension) implements the [x-var-names OpenAPI 3.0 extension](https://github.com/OpenAPITools/openapi-generator/blob/master/docs/templating.md#enum) in our generated API json.
- [SemaphoreSlimContext](./SemaphoreSlimContext.cs) is a helper class for working with [.NET asynchronous sempahores](https://docs.microsoft.com/en-us/dotnet/api/system.threading.semaphoreslim?view=netcore-10.0).
- [SwaggerConfiguration](./SwaggerConfiguration.cs) configures [Swashbuckle](https://github.com/domaindrivendev/Swashbuckle.AspNetCore) to generate our OpenAPI specification.
