# CONTRIBUTING

## Introduction

Hello and welcome to /tg/station servers's contributing page. You are here because you are curious or interested in contributing - thank you! Everyone is free to contribute to this project as long as they follow the simple guidelines and specifications below; at /tg/station, we strive to maintain code stability and maintainability, and to do that, we need all pull requests to hold up to those specifications. It's in everyone's best interests - including yours! - if the same bug doesn't have to be fixed twice because of duplicated code.

First things first, we want to make it clear how you can contribute (if you've never contributed before), as well as the kinds of powers the team has over your additions, to avoid any unpleasant surprises if your pull request is closed for a reason you didn't foresee.

## Meet the Team

**Headcoder**

The Headcoder is responsible for controlling, adding, and removing maintainers from the project. In addition to filling the role of a normal maintainer, they have sole authority on who becomes a maintainer, as well as who remains a maintainer and who does not.

**Maintainers**

Maintainers are quality control. If a proposed pull request doesn't meet the following specifications, they can request you to change it, or simply just close the pull request. Maintainers are required to give a reason for closing the pull request.

Maintainers can revert your changes if they feel they are not worth maintaining or if they did not live up to the quality specifications.

## Getting Started

/tg/station doesn't have a list of goals and features to add; we instead allow freedom for contributors to suggest and create their ideas for the server. That doesn't mean we aren't determined to squash bugs, which unfortunately pop up a lot due to the deep complexity of the game. Here are some useful starting guides, if you want to contribute or if you want to know what challenges you can tackle with zero knowledge about the game's code structure.

If you want to contribute the first thing you'll need to do is [set up Git](http://tgstation13.org/wiki/Setting_up_git) so you can download the source code.

You'll probably want to get a brief overview of the [server architecture](https://tgstation.github.io/tgstation-server/architecture.html) so as to better implement your change

There is an open list of approachable issues for [your inspiration here](https://github.com/tgstation/tgstation-server/issues?q=is%3Aopen+is%3Aissue+label%3A%22Good+First+Issue%22). You can also view the waffle board [here](https://waffle.io/tgstation/tgstation-server).

Here is a link to the code's always up-to-date documentation: https://tgstation.github.io/tgstation-server/annotated.html

You can of course, as always, ask for help at [#coderbus](irc://irc.rizon.net/coderbus) on irc.rizon.net. We're just here to have fun and help out, so please don't expect professional support.

### Development Environment

You need the Dotnet 2.2 SDK and npm>=v5.7 (in your PATH) to compile the server. In order to build the service version you also need a .NET 4.7.1 build chain

The recommended IDE is Visual Studio 2019 which has installation options for both of these.

In order to run the integration tests you must have the following environment variables set:
- `TGS4_TEST_DATABASE_TYPE`: `MySql`, `MariaDB` or `SqlServer`.
- `TGS4_TEST_CONNECTION_STRING`: To a valid database connection string. You can use the setup wizard to create one.

The following environment variables aren't required but enable more tests.
- `TSG4_TEST_DISCORD_TOKEN`: To a valid discord bot token.
- `TGS4_TEST_DISCORD_CHANNEL`: To a valid discord channel ID that the above bot can access.

### Know your Code

The `/src` folder at the root of this repository contains a series of `README.md` files useful for helping find your way around the codebase.

## Specifications

As mentioned before, you are expected to follow these specifications in order to make everyone's lives easier. It'll save both your time and ours, by making sure you don't have to make any changes and we don't have to ask you to. Thank you for reading this section!

### Object Oriented Code
As C# is an object-oriented language, code must be object-oriented when possible in order to be more flexible when adding content to it. If you don't know what "object-oriented" means, we highly recommend you do some light research to grasp the basics.

### No hacky code
Hacky code, such as adding specific checks, is highly discouraged and only allowed when there is ***no*** other option. (Protip: 'I couldn't immediately think of a proper way so thus there must be no other option' is not gonna cut it here! If you can't think of anything else, say that outright and admit that you need help with it. Maintainers exist for exactly that reason.)

You can avoid hacky code by using object-oriented methodologies, such as overriding a function (called "procs" in DM) or sectioning code into functions and then overriding them as required.

### No duplicated code
Copying code from one place to another may be suitable for small, short-time projects, but /tg/station is a long-term project and highly discourages this.

Instead you can use object orientation, or simply placing repeated code in a function, to obey this specification easily.

### No magic numbers or strings
This means stuff like having a "mode" variable for an object set to "1" or "2" with no clear indicator of what that means. Make these `const string`s with a name that more clearly states what it's for. This is clearer and enhances readability of your code! Get used to doing it!

### Class Design Guidelines

DO:

- Use the sealed keyword where possible
- Use the readonly keyword where possible
- Use 1 line bodies (void X() => Y();) where possible (Excluding constructors)
- Use the factory pattern where reasonable
- Use the const keyword where possible
- Use the var keyword where possible
- Use the static keyword on member functions where possible
- Use CancellationTokens where possible
- Throw appropriate ArgumentExceptions for public functions

DON'T:

- Use the private keyword
- Use the internal keyword
- Use the static keyword on fields where avoidable
- Use the public keyword where avoidable
- Handle Tasks in a synchronous fashion

### Versioning

The version format we use is 4.\<major\>.\<minor\>.\<patch\>. The first number never changes and TGS 1/2/3/4 are to be considered seperate products. The numbers that follow are the semver. The criteria for changing a version number is as follows

- Major: A breaking change to the DMAPI
- Minor: Additions or changes to the API or DMAPI or feature additions to the service
- Patch: Non-breaking changes internal to each of the 3 modules (Service, API, DMAPI)

### Formatting

Stylecop will throw warnings if your code does not match style guidelines. Do NOT suppress these

### Use early return
Do not enclose a function in an if-block when returning on a condition is more feasible
This is bad:
```C#
void Hello()
{
	if (thing1)
		if (!thing2)
			if (thing3 == 30)
				do stuff
}
```
This is good:
```C#
void Hello() 
{
	if (!thing1)
		return;
	if (thing2)
		return;
	if (thing3 != 30)
		return;
	do stuff
}
```
This prevents nesting levels from getting deeper then they need to be.

### Other Notes
* Code should be modular where possible; if you are working on a new addition, then strongly consider putting it in its own file unless it makes sense to put it with similar ones.

* You are expected to help maintain the code that you add, meaning that if there is a problem then you are likely to be approached in order to fix any issues, runtimes, or bugs.

* Some terminology to help understand the architecture:
	* An instance can be thought of as a separate server. It has a separate directory, repository, set of byond installations, etc... The only thing shared amongst instances is API surface, users, global configuration, the active tgstation-server version, and the host machine.
	* API refers to the HTTP API unless otherwise specified.
	* The entirety of server functionality resides in the host (Tgstation.Server.Host) project.
	* A Component is a service running in tgstation-server to help with instance functionality. These can only be communicated with via the HTTP or DM APIs.
	* There is a difference between Watchdog and Host Watchdog. The former monitors DreamDaemon uptime, the latter handles updating tgstation-server.
	* Interop is complicated terminology wise:
		* Interop: The overall process of communication between tgstation-server and DreamDaemon.
		* DMAPI: The tgstation-server provided code compiled into .dmbs to provide additional functionality.
		* Topic: The process of sending a message from the TGS -> DD via /world/Topic() and receiving a response.
		* Bridge: The process of sending a message from DD -> TGS and receiving a response.

## Pull Request Process

There is no strict process when it comes to merging pull requests. Pull requests will sometimes take a while before they are looked at by a maintainer; the bigger the change, the more time it will take before they are accepted into the code. Every team member is a volunteer who is giving up their own time to help maintain and contribute, so please be courteous and respectful. Here are some helpful ways to make it easier for you and for the maintainers when making a pull request.

* Make sure your pull request complies to the requirements outlined in [this guide](http://tgstation13.org/wiki/Getting_Your_Pull_Accepted) (with the exception of point 3)

* You are going to be expected to document all your changes in the pull request and add/update XML documentation comments for the functions and classes you modify. Failing to do so will mean delaying it as we will have to question why you made the change. On the other hand, you can speed up the process by making the pull request readable and easy to understand, with diagrams or before/after data.

* If you are proposing multiple changes, which change many different aspects of the code, you are expected to section them off into different pull requests in order to make it easier to review them and to deny/accept the changes that are deemed acceptable.

* If your pull request is accepted, the code you add no longer belongs exclusively to you but to everyone; everyone is free to work on it, but you are also free to support or object to any changes being made, which will likely hold more weight, as you're the one who added the feature. It is a shame this has to be explicitly said, but there have been cases where this would've saved some trouble.

* Your submission must be tested with 100% code coverage with both unit and integration tests

* Please explain why you are submitting the pull request, and how you think your change will be beneficial to the server. Failure to do so will be grounds for rejecting the PR.

* Commits MUST be properly titled and commented as we only use merge commits for the pull request process

## Making Model Changes

Whenever you make a change to a model schema that must be reflected in the database, you'll have to generate and write a migration for it on all supported database types.

1. Ensure you have the EntityFrameworkCore migration tools installed with `dotnet tool install --global dotnet-ef`.
1. Make the code changes for your model.
1. Open a command prompt in the `/src/Tgstation.Server.Host` directory.
1. Configure your appsettings.Development.json to connect to a valid SQL Server database.
1. Run `dotnet ef migrations add MS<NameOfYourMigration> --context SqlServerDatabaseContext`
1. Configure your appsettings.Development.json to connect to a valid MySQL/MariaDB database.
1. Run `dotnet ef migrations add MY<NameOfYourMigration> --context MySqlDatabaseContext`
1. Configure your appsettings.Development.json to connect to a valid SQLite3 database.
1. Run `dotnet ef migrations add SL<NameOfYourMigration> --context SqliteDatabaseContext`
1. Fix compiler warnings in the generated files. Ensure all classes are in the Tgstation.Server.Host.Models.Migrations namespace.
1. Run the server in both configurations to ensure the migrations work.

You should now have MY/MS migration files generated in `/src/Tgstation.Server.Host/Models/Migrations

### Important Note About the \[Required\] Attribute.

We use this attribute to ensure EFCore generated tables are not nullable for specific properties. They are valid to be null in API communication. Do not use this attribute expecting the model validator to prevent null data in API request.

## Code Versioning

Any backwards compatible fixes made should be committed to the `master` branch if possible. These will be automatically merged into the `dev` branch.
All other changes should be made directly to the `dev` branch. These will be merged to `master` on the next minor release cycle.

## Deployment Process

Every issue/pull request in a release should share a common milestone named with the release version i.e. `4.5.3.5`

When every issue and PR in the milestone is closed. Create a version bump PR that changes the version numbers. At the time of this writing they exist in the following files

- `/src/Tgstation.Server.Host/Tgstation.Server.Host.csproj`
- `/src/Tgstation.Server.Host.Console/Tgstation.Server.Host.Console.csproj`
- 2 in `/src/Tgstation.Server.Host.Service/Properties/AssemblyInfo.cs`
- `/src/Tgstation.Server.Host.Watchdog/Tgstation.Server.Host.Watchdog.csproj`

Merge the pull request with `[TGSDeploy]` somewhere in the commit title. The scripts will handle amalgamating release notes, building, closing the milestone, and publishing the release. This will also trigger update notifications on existing TGS deployments.

### API/Client Deployment

The Api/Client project versions must be updated on nuget when changed. The numbers exist in the following files:

- `/src/Tgstation.Server.Api/Tgstation.Server.Api.csproj`
- `/src/Tgstation.Server.Client/Tgstation.Server.Client.csproj`

These should not be the same numbers as the main suite. When bumping the API version the client should only receive a minor (3rd number) version bump unless major changes to CLIENT code were made.

Merge these alongside regular deployments with `[NugetDeploy]` in the commit title (Won't work without an accompanying `[TGSDeploy]`). This will handle the nuget publishing.

## Banned content

Do not add any of the following in a Pull Request or risk getting the PR closed:
* National Socialist Party of Germany content, National Socialist Party of Germany related content, or National Socialist Party of Germany references

Just becuase something isn't on this list doesn't mean that it's acceptable. Use common sense above all else.
