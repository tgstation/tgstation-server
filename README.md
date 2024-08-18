<p align="center">
  <img src =./build/logo.svg>
</p>

# tgstation-server

[![CI Pipeline](https://github.com/tgstation/tgstation-server/actions/workflows/ci-pipeline.yml/badge.svg)](https://github.com/tgstation/tgstation-server/actions/workflows/ci-pipeline.yml) [![codecov](https://codecov.io/gh/tgstation/tgstation-server/branch/master/graph/badge.svg)](https://codecov.io/gh/tgstation/tgstation-server)

[![GitHub license](https://img.shields.io/github/license/tgstation/tgstation-server.svg)](LICENSE) [![Average time to resolve an issue](http://isitmaintained.com/badge/resolution/tgstation/tgstation-server.svg)](http://isitmaintained.com/project/tgstation/tgstation-server "Average time to resolve an issue") [![NuGet version](https://img.shields.io/nuget/v/Tgstation.Server.Api.svg)](https://www.nuget.org/packages/Tgstation.Server.Api) [![NuGet version](https://img.shields.io/nuget/v/Tgstation.Server.Client.svg)](https://www.nuget.org/packages/Tgstation.Server.Client)

[![forthebadge](http://forthebadge.com/images/badges/made-with-c-sharp.svg)](http://forthebadge.com) [![forinfinityandbyond](https://user-images.githubusercontent.com/5211576/29499758-4efff304-85e6-11e7-8267-62919c3688a9.gif)](https://www.reddit.com/r/SS13/comments/5oplxp/what_is_the_main_problem_with_byond_as_an_engine/dclbu1a) [![forthebadge](http://forthebadge.com/images/badges/built-with-love.svg)](http://forthebadge.com)

This is a toolset to manage production DreamMaker servers. It includes the ability to update the server without having to stop or shutdown the server (the update will take effect on a "reboot" of the server), the ability to start the server and restart it if it crashes, as well as systems for managing code and game files, and locally merging GitHub Pull Requests for test deployments.

## Setup

### Pre-Requisites

_Note: If you opt to use the Windows installer, most pre-requisites for running BYOND servers (including MariaDB) are provided out of the box._

_If you are running on a Windows Server OS. You **might** need to install the [x86 Visual C++ 2015 Runtime](https://aka.ms/vs/17/release/vc_redist.x86.exe) to run BYOND._

_If you wish to use OpenDream you will need to install the required dotnet SDK manually._

tgstation-server needs a relational database to store it's data.

If you're just a hobbyist server host, you can probably get away with using SQLite for this. SQLite is bundled with TGS and simply requires you to specify where on your machine you want to store the data.

_HOWEVER_

SQLite is not a battle-ready relational database. It doesn't scale well for any use case. TGS *strongly* recommends you use one of its supported standalone databases. Setting one of these up is more involved but worth the effort.

The supported standalone databases are:

- [MariaDB](https://downloads.mariadb.org/) _- NOTE: If you plan on hosting SpaceStation 13, this is the database most codebases support, making it an ideal choice_
- [PostgresSQL](https://www.postgresql.org/download/)
- [Microsoft SQL Server](https://www.microsoft.com/en-us/download/details.aspx?id=55994)
- MySQL

TGS will require either:
- No pre-existing database WITH schema creation permissions.
or
- Exclusive access to a database schema that TGS has full control over.

### Installation

Follow the instructions for your OS below.

#### Windows

###### Note about Digital Signatures

Note that the Windows Service and installer executables require administrative privileges. These are digitally signed against the Root CA managed by [Jordan Dominion](https://github.com/Cyberboss). Consider installing the certificate into your `Trusted Root Authorities` store for cleaner UAC prompts. The certificate can be downloaded [here](https://file.house/zpFb.cer), please validate the thumbprint is `70176acf7ffa2898fa5b5cd6e38b43b38ea5d07f` before installing. The OCSP server for this is `http://ocsp.dextraspace.net`.

##### Installer

[Download the latest release's tgstation-server-installer.exe](https://github.com/tgstation/tgstation-server/releases/latest). Executing it will take you through the process of installing and configuring your server. The required dotnet runtime may be installed as a pre-requisite.

Note: If you use the `/silent` or `/passive` arguments to the installer, you will not be able to install MariaDB using it. In addition, if those arguments are present, you'll need to either pre-configure TGS or configure and start the `tgstation-server` service after installing. A shortcut will be placed on your desktop and in your start menu to assist with this.

##### winget (Windows 10 or later)

[winget](https://github.com/microsoft/winget-cli) installed is the easiest way to install the latest version of tgstation-server (provided Microsoft has approved the most recent package manifest).

Check if you have `winget` by running the following command.
```
winget --version
```

If it returns an error that means you don't have winget. You can easily install it by running the following commands in an administrative Windows Powershell instance:
```
Import-Module Appx
Invoke-WebRequest -Uri https://www.nuget.org/api/v2/package/Microsoft.UI.Xaml/2.7.3 -OutFile .\microsoft.ui.xaml.2.7.3.zip
Expand-Archive .\microsoft.ui.xaml.2.7.3.zip
Add-AppxPackage .\microsoft.ui.xaml.2.7.3\tools\AppX\x64\Release\Microsoft.UI.Xaml.2.7.appx
Add-AppxPackage -Path "https://aka.ms/Microsoft.VCLibs.x64.14.00.Desktop.appx"
Add-AppxPackage -Path "https://github.com/microsoft/winget-cli/releases/latest/download/Microsoft.DesktopAppInstaller_8wekyb3d8bbwe.msixbundle"
Remove-Item .\microsoft.ui.xaml.2.7.3\ -r
Remove-Item .\microsoft.ui.xaml.2.7.3.zip
```

Once winget is installed, simply run the following commands, accepting any prompts that may appear:

```ps
winget install tgstation-server
```

The required dotnet runtime may be installed as a pre-requisite. MariaDB will not be installed.

Note: If you use the `-h` or `--disable-interactivity` winget arguments, you will need to either pre-configure TGS or configure and start the `tgstation-server` service after installing. A shortcut will be placed on your desktop and in your start menu to assist with this.

Note: The `winget` package is submitted to Microsoft for approval once TGS releases. This means the winget version may be out of date with the current release version. You can always use the TGS updater after installing. You can see the versions still awaiting approval [here](https://github.com/microsoft/winget-pkgs/pulls?q=is%3Apr+is%3Aopen+Tgstation.Server).

##### Manual

If you don't have it installed already, download and install the [ASP .NET Core Runtime Hosting Bundle (>= v8.0)](https://dotnet.microsoft.com/download/dotnet/8.0). Ensure that the `dotnet` executable file is in your system's `PATH` variable (or that of the user's that will be running the server). You can test this by opening a command prompt and running `dotnet --list-runtimes`.

[Download the latest release .zip](https://github.com/tgstation/tgstation-server/releases/latest). Typically, you want the `ServerService.zip` package in order to run TGS as a Windows service. Choose `ServerConsole.zip` if you prefer to use a command line daemon.

Extract the .zip file to where you want the server to run from. Note the account running the server must have write, execute, and delete access to the `lib` subdirectory.

If you wish to install the TGS as a service, run `Tgstation.Server.Host.Service.exe`. It should prompt you to install it. Click `Yes` and the setup wizard should run.

Should you want a clean start, be sure to first uninstall the service by running `Tgstation.Server.Host.Service.exe -u` from the command line.

If using the console version, run `./tgs.bat` in the root of the installation directory. Ctrl+C will close the server, terminating all live game instances.

#### Linux

Installing natively is the recommended way to run tgstation-server on Linux.

##### Ubuntu/Debian Package

You first need to add the appropriate Microsoft package repository for your distribution

Refer to the Microsoft website for steps for

- [Ubuntu](https://learn.microsoft.com/en-us/dotnet/core/install/linux-ubuntu#register-the-microsoft-package-repository)
- [Debian 12](https://learn.microsoft.com/en-us/dotnet/core/install/linux-debian#debian-12)
- [Debian 11](https://learn.microsoft.com/en-us/dotnet/core/install/linux-debian#debian-11)
- [Debian 10](https://learn.microsoft.com/en-us/dotnet/core/install/linux-debian#debian-10)
- [Other Distros](https://learn.microsoft.com/en-us/dotnet/core/install/linux-scripted-manual#manual-install)

After that, install TGS and all it's dependencies via our apt repository, interactively configure it, and start the service with this one-liner:

```sh
sudo dpkg --add-architecture i386 \
&& sudo apt update \
&& sudo apt install -y software-properties-common \
&& sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv B6FD15EE7ED77676EAEAF910EEEDC8280A307527 \
&& sudo add-apt-repository -y "deb https://tgstation.github.io/tgstation-ppa/debian unstable main" \
&& sudo apt update \
&& sudo apt install -y tgstation-server \
&& sudo tgs-configure \
&& sudo systemctl start tgstation-server
```

The service will execute as the newly created user: `tgstation-server`. You should, ideally, store your instances somewhere under `/home/tgstation-server`.

##### Manual Setup

The following dependencies are required.

- aspnetcore-runtime-8.0 (See Prerequisites under the `Ubuntu/Debian Package` section)
- libc6-i386
- libstdc++6:i386
- gcc-multilib (Only on 64-bit systems)
- gdb (for using gcore to create core dumps)

[Download the latest release .zip](https://github.com/tgstation/tgstation-server/releases/latest). Choose `ServerConsole`.

If you have SystemD installed, we recommend installing the service unit [here](./build/tgstation-server.service). It assumes TGS is installed into `/opt/tgstation-server`, it is executing as the user `tgstation-server`, and you will be using the console runner, but feel free to adjust it to your needs. Note that the server will need to have it's configuration file setup before running with SystemD.

Alternatively, to launch the server in the current shell, run `./tgs.sh` in the root of the installation directory. The process will run in a blocking fashion. SIGQUIT will close the server, terminating all live game instances.

##### Docker

tgstation-server supports running in a docker container. The official image repository is located at https://hub.docker.com/r/tgstation/server. It can also be built locally by running `docker build . -f build/Dockerfile -t <your tag name>` in the repository root.

To create a container run
```sh
docker run \
	-ti \ # Start with interactive terminal the first time to run the setup wizard
	--restart=always \ # Recommended for maximum uptime
	--network="host" \ # Not recommended, eases networking setup if your sql server is on the same machine
	--name="tgs" \ # Name for the container
	--cap-add=sys_nice \ # Recommended, allows TGS to lower the niceness of child processes if it sees fit
	--cap-add=sys_resource \ # Recommended, allows TGS to not be killed by the OOM killer before its child processes
	--init \ #Highly recommended, reaps potential zombie processes
	-p 5000:5000 \ # Port bridge for accessing TGS, you can change this if you need
	-p 0.0.0.0:<public game port>:<public game port> \ # Port bridge for accessing DreamDaemon
	-v /path/to/your/configfile/directory:/config_data \ # Recommended, create a volume mapping for server configuration
	-v /path/to/store/instances:/tgs_instances \ # Recommended, create a volume mapping for server instances
	-v /path/to/your/log/folder:/tgs_logs \ # Recommended, create a volume mapping for server logs
	tgstation/server[:<release version>]
```
with any additional options you desire (i.e. You'll have to expose more game ports in order to host more than one instance).

When launching the container for the first time, you'll be prompted with the setup wizard and then the container will exit. Start the container again to launch the server.

Important note about port exposure: The internal port used by DreamDaemon _**MUST**_ match the port you want users to connect on. If it doesn't, you'll still be able to have them connect HOWEVER links from the BYOND hub will point at what DreamDaemon thinks the port is (the internal port).

Note although `/app/lib` is specified as a volume mount point in the `Dockerfile`, unless you REALLY know what you're doing. Do not mount any volumes over this for fear of breaking your container.

The configuration option `General:ValidInstancePaths` will be preconfigured to point to `/tgs_instances`. It is recommended you don't change this.

Note that this container is meant to be long running. Updates are handled internally as opposed to at the container level.

Note that automatic configuration reloading is currently not supported in the container. See #1143

If using manual configuration, before starting your container make sure the aforementioned `appsettings.Production.yml` is setup properly. See below

#### OpenDream

In order for TGS to use [OpenDream](https://github.com/OpenDreamProject/OpenDream), it requires the full .NET SDK to build whichever version your servers target. Whatever that is, it must be available using the `dotnet` command for whichever user runs TGS.

OpenDream currently requires [.NET SDK 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) at the time of this writing. You must install this manually outside of TGS (i.e. using your package manager).

<details>
  <summary>How to handle a different SDK version than the ASP.NET runtime of TGS.</summary>

  On Linux, as long as OpenDream and TGS do not use the same .NET major version, you cannot achieve this with the package manager as they will conflict. For example, the 7.0 SDK can be added to an 8.0 runtime installation via the following steps.

  1. Install `tgstation-server` using any of the above methods.
  1. [Download the Linux SDK binaries](https://dotnet.microsoft.com/en-us/download/dotnet/7.0) for your selected architecture.
  1. Extract everything EXCEPT the `dotnet` executable, `LICENSE.txt`, and `ThirdPartyNotices.txt` in the `.tar.gz` on top of the existing installation directory `/usr/share/dotnet/`
  1. Run `sudo chown -R root /usr/share/dotnet`

  You should now be able to run the `dotnet --list-sdks` command and see an entry for `7.0.XXX [/usr/share/dotnet/sdk]`.
</details>

### Configuring

The first time you run TGS you should be prompted with a configuration wizard which will guide you through setting up your `appsettings.Production.yml`

This wizard will, generally, run whenever the server is launched without detecting the config yml. Follow the instructions below to perform this process manually.

#### Configuration Methods

There are 3 primary supported ways to configure TGS:

- Modify the `appsettings.Production.yml` file (Recommended).
- Set environment variables in the form `Section__Subsection=value` or `Section__ArraySubsection__0=value` for arrays.
- Set command line arguments in the form `--Section:Subsection=value` or `--Section:ArraySubsection:0=value` for arrays.

The latter two are not recommended as they cannot be dynamically changed at runtime. See more on ASP.NET core configuration [here](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-8.0).

#### Manual Configuration

Create an `appsettings.Production.yml` file next to `appsettings.yml`. This will override the default settings in `appsettings.yml` with your production settings. There are a few keys meant to be changed by hosts. The configuration is only read after server statup. To reload it you must restart TGS. This can be done while your game servers are running by using the Administrative restart function (as opposed to an OS service restart). Note these are all case-sensitive:

- `General:ConfigVersion`: Suppresses warnings about out of date config versions. You should change this after updating TGS to one with a new config version. The current version can be found on the releases page for your server version.

- `General:MinimumPasswordLength`: Minimum password length requirement for database users

- `General:ValidInstancePaths`: Array meant to limit the directories in which instances may be created.

- `General:UserLimit`: Maximum number of users that may be created

- `General:InstanceLimit`: Maximum number of instances that may be created

- `General:GitHubAccessToken`: Specify a classic GitHub personal access token with no scopes here to highly mitigate the possiblity of 429 response codes from GitHub requests

- `General:SkipAddingByondFirewallException`: Set to `true` if you have Windows firewall disabled

- `Session:HighPriorityLiveDreamDaemon`: Boolean controlling if live DreamDaemon instances get set to above normal priority processes.

- `Session:LowPriorityDeploymentProcesses `: Boolean controlling if DreamMaker and API validation DreamDaemon instances get set to below normal priority processes.

- `FileLogging:Directory`: Override the default directory where server logs are stored. Default is `C:/ProgramData/tgstation-server/logs` on Windows, `/usr/share/tgstation-server/logs` otherwise

- `FileLogging:LogLevel`: Can be one of `Trace`, `Debug`, `Information`, `Warning`, `Error`, or `Critical`. Restricts what is put into the log files. Currently `Debug` is reccommended for help with error reporting.

- `Kestrel:Endpoints:Http:Url`: The URL (i.e. interface and ports) your application should listen on. General use case should be `http://localhost:<port>` for restricted local connections. See the Remote Access section for configuring public access to the World Wide Web. This doesn't need to be changed using the docker setup and should be mapped with the `-p` option instead

- `Database:DatabaseType`: Can be one of `SqlServer`, `MariaDB`, `MySql`, `PostgresSql`, or `Sqlite`.

- `Database:ServerVersion`: The version of the database server. Used by the MySQL/MariaDB and Postgres providers for selection of certain features, ignore at your own risk. A string in the form `<major>.<minor>.<patch>` for MySQL/MariaDB or `<major>.<minor>` for PostgresSQL.

- `Database:ConnectionString`: Connection string for your database. Click [here](https://www.developerfusion.com/tools/sql-connection-string/) for an SQL Server generator, or see [here](https://www.connectionstrings.com/postgresql/) for a Postgres guide or [here](https://www.connectionstrings.com/mysql/) for a MySQL guide ([You should probably use '127.0.0.1' instead of 'localhost'](https://stackoverflow.com/questions/19712307/mysql-localhost-127-0-0-1)). Sqlite connection strings should be in the format `Data Source=<PATH TO DATABASE FILE>;Mode=ReadWriteCreate`.

- `ControlPanel:Enable`: Enable the javascript based control panel to be served from the server via /index.html

- `ControlPanel:AllowAnyOrigin`: Set the Access-Control-Allow-Origin header to * for all responses (also enables all headers and methods)

- `ControlPanel:AllowedOrigins`: Set the Access-Control-Allow-Origin headers to this list of origins for all responses (also enables all headers and methods). This is overridden by `ControlPanel:AllowAnyOrigin`

- `ControlPanel:PublicPath`: URL from which the webpanel can be accessed, defaults to "/app/". Must be an absolute path (https://example.org/path/to/webpanel) or a path starting from root (/path/to/webpanel). Note that this option does not relocate the webpanel for you; you will need a reverse proxy to relocate the webpanel

- `Elasticsearch`: tgstation-server also supports automatically ingesting its logs to ElasticSearch. You can set this up in the setup wizard, or with the following configuration:
  ```yml
  Elasticsearch:
    Enable: true
    Host: http://192.168.0.200:9200
    Username: youruserhere
    Password: yourpasshere
  ```

- `Swarm`: This section should be left empty unless using the server swarm system. If this is to happen, ensure all swarm servers are set to connect to the same database.

- `Swarm:PrivateKey`: Must be a secure string set identically on all swarmed servers.

- `Swarm:ControllerAddress`: Must be set on all swarmed servers that are **not** the controller server and should be an address the controller server may be reached at.

- `Swarm:Address`: Must be set on all swarmed servers. Should be an address the server can be reached at by other servers in the swarm.

- `Swarm:PublicAddress`: Should be set on all swarmed servers. Should be an address the server can be reached at by other servers in the swarm.

- `Swarm:Identifier`: Must be set uniquely on all swarmed servers. Used to identify the current server. This is also used to select which instances exist on the current machine and should not be changed post-setup.

- `Swarm:UpdateRequiredNodeCount`: Should be set to the total number of servers in your swarm minus 1. Prevents updates from occurring unless the non-controller server count in the swarm is greater than or equal to this value.

- `Security:OAuth:<Provider Name>`: Sets the OAuth client ID and secret for a given `<Provider Name>`. The currently supported providers are `Keycloak`, `GitHub`, `Discord`, `InvisionCommunity` and `TGForums`. Setting these fields to `null` disables logins with the provider, but does not stop users from associating their accounts using the API. Sample Entry:
```yml
Security:
  OAuth:
    Keycloak:
      ClientId: "..."
      ClientSecret: "..."
      RedirectUrl: "..."
      ServerUrl: "..."
      UserInformationUrlOverride: "..." # For power users, leave out of configuration for most cases. Not supported by GitHub provider.
```
The following providers use the `RedirectUrl` setting:

- GitHub
- TGForums
- Keycloak
- InvisionCommunity

The following providers use the `ServerUrl` setting:

- Keycloak
- InvisionCommunity

- `Telemetry:DisableVersionReporting`: Prevents you installation and the version you're using from being reported on the source repository's deployments list

- `Telemetry:ServerFriendlyName`: Prevents anonymous TGS version usage statistics from being sent to be displayed on the repository.

- `Telemetry:VersionReportingRepositoryId`: The repository telemetry is sent to. For security reasons, this is not the main TGS repo. See the [tgstation-server-deployments](https://github.com/tgstation/tgstation-server-deployments) repository for more information.

### Database Configuration

If using a MariaDB/MySQL server, our client library [recommends you set 'utf8mb4' as your default charset](https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql#1-recommended-server-charset) disregard at your own risk.

The user created for the application will need the privilege to create databases on the first run, do not create the database for it. Once the initial set of migrations is run, the create right may be revoked. The user should maintain DDL rights though for applying future migrations

Note that the ratio of application installations to databases is 1:1. Do not attempt to share a database amongst multiple TGS installations.

### Starting

For the Windows service version start the `tgstation-server` service. If it fails to start, check the Windows event log under Windows/Application for entries from tgstation-server for errors.

For the console version run `dotnet Tgstation.Server.Host.Console.dll` in the installation directory. The `tgs.bat` and `tgs.sh` shell scripts are shortcuts for this. If on Windows, you must do this as admin to give the server permission to install the required DirectX dependency for certain 512 BYOND versions as well as create symlinks.

For the docker version run `docker start <your container name>`

Test your server is running by visiting the local port in your browser. You should receive a 401 Unauthorized response (You may need to view the developer console). Otherwise an error page will be present.

### Stopping

Normally stopping TGS will terminate DreamDaemon processes. If you need a graceful detach, send command `130` to the Windows service or signal `SIGUSR2` to the Linux dotnet process. Detaching with the Windows console runner is currently not officially supported.

For the Windows service version stop the `tgstation-server` service.

For the SystemD managed service, use `systemctl stop tgstation-server`. DO NOT USE `systemctl kill` as this can create orphaned processes while leaving TGS running.

For the console version press `Ctrl+C` or send a SIGQUIT to the ORIGINAL dotnet process.

For the docker version run `docker stop <your container name>`.

### Updating the Game

## Integrating

tgstation-server provides the DMAPI which can be be integrated into any BYOND codebase for heavily enhanced functionality. The integration process is a fairly simple set of code changes.

1. Copy the [latest release of the DMAPI](https://github.com/tgstation/tgstation-server/releases?q=dmapi&expanded=true) anywhere in your code base. `tgs.dm` can be seperated from the `tgs` folder, but do not modify or move the contents of the `tgs` folder
2. Modify your `.dme`(s) to include the `tgs.dm` and `tgs/includes.dm` files (ORDER OF APPEARANCE IS MANDATORY)
3. Follow the instructions in `tgs.dm` to integrate the API with your codebase.

The DMAPI is fully backwards compatible and should function with any tgstation-server version to date. Updates can be performed in the same manner. Using the `TGS_EXTERNAL_CONFIGURATION` is recommended in order to make the process as easy as replacing `tgs.dm` and the `tgs` folder with a newer version

### Example

Here is a bare minimum example project that implements the essential code changes for integrating the DMAPI

Before `tgs.dm`:
```dm
//Remember, every codebase is different, you probably have better methods for these defines than the ones given here
#define TGS_EXTERNAL_CONFIGURATION
#define TGS_DEFINE_AND_SET_GLOBAL(Name, Value) var/global/##Name = ##Value
#define TGS_READ_GLOBAL(Name) global.##Name
#define TGS_WRITE_GLOBAL(Name, Value) global.##Name = ##Value
#define TGS_WORLD_ANNOUNCE(message) world << ##message
#define TGS_INFO_LOG(message) world.log << "TGS Info: [##message]"
#define TGS_WARNING_LOG(message) world.log << "TGS Warning: [##message]"
#define TGS_ERROR_LOG(message) world.log << "TGS Error: [##message]"
#define TGS_NOTIFY_ADMINS(event) world.log << "TGS Admin Message: [##event]"
#define TGS_CLIENT_COUNT global.client_cout
#define TGS_PROTECT_DATUM(Path) // Leave blank if your codebase doesn't give administrators code reflection capabilities
```

Anywhere else:
```dm
var/global/client_count = 0

/world/New()
	..()
	TgsNew()
	TgsInitializationComplete()

/world/Reboot()
	TgsReboot()
	..()

/world/Topic()
	TGS_TOPIC
	..()

/client/New()
	..()
	++global.client_count

/client/Del()
	..()
	--global.client_count
```

### Remote Access

tgstation-server is an [ASP.Net Core](https://docs.microsoft.com/en-us/aspnet/core/) app based on the Kestrel web server. This section is meant to serve as a general use case overview, but the entire Kestrel configuration can be modified to your liking with the configuration YAML. See [the official documentation](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel) for details.

Exposing the builtin Kestrel server to the internet directly over HTTP is highly not reccommended due to the lack of security. The recommended way to expose tgstation-server to the internet is to host it through a reverse proxy with HTTPS support. Here are some step by step examples to achieve this for major web servers.

System administrators will most likely have their own configuration plans, but here are some basic guides for beginners.

Once complete, test that your configuration worked by visiting your proxy site from a browser on a different computer. You should receive a 401 Unauthorized response.

_NOTE: Your reverse proxy setup may interfere with SSE (Server-Sent Events) which is used for real-time job updates. If you find this to be the case, please open an issue describing what you did to fix it as there may be a way for us to bypass the need for a workaround from our end._

#### IIS (Reccommended for Windows)

1. Acquire an HTTPS certificate. The easiet free way for Windows is [win-acme](https://github.com/PKISharp/win-acme) (requires you to set up the website first)
1. Install the [URL Rewrite Module](https://www.iis.net/downloads/microsoft/url-rewrite)
1. Install the [Application Request Routing Module](https://www.iis.net/downloads/microsoft/application-request-routing)
1. Create a new website, bind it to HTTPS only with your chosen certificate and exposed port. The physical path won't matter since it won't be used. Use `Require Server Name Indication` if you want to limit requests to a specific URL prefix. Do not use the same port as the one TGS is running on.
1. Close and reopen the IIS Manager
1. Open the site and navigate to the `URL Rewrite` module
1. In the `Actions` Pane on the right click `Add Rule(s)...`
1. For the rule template, select `Reverse Proxy` under `Inbound and Outbound Rules` and click `OK`
1. You may get a prompt about enabling proxy functionality. Click `OK`
1. In the window that appears set the `Inbound Rules` textbox to the URL of your tgstation-server i.e. `localhost:5000`. Ensure `Enable SSL Offloading` is checked, then click `OK`

#### Caddy (Reccommended for Linux, or those unfamilar with configuring NGINX or Apache)

1. Setup a basic website configuration. Instructions on how to do so are out of scope.
2. In your Caddyfile, under a server entry, add the following (replace 5000 with the port TGS is hosted on):
```
https://your.site.here {
        reverse_proxy localhost:5000
}
```
3. For this setup, your configuration's `ControlPanel:PublicPath` needs to be blank. If you have a path in `PublicPath`, it needs to be in "reverse_proxy PublicPathHere localhost:5000".

See https://caddyserver.com/docs/caddyfile/directives/reverse_proxy

#### NGINX (Reccommended for Linux)

1. Setup a basic website configuration. Instructions on how to do so are out of scope.
2. Acquire an HTTPS certificate, likely via Let's Encrypt, and configure NGINX to use it.
3. Setup a path under a server like the following (replace 8080 with the port TGS is hosted on):
```
location /tgs {
	proxy_pass http://127.0.0.1:8080;
	break;
}
```

See https://docs.nginx.com/nginx/admin-guide/web-server/reverse-proxy/

#### Apache

1. Ensure the `mod_proxy` extension is installed.
2. Setup a basic website configuration. Instructions on how to do so are out of scope.
3. Acquire an HTTPS certificate, likely via Let's Encrypt, and configure Apache to use it.
4. Under a VirtualHost entry, setup the following (replace 8080 with the port TGS is hosted on):
```
ProxyPass / http://127.0.0.1:8080
ProxyPassReverse / http://127.0.0.1:8080
```

See https://httpd.apache.org/docs/2.4/howto/reverse_proxy.html

Example VirtualHost Entry
```
<IfModule mod_ssl.c>
<VirtualHost *:443>
	ServerName tgs_subdomain.example.com

	SSLEngine on
	SSLCertificateFile /etc/letsencrypt/live/example.com/fullchain.pem
	SSLCertificateKeyFile /etc/letsencrypt/live/example.com/privkey.pem

	ProxyPass / http://127.0.0.1:8080/
	ProxyPassReverse / http://127.0.0.1:8080/
</VirtualHost>
</IfModule>
```

### Swarmed Servers

Multiple tgstation-servers can be linked together in a swarm. The main benefit of this is allowing for users, groups, and permissions to be shared across the servers. Servers in a swarm must connect to the same database, use the same tgstation-server version, and have their own unique names.

In a swarm, one server is designated the 'controller'. This is the server other 'node's in the swarm communicate with and coordinates group updates. Issuing an update command to one server in a swarm will update them all to the specified version.

#### Swarm Server Instances

Instances can be either part of a swarm or not. Once in the database they cannot switch between these states. In order to brin a non-swarmed instance into a swarmed server or vice-versa follow these steps.

1. With the existing server, detach the instance.
1. Shut down the server and configure it to be in or out of swarm mode.
1. Reattach the instance after starting the server again.

## Usage

tgstation-server is controlled via a RESTful HTTP json API. Documentation on this API can be found [here](https://tgstation.github.io/tgstation-server/api.html). This section serves to document the concepts of the server. The API is versioned separately from the release version. A specification for it can be found in the api-vX.X.X git releases/tags.

### Updating TGS

TGS can self update without stopping your DreamDaemon servers. Releases made to this repository are bound by a contract that allows changes of the runtime assemblies without stopping your servers. Database migrations are automatically applied as well. Reverting to lower versions works but only works so far back in time, do so at your own risk (check changes made to `/src/Tgstation.Server.Host/Models/Migrations`).

Major version updates may require additional action on the part of the user (apart from the configuration changes).

#### Linux Notes

If TGS was installed via a package manager, using the TGS self updater will cause the version to change without notifying said package manager. This is not necessarily a problem if you're okay with the deviation.

To avoid this, use the package manager to update TGS. It is just as seamless as the self-updater.

##### apt

```sh
sudo apt update && sudo apt upgrade -y
```

#### Notifications

If a server update is available, it will be indicated in the response from the GET /Administration endpoint and shown as a green exclamation mark in the webpanel navbar. For more active notifications, you can subscribe to [this GitHub discussion](https://github.com/tgstation/tgstation-server/discussions/1322).

### Users

All actions apart from logging in must be taken by a user. TGS installs with one default user whose credentials can be found [here](src/Tgstation.Server.Api/DefaultCredentials.cs). It is recommended to disable this user ASAP as it is used to create Jobs that are started by the server itself. If access to all users is lost, the default user can be reset using the `Database:ResetAdminPassword` configuration setting.

Users can be enabled/disabled and have a very granular set of rights associated to them that determine the actions they are allowed to take (i.e. Modify the user list or create instances). Users can be _database based_ or _system based_. Database users are your standard web users with a username and password. System users, on the otherhand, are authenticated with the host OS. These users cannot have their password or names changed by TGS as they are managed by the system (and in reverse, login tokens don't expire when their password changes). The benefit to having these users is it allows the use of system ACLs for static file control. More on that later.

### Instances

A TGS deployment is made up with a set of instances, which each represent a production BYOND server. As many instances as desired can be created. Be aware, however, due to the nature of BYOND, this will quickly result in system resource exhaustion.

An instance is stored in a single folder anywhere on a system and is made up of several components: The source code git repository, the BYOND installations, the compiler, the watchdog, chat bots, and static file management systems.

##### Instance Users

All users with access to an instance have an InstanceUser object associated with the two that defines more rights specific to that instance (i.e. Deploy code, modify bots, edit other InstanceUsers).

#### Repository

The `Repository` folder is a git repository containing the code of the game you wish to host. It can be cloned from any public or private remote repository and has capabilities to affect changes back to it. All the standard benefits of git are utilized (i.e. check out any revision or reference).

Additional features become available if the remote repository is hosted on https://github.com/. Namely the Test Merge feature, which allows you to take a pull request opened on the repository and compile it into a game deployment for testing. Information about test merges is available in game via the DMAPI and via the main API as well.

Manual operations on the repository while an instance is running may lead to git data corruption. Thankfully, it's simple enough to delete and reclone the repository via the API.

#### Byond

The `Byond` folder contains installations of [BYOND](https://www.byond.com/) or [OpenDream](https://github.com/OpenDreamProject/OpenDream) versions. The version which is used by your game code can be changed on a whim (Note that only versions >= 511.1385 have been thouroughly tested. Lower versions should work but if one doesn't function, please open an issue report) and the server will take care of installing it.

##### Environment Variables

You can specify additional environment variables to launch your server/compiler with by adding `server.env`/`compiler.env` to your engine installation directory (i.e. `<instance>/Byond/515.1530/server.env`). These are [.env](https://hexdocs.pm/dotenvy/dotenv-file-format.html) files.

#### Compiler

The compiler deploys code from the `Repository` folder to the `Game` folder and compiles it either by autodetecting the `.dme` or having it set by configuration. Several other step are also run such as validating the DMAPI version and creating symlinks for static files are done at this point. The compiler also applies server side code modifications and duplicates compiled code for the watchdog as well (See following section).

#### Watchdog

The watchdog is responsible for starting and keeping your server running. It functions by launching two servers which are hot-swapped on `/world/Reboot`s and during crashes to prevent downtime. This hot swapping feature is also what allows TGS to deploy updates to live servers without bringing them down.

DreamDaemon can be finicky and will crash with several high load games or bad DM code. The watchdog has several failure prevention methods to keep at least one server running while these issues are sorted out.

#### Chat Bots

TGS supports creating infinite chat bots for notifying staff or players of things like code deployments and uptime in. Currently the following providers are supported

- Internet Relay Chat (IRC)
- Discord
  - NOTE: Bot MUST have Message Content Intent enabled

More can be added by providing a new implementation of the [IProvider](src/Tgstation.Server.Host/Components/Chat/Providers/IProvider.cs) interface

Bots have a set of built-in commands that can be triggered via `!tgs`, mentioning, or private messaging them. Along with these, custom commands can be defined using the DMAPI by creating a subtype of the `/datum/tgs_chat_command` type (See `tgs.dm` for details). Invocation for custom commands can be restricted to certain channels.

#### Static Files

All files in game code deployments are considered transient by default, meaning when new code is deployed, changes will be lost. Static files allow you to specify which files and folders stick around throughout all deployments.

The `StaticFiles` folder contains 3 root folders which cannot be deleted and operate under special rules
	- `CodeModifications`
	- `EventScripts`
	- `GameStaticFiles`

These files can be modified either in host mode or system user mode. In host mode, TGS itself is responsible for reading and writing the files. In system user mode read and write actions are performed using the system account of the logged on User, enabling the use of ACLs to control access to files. Database users will not be able to use the static file system if this mode is configured for an instance.

This folder may be freely modified manually just beware this may cause in-progress deployments to error if done on Windows systems.

#### CodeModifications

When a deployment is made by the compiler, all the contents of this folder are copied over the repository contents. Then one of two code change modes are selected based on the prescense of certain files.

If `<target dme>.dme` is present, that .dme will be used instead of the repository's `.dme`

Otherwise the files `HeadInclude.dm` and `TailInclude.dm` are searched for and added as include lines to the top and bottom of the target `.dme` repsectively if they exist. These files can contain any valid DreamMaker code (Including `#include`ing other `.dm` files!) allowing you to modify the a repository's code on a per instance basis

#### EventScripts

This folder can contain anything. But, when certain events occur in the instance, TGS will look here for `.bat` or `.sh` files with the same name and run those with corresponding arguments. List of supported events can be found [here](src/Tgstation.Server.Host/Components/Events/EventType.cs).

#### GameStaticFiles

Any files and folders contained in this root level of this folder will be symbolically linked to all deployments at the time they are created. This allows persistent game data (BYOND `.sav`s or code configuration files for example) to persist across all deployments. This folder contains a .tgsignore file which can be used to prevent symlinks from being generated by entering the names of files and folders (1 per line).

This functionality has the following prerequisites:

- You are using Windows.

**OR**

- Your world uses the TGS DreamMaker API.
- Your world runs with the `Trusted` security level.

**OR**

- You are NOT using the basic watchdog.
- The contents of the `GameStaticFiles` directory are on the same filesystem as the instance's `Game` directory.

**OR**

- You are using the basic watchdog.
- Your world runs with the `Trusted` security level.

### Clients

Here are tools for interacting with the TGS web API

- [tgstation-server-webpanel](https://github.com/tgstation/tgstation-server-webpanel): Official client and included with the server. A react web app for using tgstation-server.
- [Tgstation.Server.ControlPanel](https://github.com/tgstation/Tgstation.Server.ControlPanel): Official client. A cross platform GUI for using tgstation-server. Feature complete but lacks OAuth login options.
- [Tgstation.Server.Client](https://www.nuget.org/packages/Tgstation.Server.Client): A nuget .NET Standard 2.0 TAP based library for communicating with tgstation-server. Feature complete.
- [Tgstation.Server.Api](https://www.nuget.org/packages/Tgstation.Server.Api): A nuget .NET Standard 2.0 library containing API definitions for tgstation-server. Feature complete.

Contact project maintainers to get your client added to this list

## Backup/Restore

Note that tgstation-server is NOT a backup solution, the onus is on the server runners.

The `Repository` folder should be backed up on the remote server, do not rely on the instance copy to store changes.

The `BYOND` and `Game` folders should never be backed up due to being intertwined with instance data.

The `Configuration` folder should be fully backed up.

The database should be fully backed up.

To restore an installation from backups, first restore the instance `Configuration` folder in its new home. Then restore the database, modifying the `Path` column in the `Instances` table where necessary to point to the new instances. Then start the server pointed at the new database.

Should you end up with a lost database for some reason or want to reattach a detached instance you can reattach an existing folder by creating an empty file named `TGS4_ALLOW_INSTANCE_ATTACH` inside it (This is automatically created when detaching instances). Then create a new instance with that path, this will bypass the empty folder check. Note that this will not restore things such as user permissions, server config options, or deployment metadata. Those must be reconfigured manually.

## Troubleshooting

Feel free to ask for help [on the discussions page](https://github.com/tgstation/tgstation-server/discussions).

## Contributing

* See [CONTRIBUTING.md](.github/CONTRIBUTING.md)

## Licensing

* The DMAPI for the project is licensed under the MIT license.
* The /tg/station 13 icon is licensed under [Creative Commons 3.0 BY-SA](http://creativecommons.org/licenses/by-sa/3.0/).
* The remainder of the project is licensed under [GNU AGPL v3](http://www.gnu.org/licenses/agpl-3.0.html)

See the files in the `/src/DMAPI` tree for the MIT license
