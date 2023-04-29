# tgstation-server

![CI](https://github.com/tgstation/tgstation-server/workflows/CI/badge.svg) [![codecov](https://codecov.io/gh/tgstation/tgstation-server/branch/master/graph/badge.svg)](https://codecov.io/gh/tgstation/tgstation-server)

[![GitHub license](https://img.shields.io/github/license/tgstation/tgstation-server.svg)](LICENSE) [![Average time to resolve an issue](http://isitmaintained.com/badge/resolution/tgstation/tgstation-server.svg)](http://isitmaintained.com/project/tgstation/tgstation-server "Average time to resolve an issue") [![NuGet version](https://img.shields.io/nuget/v/Tgstation.Server.Api.svg)](https://www.nuget.org/packages/Tgstation.Server.Api) [![NuGet version](https://img.shields.io/nuget/v/Tgstation.Server.Client.svg)](https://www.nuget.org/packages/Tgstation.Server.Client)

[![forthebadge](http://forthebadge.com/images/badges/made-with-c-sharp.svg)](http://forthebadge.com) [![forinfinityandbyond](https://user-images.githubusercontent.com/5211576/29499758-4efff304-85e6-11e7-8267-62919c3688a9.gif)](https://www.reddit.com/r/SS13/comments/5oplxp/what_is_the_main_problem_with_byond_as_an_engine/dclbu1a)

[![forthebadge](http://forthebadge.com/images/badges/built-with-love.svg)](http://forthebadge.com) [![forthebadge](http://forthebadge.com/images/badges/60-percent-of-the-time-works-every-time.svg)](http://forthebadge.com)

This is a toolset to manage production BYOND servers. It includes the ability to update the server without having to stop or shutdown the server (the update will take effect on a "reboot" of the server), the ability to start the server and restart it if it crashes, as well as systems for managing code and game files, and merging GitHub Pull Requests for test deployments.

### Legacy Servers

Older server versions can be found in the V# branches of this repository. Note that the current server fully incompatible with installations before version 4. Only some static files may be copied over: https://github.com/tgstation/tgstation-server#static-files

## Setup

### Pre-Requisites

- [ASP .NET Core Runtime (>= v6.0)](https://dotnet.microsoft.com/download/dotnet/6.0) (Choose the option to `Run Server Apps` for your system) If you plan to install tgstation-server as a Windows service, you should also ensure that your .NET Framework runtime version is >= v4.7.2 (Download can be found on same page). Ensure that the `dotnet` executable file is in your system's `PATH` variable (or that of the user's that will be running the server).
- A [MariaDB](https://downloads.mariadb.org/), MySQL, [PostgresSQL](https://www.postgresql.org/download/), or [Microsoft SQL Server](https://www.microsoft.com/en-us/download/details.aspx?id=55994) database engine is required

### Installation

1. [Download the latest release .zip](https://github.com/tgstation/tgstation-server/releases/latest). The `ServerService` package will only work on Windows. Choose `ServerConsole` if that is not your target OS or you prefer not to use the Windows service.
2. Extract the .zip file to where you want the server to run from. Note the account running the server must have write and delete access to the `lib` subdirectory.

#### Windows

If you wish to install the TGS as a service, run `Tgstation.Server.Host.Service.exe`. It should prompt you to install it. Click `Yes` and accept a potential UAC elevation prompt and the setup wizard should run.

Should you want a clean start, be sure to first uninstall the service by running `Tgstation.Server.Host.Service.exe -u` from the command line.

If using the console version, run ./tgs.bat in the root of the installation directory. Ctrl+C will close the server, terminating all live game instances.


#### Linux (Native)

We recommend using Docker for Linux installations, see below. The content of this parent section may be skipped if you choose to do so.

The following dependencies are required to run tgstation-server on Linux alongside the .NET Core runtime

- libc6-i386
- libstdc++6:i386
- libssl1.0.0
- gdb (for using gcore to create core dumps)
- gcc-multilib (Only on 64-bit systems)

To launch the server, run ./tgs.sh in the root of the installation directory. The process will run in a blocking fashion. SIGQUIT will close the server, terminating all live game instances.

Note that tgstation-server has only ever been tested on Linux via it's [docker environment](build/Dockerfile#L22). If you are having trouble with something in a native installation, or figure out a required workaround, please contact project maintainers so this documentation may be better updated.

#### Docker (Linux)

tgstation-server supports running in a docker container and is the recommended deployment method for Linux systems. The official image repository is located at https://hub.docker.com/r/tgstation/server. It can also be built locally by running `docker build . -f build/Dockerfile -t <your tag name>` in the repository root.

To create a container run
```sh
docker run \
	-ti \ # Start with interactive terminal the first time to run the setup wizard
	--restart=always \ # Recommended for maximum uptime
	--network="host" \ # Not recommended, eases networking setup if your sql server is on the same machine
	--name="tgs" \ # Name for the container
	--cap-add=sys_nice \ # Recommended, allows TGS to lower the niceness of child processes if it sees fit
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

### Configuring

The first time you run TGS you should be prompted with a configuration wizard which will guide you through setting up your `appsettings.Production.yml`

This wizard will, generally, run whenever the server is launched without detecting the config yml. Follow the instructions below to perform this process manually.

#### Configuration Methods

There are 3 primary supported ways to configure TGS:

- Modify the `appsettings.Production.yml` file (Recommended).
- Set environment variables in the form `Section__Subsection=value` or `Section__ArraySubsection__0=value` for arrays.
- Set command line arguments in the form `--Section:Subsection=value` or `--Section:ArraySubsection:0=value` for arrays.

The latter two are not recommended as they cannot be dynamically changed at runtime. See more on ASP.NET core configuration [here](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-6.0).

#### Manual Configuration

Create an `appsettings.Production.yml` file next to `appsettings.yml`. This will override the default settings in `appsettings.yml` with your production settings. There are a few keys meant to be changed by hosts. Modifying any config files while the server is running will trigger a safe restart (Keeps DreamDaemon instances running). Note these are all case-sensitive:

- `General:ConfigVersion`: Suppresses warnings about out of date config versions. You should change this after updating TGS to one with a new config version. The current version can be found on the releases page for your server version.

- `General:MinimumPasswordLength`: Minimum password length requirement for database users

- `General:ValidInstancePaths`: Array meant to limit the directories in which instances may be created.

- `General:UserLimit`: Maximum number of users that may be created

- `General:InstanceLimit`: Maximum number of instances that may be created

- `General:GitHubAccessToken`: Specify a GitHub personal access token with no scopes here to highly mitigate the possiblity of 429 response codes from GitHub requests

- `General:SkipAddingByondFirewallException`: Set to `true` if you have Windows firewall disabled

- `Session:HighPriorityLiveDreamDaemon`: Boolean controlling if live DreamDaemon instances get set to above normal priority processes.

- `Session:LowPriorityDeploymentProcesses `: Boolean controlling if DreamMaker and API validation DreamDaemon instances get set to below normal priority processes.

- `FileLogging:Directory`: Override the default directory where server logs are stored. Default is C:/ProgramData/tgstation-server/logs on Windows, /usr/share/tgstation-server/logs otherwise

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

- `Swarm`: This section should be left `null` unless using the server swarm system. If this is to happen, ensure all swarm servers are set to connect to the same database.

- `Swarm:PrivateKey`: Should be a secure string set identically on all swarmed servers.

- `Swarm:ControllerAddress`: Should be set on all swarmed servers that are **not** the controller server and should be an address the controller server may be reached at.

- `Swarm:Address`: Should be set on all swarmed servers. Should be an address the server can be reached at by other servers in the swarm.

- `Swarm:Identifier` should be set uniquely on all swarmed servers. Used to identify the current server. This is also used to select which instances exist on the current machine and should not be changed post-setup.

- `Swarm:UpdateRequiredNodeCount` should be set to the total number of servers in your swarm, minus the controller. Prevents updates from occurring unless the non-controller server count in the swarm is greater than or equal to this value.

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

Note that the live detach for DreamDaemon servers is only supported for updates or restarts via the API at this time. Stopping tgstation-server will TERMINATE ALL CHILD DREAMDAEMON SERVERS.

For the Windows service version stop the `tgstation-server` service

For the console version press `Ctrl+C` or send a SIGQUIT to the ORIGINAL dotnet process

For the docker version run `docker stop <your container name>`

### Updating

## Integrating

tgstation-server provides the DMAPI which can be be integrated into any BYOND codebase for heavily enhanced functionality. The integration process is a fairly simple set of code changes.

1. Copy the [latest release of the DMAPI](https://github.com/tgstation/tgstation-server/releases) anywhere in your code base. `tgs.dm` can be seperated from the `tgs` folder, but do not modify or move the contents of the `tgs` folder
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

Once complete, test that your configuration worked by visiting your proxy site from a browser on a different computer. You should recieve a 401 Unauthorized response.

#### IIS (Reccommended for Windows)

1. Acquire an HTTPS certificate. The easiet free way for Windows is [win-acme](https://github.com/PKISharp/win-acme) (requires you to set up the website first)
2. Install the [Web Platform Installer](https://www.microsoft.com/web/downloads/platform.aspx)
3. Open the web platform installer in the IIS Manager and install the Application Request Routing 3.0 module
4. Create a new website, bind it to HTTPS only with your chosen certificate and exposed port. The physical path won't matter since it won't be used. Use `Require Server Name Indication` if you want to limit requests to a specific URL prefix. Do not use the same port as the one TGS is running on.
5. Close and reopen the IIS Manager
5. Open the site and navigate to the `URL Rewrite` module
6. In the `Actions` Pane on the right click `Add Rule(s)...`
7. For the rule template, select `Reverse Proxy` under `Inbound and Outbound Rules` and click `OK`
8. You may get a prompt about enabling proxy functionality. Click `OK`
9. In the window that appears set the `Inbound Rules` textbox to the URL of your tgstation-server i.e. `http://localhost:5000`. Ensure `Enable SSL Offloading` is checked, then click `OK`

#### Caddy (Reccommended for Linux, or those unfamilar with configuring NGINX or Apache)

1. Setup a basic website configuration. Instructions on how to do so are out of scope.
2. In your Caddyfile, under a server entry, add the following (replace 8080 with the port TGS is hosted on):
```
proxy /tgs localhost:8080 {
	transparent
}
```

See https://caddyserver.com/docs/proxy

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

### Updating

TGS can self update without stopping your DreamDaemon servers. Releases made to this repository are bound by a contract that allows changes of the runtime assemblies without stopping your servers. Database migrations are automatically applied as well. Because of this REVERTING TO LOWER VERSIONS IS NOT OFFICIALLY SUPPORTED, do so at your own risk (check changes made to `/src/Tgstation.Server.Host/Models/Migrations`).

Major version updates may require additional action on the part of the user (apart from the configuration changes).

#### Notifications

If a server update is available, it will be indicated in the response from the GET /Administration endpoint. For more active notifications, you can subscribe to [this GitHub discussion](https://github.com/tgstation/tgstation-server/discussions/1322).

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

The `Byond` folder contains installations of [BYOND](https://secure.byond.com/) versions. The version which is used by your game code can be changed on a whim (Note that only versions >= 511.1385 have been thouroughly tested. Lower versions should work but if one doesn't function, please open an issue report) and the server will take care of installing it.

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

Any files and folders contained in this root level of this folder will be symbolically linked to all deployments at the time they are created. This allows persistent game data (BYOND `.sav`s or code configuration files for example) to persist across all deployments. This folder contains a .tgsignore file which can be used to prevent symlinks from being generated by entering the names of files and folders (1 per line)

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

