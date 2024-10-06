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

You need the .NET 8.0 SDK, node>=v20, and npm>=v5.7 (in your PATH) to compile the server. On Linux, you also need the `libgdiplus` package installed to generate icons.

You need to run `corepack enable` to configure node to correctly build the webpanel.

The recommended IDE is Visual Studio 2022 or VSCode.

In order to build the service version and/or the Windows installer you need a to run on Windows.

In addition, the installer project uses the Wix v4 Toolset which will cause an error on loading the .sln in Visual Studio if the [HeatWave for VS2022 Extension](https://marketplace.visualstudio.com/items?itemName=FireGiant.FireGiantHeatWaveDev17) is not installed.

In order to run the integration tests you must have the dotnet 7.0 SDK installed to properly build the OpenDream minimum compatible version.
You must also have the following environment variables set. To run them more accurately, include the optional ones.

- `TGS_TEST_DATABASE_TYPE`: `MySql`, `MariaDB`, `PostgresSql`, or `SqlServer`.
- `TGS_TEST_CONNECTION_STRING`: To a valid database connection string. You can use the setup wizard to create one.
- (Optional) `TGS_TEST_GITHUB_TOKEN`: A GitHub personal access token with no scopes used to bypass rate limits.
- (Optional) The following variables are all interdependent, so if one is set they all must be.
  - `TGS_TEST_DISCORD_TOKEN`: To a valid discord bot token.
  - `TGS_TEST_DISCORD_CHANNEL`: To a valid discord channel ID that the above bot can access.
  - `TGS_TEST_IRC_CONNECTION_STRING`: To a valid IRC connection string. See the code for [IrcConnectionStringBuilder](../src/Tgstation.Server.Api/Models/IrcConnectionStringBuilder.cs) for details.
  - `TGS_TEST_IRC_CHANNEL`: To a valid IRC channel accessible with the above connection.
- (Optional) `TGS_TEST_OD_ENGINE_VERSION`: Specify the full git commit SHA of the [OpenDream](https://github.com/OpenDreamProject/OpenDream) version to use in the main integration test, the default is the current HEAD of the default branch.
- (Optional) `TGS_TEST_OD_GIT_DIRECTORY`: Path to a local [OpenDream](https://github.com/OpenDreamProject/OpenDream) git repository to use as an upstream for testing.
- (Optional) `TGS_TEST_OD_EXCLUSIVE`: Set to `true` to enable the quicker integration test that only runs [OpenDream](https://github.com/OpenDreamProject/OpenDream) functionality. This is tested by default in the main integration test.

### Notes About Forks

For the full CI gambit, the following repository configuration must be set:

- Setting `Workflow Permissions` to `Read and write permissions`: Enables GitHub Actions comments.
  ![image](https://github.com/tgstation/tgstation-server/assets/8171642/ab17fa74-364f-4e66-b7c4-b9bb24c6a599)
- Label `CI Cleared`: To allow PRs from forks to run CI with secrets after approval.
- Integration [CodeCov](https://github.com/apps/codecov): Enables CodeCov status checks.
- Secret `CODECOV_TOKEN`: A CodeCov repo token to work around https://github.com/codecov/codecov-action/issues/837.
- Secret `LIVE_TESTS_TOKEN`: A GitHub token with read access to the repository and write access to https://github.com/Cyberboss/common_core (TODO: Make the target repository here configurable). Despite it's name, it may be used across the entire test suite.
- Secret `TGS_TEST_DISCORD_TOKEN`: See above note about test environment variables.
- Secret `TGS_TEST_DISCORD_CHANNEL`: See above note about test environment variables.
- Secret `TGS_TEST_IRC_CONNECTION_STRING`: See above note about test environment variables.
- Secret `TGS_TEST_IRC_CHANNEL`: See above note about test environment variables.

If you don't plan on deploying TGS, the following secrets can be omitted:

- Secret `DEV_PUSH_TOKEN`: A repo scoped GitHub PAT with read/write access on the repository and the ability to trigger workflows on https://github.com/tgstation/tgstation-ppa. Used to trigger debian repo rebuilds, bypass rate limits, update milestones, and create winget package acceptance PRs.
- Secret `TGS_CI_GITHUB_APP_TOKEN_BASE64` is a base 64 encoded private key for a GitHub App. This app must be installed on the repo and have read/write access to checks and contents. Used to generate CI checks, push changelogs, and create releases.
- Secret `DOCKER_USERNAME`: Login username for Docker image push.
- Secret `DOCKER_PASSWORD`: Login password for Docker image push.
- Secret `NUGET_API_KEY`: Nuget.org API Key for client libraries push.
- Secret `CODE_SIGNING_BASE64`: Base64 string of a .pfx file containing an X.509 code-signing certificate.
- Secret `CODE_SIGNING_PASSWORD`: Password for importing the above .pfx.
- Variable `CODE_SIGNING_THUMBPRINT`: Thumbprint for the above .pfx

### Know your Code

- All feature work should be submitted to the `dev` branch for the next minor release.
- All patch work should be submitted to the `master` branch for the next patch. Changes will be automatically integrated into `dev`

The `/src` folder at the root of this repository contains a series of `README.md` files useful for helping find your way around the codebase.

## Specifications

You are expected to follow these specifications in order to make everyone's lives easier. It'll save both your time and ours, by making sure you don't have to make any changes and we don't have to ask you to. Thank you for reading this section!

### Object Oriented Code

As C# is an object-oriented language, code must be object-oriented when possible in order to be more flexible when adding content to it. If you don't know what "object-oriented" means, we highly recommend you do some light research to grasp the basics.

### No hacky code

Hacky code, such as adding specific checks, is highly discouraged and only allowed when there is **_no_** other option. (Protip: 'I couldn't immediately think of a proper way so thus there must be no other option' is not gonna cut it here! If you can't think of anything else, say that outright and admit that you need help with it. Maintainers exist for exactly that reason.)

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
- Use automatic getters where possible.
- Prefer fields to properties where stylecop allows.
- Use 1 line bodies (void X() => Y();) where possible (Excluding constructors)
- Use the factory pattern where reasonable
- Use the const keyword where possible
- Use the var keyword where possible
- Use the static keyword on member and inline functions where possible
- Use CancellationTokens where possible
- Throw appropriate ArgumentExceptions for public functions
- Use nullable references approprately
- Prefer `ValueTask`s to `Task`s where possible.
- Return `Task` instead of `ValueTask` if all the callers would need to `.AsTask()` it.

DON'T:

- Use the private keyword
- Use the internal keyword
- Use the static keyword on fields where avoidable
- Use the public keyword where avoidable
- Handle `ValueTask`s/`Task`s in a synchronous fashion
- Use static methods from built-in keywords i.e. Use `Boolean.TryParse` instead of `bool.TryParse`

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

- Code should be modular where possible; if you are working on a new addition, then strongly consider putting it in its own file unless it makes sense to put it with similar ones.

- You are expected to help maintain the code that you add, meaning that if there is a problem then you are likely to be approached in order to fix any issues, runtimes, or bugs.

- Some terminology to help understand the architecture:
  - An instance can be thought of as a separate server. It has a separate directory, repository, set of byond installations, etc... The only thing shared amongst instances is API surface, users, global configuration, the active tgstation-server version, and the host machine.
  - API refers to the REST API unless otherwise specified.
  - The entirety of server functionality resides in the host (Tgstation.Server.Host) project.
  - A Component is a service running in tgstation-server to help with instance functionality. These can only be communicated with via the HTTP or DM APIs.
  - There is a difference between Watchdog and Host Watchdog. The former monitors DreamDaemon uptime, the latter handles updating tgstation-server.
  - Interop is complicated terminology wise:
    - Interop: The overall process of communication between tgstation-server and DreamDaemon.
    - DMAPI: The tgstation-server provided code compiled into .dmbs to provide additional functionality.
    - Topic: The process of sending a message from the TGS -> DD via /world/Topic() and receiving a response.
    - Bridge: The process of sending a message from DD -> TGS and receiving a response.

## Pull Request Process

There is no strict process when it comes to merging pull requests. Pull requests will sometimes take a while before they are looked at by a maintainer; the bigger the change, the more time it will take before they are accepted into the code. Every team member is a volunteer who is giving up their own time to help maintain and contribute, so please be courteous and respectful. Here are some helpful ways to make it easier for you and for the maintainers when making a pull request.

- Make sure your pull request complies to the requirements outlined in [this guide](http://tgstation13.org/wiki/Getting_Your_Pull_Accepted) (with the exception of point 3)

- You are going to be expected to document all your changes in the pull request and add/update XML documentation comments for the functions and classes you modify. Failing to do so will mean delaying it as we will have to question why you made the change. On the other hand, you can speed up the process by making the pull request readable and easy to understand, with diagrams or before/after data.

- If you are proposing multiple changes, which change many different aspects of the code, you are expected to section them off into different pull requests in order to make it easier to review them and to deny/accept the changes that are deemed acceptable.

- If your pull request is accepted, the code you add no longer belongs exclusively to you but to everyone; everyone is free to work on it, but you are also free to support or object to any changes being made, which will likely hold more weight, as you're the one who added the feature. It is a shame this has to be explicitly said, but there have been cases where this would've saved some trouble.

- Your submission must be tested with 100% code coverage with both unit and integration tests

- Please explain why you are submitting the pull request, and how you think your change will be beneficial to the server. Failure to do so will be grounds for rejecting the PR.

- Commits MUST be properly titled and commented as we only use merge commits for the pull request process

## Making Model Changes

Whenever you make a change to a model schema that must be reflected in the database, you'll have to generate and write a migration for it on all supported database types.

We have a script to do this.

Warning: You may need to temporarily set valid MySql credentials in [MySqlDesignTimeDbContextFactory.cs](../src/Tgstation.Server.Host/Database/Design/MySqlDesignTimeDbContextFactory.cs) for migrations to generate properly. I have no idea why. Be careful not to commit the change.

1. Run `build/GenerateMigrations.sh NameOfMigration` from the project root.
1. You should now have MY/MS/SL/PG migration files generated in `/src/Tgstation.Server.Host/Models/Migrations`. Fix compiler warnings in the generated files. Ensure all classes are in the Tgstation.Server.Host.Database.Migrations namespace.
1. Manually review what each migration does.
1. Run the server in both configurations to ensure the migrations work.

### Manual Method

1. Make the code changes for your model.
1. Open a command prompt in the `/src/Tgstation.Server.Host` directory.
1. Ensure you have the EntityFrameworkCore migration tools installed with `dotnet tool restore`.
1. Run `dotnet ef migrations add MS<NameOfYourMigration> --context SqlServerDatabaseContext`
1. Run `dotnet ef migrations add MY<NameOfYourMigration> --context MySqlDatabaseContext`
1. Run `dotnet ef migrations add PG<NameOfYourMigration> --context PostgresSqlDatabaseContext`
1. Run `dotnet ef migrations add SL<NameOfYourMigration> --context SqliteDatabaseContext`.
1. Follow the above steps.

## Adding OAuth Providers

OAuth providers are hardcoded but it is fairly easy to add new ones. The flow doesn't need to be strict OAuth either (r.e. /tg/ forums). Follow the following steps:

1. Add the name to the [Tgstation.Server.Api.Models.OAuthProviders](../src/Tgstation.Server.Api/Models/OAuthProviders.cs) enum (Also necessitates a minor API version bump to the HTTP APIs (REST/GraphQL)).
1. Create an implementation of [IOAuthValidator](../src/Tgstation.Server.Host/Security/OAuth/IOAuthValidator.cs).
   - Most providers can simply override the [GenericOAuthValidator](../src/Tgstation.Server.Host/Security/OAuth/GenericOAuthValidator.cs).
1. Construct the implementation in the [OAuthProviders](../src/Tgstation.Server.Host/Security/OAuth/OAuthProviders.cs) class.
1. Add a null entry to the default [appsettings.yml](../src/Tgstation.Server.Host/appsettings.yml).
1. Update the main [README.md](../README.md) to indicate the new provider.
1. Update the [API documentation](../docs/API.dox) to indicate the new provider.

TGS should now be able to accept authentication response tokens from your provider.

### Important Note About the \[Required\] Attribute.

We use this attribute to ensure EFCore generated tables are not nullable for specific properties. They are valid to be null in API communication. Do not use this attribute expecting the model validator to prevent null data in API request.

## Versioning

We follow [semantic versioning](https://semver.org) (Although TGS 1/2/3 are to be considered seperate products). The numbers that follow are the semver. The criteria for changing a version number is as follows

- Major: A change that requires direct host access to apply properly. Generally, these are updates to the dotnet runtime.
- Minor: A feature addition to the core server functionality.
- Patch: Patch changes.

Patch changes should be committed to the `master` branch if possible. These will be automatically merged into the `dev` branch.
All minor changes should be made directly to the `dev` branch. These will be merged to `master` on the next minor release cycle.
Major changes should be committed to the `VX` branch created when the time for a major release comes around.

We have several subcomponent APIs we ship with the core server that have their own versions.

- REST API
- GraphQL API
- DreamMaker API
- Interop API
- Configuration File
- Host Watchdog
- Web Control Panel

These don't affect the core version. The only stipulation is major changes to the HTTP and DreamMaker APIs or the configuration must coincide with a minor core version bump.

All versions are stored in the master file [build/Version.props](../build/Version.props). They are repeatedly defined in a few other places, but integration tests will make sure they are consistent.

#### Nuget Versioning

The NuGet package Tgstation.Server.Client is another part of the suite which should be versioned separately. However, Tgstation.Server.Api is also a package that is published, and breaking changes can happen independantly of each other.

- Consider Tgstation.Server.Client it's own product, perform major and minor bumps according to semver semantics including the Tgstation.Server.Api code (but not the version).
- Tgstation.Server.Api is a bit tricky as breaking code changes may occur without affecting the actual HTTP contract. For this reason, the library itself is versioned separately from the API contract.
- Tgstation.Server.Common is also versioned independently.

## Triage, Deployment, and Releasing

_This section mainly applies to people with write access to the repository. Anyone is free to propose their work and maintainers will triage it appropriately._

When issues affecting the server come in, they should be labeled appropriately and either put into the `Backlog` milestone or current patch milestone depending on if it's a feature request or bug.

After a minor release, the team should decide at that time what will go into it and setup the milestone accordingly. At this point the `Backlog` label should be removed and replaced with `Ready` and the milestone changed from `Backlog` to `vX.Y.0` with X/Y being the major/minor release versions respectively.

Assign work before beginning on it. When work is started, replace the `Ready` label with the `Work In Progress` label.

Word commit names descriptively. Only submit work through pull requests (With the exception of resolving conflicts in the `master` -> `dev` automatic merge). When doing so, link the issue you'll be closing and set the milestone appropriately. Don't forget a changelog. **WARNING** Remember to submit patches to the `master` branch. Also at the time of this writing there appears to be an issue where GitHub won't close issues with PRs to the non-default branch. Maintainers may need to do this manually after merging. Alternatively, add closing keywords to the commit message and let the `master` -> `dev` merge do the closing.

At the time of this writing, the repository is configured to automate much of the deployment/release process.

When the new API, client, or DMAPI is ready to be released, update the `Version.props` file appropriately and merge the pull request with the text `[RESTDeploy]`, `[GQLDeploy]`, `[NugetDeploy]`, or `[DMDeploy]` respectively in the commit message (or all three!). The release will be published automatically.

That step should be taken for the latest API and client before releasing the core version that uses them if applicable.

Before releasing the core version, ensure the following:

- For minor/major releases, ensure your changes are merging `dev` into `master`.
- Ensure all issues and pull requests in the associated milestone are closed (aside from the PR you are using to cut the release).

To perform the release, merge the PR with `[TGSDeploy]` in the commit message. The build system will handle generating release notes, packaging, and pushing the build to GitHub releases. This will also make it available for servers to self update. Note that `[TGSDeploy]` only affects commits on the `master` branch.

The build system will also handle closing the current milestone and creating new minor/patch milestones where applicable.

## Banned content

Do not add any of the following in a Pull Request or risk getting the PR closed:

- National Socialist Party of Germany content, National Socialist Party of Germany related content, or National Socialist Party of Germany references

Just becuase something isn't on this list doesn't mean that it's acceptable. Use common sense above all else.
