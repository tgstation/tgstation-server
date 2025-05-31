# Tgstation.Server.Host

This assembly is the primary TGS executable and does basically everything.

Before going forward, note that there (usually) is a one-to-one mapping of interface/implementation when it comes to classes. This is for ease of unit testing and facilitating saner dependency injection.

Server startup can be a bit complicated so here's a walkthrough

1. The `Main` method in [Program.cs](./Program.cs) is called.
1. The default [IServerFactory](./IServerFactory) instance is created via a static method in the [Application](./Core/Application.cs) class.
   - The `Application` class is what's called the composition root. A good article on this principle can be found [here](https://freecontent.manning.com/dependency-injection-in-net-2nd-edition-understanding-the-composition-root/).
1. `CreateServer()` is called on the `IServerFactory` to get the `IServer` instance.
   - The factory pattern is used throughout TGS to construct implementations where the composition root is not sufficient. `ServerFactory` is somewhat of an exception to this because it exists outside of the dependency injection umbrella.
1. Inside `CreateServer()` we run the [setup code](./Setup) if need be.
   - This is implemented as a separate [dotnet host](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host?view=aspnetcore-8.0) to the main server.
1. Still inside `CreateServer()` we configure the main [dotnet host (IHostBuilder)](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host?view=aspnetcore-8.0) using the application [Application](./Core/Application.cs) class as the [Startup class](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/startup?view=aspnetcore-8.0#the-startup-class).
1. The `IHostBuilder` is used to construct the return `Server` implementation.
1. `Run()` is called on the `IServer` instance.
1. The DI container is built using the [Application](./Core/Application.cs) class.
1. The [component services](./Components) are started.
1. The web server is started.

Here's a breakdown of things in this directory

- [.config](./.config) contains the dotnet-tools.json. At the time of writing, this is only used to set the version of the [dotnet ef tools](https://docs.microsoft.com/en-us/ef/core/miscellaneous/cli/) used to create database migrations.
- [Authority](./Authority) is code that contains the buisiness logic that bridges the HTTP APIs with component code and the database.
- [ClientApp](./ClientApp) contains scripts to build and deploy the web control panel with TGS.
- [Components](./Components) is where the bulk of the TGS implementation lives.
- [Configuration](./Configuration) contains classes that partly make up the configuration yaml files (i.e. [appsettings.yml](./appsettings.yml)).
- [Controllers](./Controllers) is where REST API code lives.
- [Core](./Core) contains [Application.cs](./Core/Application.cs) and other classes related to the overrarching server process.
- [Database](./Database) contains all database related code.
- [Extensions](./Extensions) contains helper functions implemented as C# extension methods.
- [GraphQL](./GraqhQL) is where GraphQL API code lives.
- [IO](./IO) contains classes related to interacting with the filesystem.
- [Jobs](./Jobs) contains the job manager code.
- [Models](./Models) contains ORM models used to interact with the database.
- [Properties](./Properties) contains some assembly metadata, mainly used to expose internals to the testing suite.
- [Security](./Security) contains all the security related classes.
- [Setup](./Setup) contains code to initiate and run the setup wizard.
- [Swarm](./Swarm) contains code relating to grouping multiple TGS processes on different machines in a cohesive cluster.
- [System](./System) contains various OS related functions.
- [Utils](./Utils) contains various helpers that don't belong anywhere else.

As a final note, all project configuration is of course in the [Tgstation.Server.Host.csproj](./Tgstation.Server.Host.csproj) file. This contains package references that are pulled in on build, as well as other things like global warning supressions, and specialized build scripts.
