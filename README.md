# Tgstation Toolkit:

[![Build status](https://ci.appveyor.com/api/projects/status/7t1h7bvuha0p9j5f/branch/master?svg=true)](https://ci.appveyor.com/project/Cyberboss/tgstation-server-tools/branch/master) [![Build Status](https://travis-ci.org/tgstation/tgstation-server.svg?branch=master)](https://travis-ci.org/tgstation/tgstation-server) [![codecov](https://codecov.io/gh/tgstation/tgstation-server/branch/master/graph/badge.svg)](https://codecov.io/gh/tgstation/tgstation-server) [![Waffle.io - Columns and their card count](https://badge.waffle.io/tgstation/tgstation-server.png?columns=all)](https://waffle.io/tgstation/tgstation-server?utm_source=badge)

[![GitHub license](https://img.shields.io/github/license/tgstation/tgstation-server.svg)](https://github.com/tgstation/tgstation-server/blob/master/LICENSE) [![Average time to resolve an issue](http://isitmaintained.com/badge/resolution/tgstation/tgstation-server.svg)](http://isitmaintained.com/project/tgstation/tgstation-server "Average time to resolve an issue") [![NuGet version](https://img.shields.io/nuget/v/TGServiceInterface.svg)](https://www.nuget.org/packages/TGServiceInterface)

[![forthebadge](http://forthebadge.com/images/badges/made-with-c-sharp.svg)](http://forthebadge.com) [![forinfinityandbyond](https://user-images.githubusercontent.com/5211576/29499758-4efff304-85e6-11e7-8267-62919c3688a9.gif)](https://www.reddit.com/r/SS13/comments/5oplxp/what_is_the_main_problem_with_byond_as_an_engine/dclbu1a)

[![forthebadge](http://forthebadge.com/images/badges/built-with-love.svg)](http://forthebadge.com) [![forthebadge](http://forthebadge.com/images/badges/60-percent-of-the-time-works-every-time.svg)](http://forthebadge.com)


This is a toolset to manage a production server of /tg/Station13 (and its forks). It includes the ability to update the server without having to stop or shutdown the server (the update will take effect next round) the ability start the server and restart it if it crashes, as well as systems for fixing errors and merging GitHub Pull Requests locally.
  
Generally, updates force a live tracking of the configured git repo, resetting local modifications. If you plan to make modifications, set up a new git repo to store your version of the code in, and point this script to that in the config (explained below). This can be on github or a local repo using file:/// urls.

Requires python 2.7/3.6 to be installed for changelog generation

### Legacy Server
* The old cmd script server can be found in the legacy tree


## Installing
1. Either compile from source (requires .NET Framework 4.5.2, Nuget, and WiX toolset 4.0) or download the latest [release from github](https://github.com/tgstation/tgstation-server-tools/releases)
1. Unzip setup files
1. Run the installer or use the console server

## Installing (GUI):
1. Launch `TGControlPanel.exe` as an administrator. A shortcut can be found on your desktop
1. Use the `Create Instance` button to create a new server instance
1. Go to the `Repository` Tab and set the remote address and branch of the git you with to track. Note: To grab the tgstation repo, use the following URL: git://github.com/tgstation/tgstation.git
1. Hit the clone button
1. While waiting go to the `BYOND` tab and install the BYOND version you wish
1. You may also configure an IRC and/or discord bot for the server on the chat tab
1. Once the clone is complete you may set up a committer identity, user name, and password on the `Repository` tab for pushing changelog updates
1. Go to the `Server` tab and click the `Initialize Game Folders` button
1. Optionally change the `Project Path` Setting from tgstation to wherever the dme/dmb pair are in your repository
1. Optionally tick the Autostart box if you wish to have your server start with Windows
1. When game folder initialization is complete, click the `Copy from Repo and Compile` option

## Installing (CL example):
This process is identical to the above steps in command line mode. You can always learn more about a command using `?` i.e. `repo ?`
1. Launch TGCommandLine.exe as an administrator (running with no parameters puts you in interactive mode)
1. `service set-python-path C:\Python27`
1. `service create-instance "TGS" D:\tgstation`
1. `instance` And enter `TGS`. If you aren't using interactive mode, the following commands must be suffixed with `--instance TGS`
1. `repo setup git://github.com/tgstation/tgstation.git master`
1. `byond update 511.1385`
1. `irc nick TGS3Test`
1. `irc set-auth-mode channel-mode`
1. `irc set-auth-level %`
1. `irc setup-auth NickServ "id hunter2"` Yes this is the real password, please use it only for testing
1. `irc join botbus dev`
1. `irc join botbus admin`
1. `irc join botbus wd`
1. `irc join botbus game`
1. `irc enable`
1. `discord set-token Rjfa93jlksjfj934jlkjasf8a08wfl.asdjfj08e44` See https://discordapp.com/developers/docs/topics/oauth2#bots
1. `discord set-auth-mode role-id`
1. `discord addmin 192837419273409` See how to get a role id: https://www.reddit.com/r/discordapp/comments/5bezg2/role_id/. Note that if you `discord set-auth-mode user-id` you'll need to use user ids (Enable developer mode, right click user, `Copy ID`)
1. `discord join 12341234453235 dev` This is a channel id (Enable developer mode, right click channel, `Copy ID`)
1. `discord join 34563456344245 dev`
1. `discord join 23452362574456 admin`
1. `discord join 23452362574456 wd`
1. `discord join 53457345736788 game`
1. `discord enable`
1. `repo set-name tgstation-server` These two lines specify the changelog's committer identity. They are not mirrored in the GUI
1. `repo set-email tgstation-server@tgstation13.org`
1. `repo set-credentials` And follow the prompts
1. `dm project-name tgstation`
1. `dd autostart on`
1. `repo status` To check the clone job status
1. `dm initialize --wait`
1. `dd start`

## Setting up Multi-User

1. Create windows accounts for those you wish to have access to the service
1. Join them in a common windows group
1. Run TGCommandLine.exe as an administrator
1. `admin set-group <Name of the group you created> --instance "<instance name>"`

Note: Due to internal functionality, a user who has access to at least one server instance will be able to view the metadata (Name, path, Logging ID, and Enabled status) of all instances.

## Setting up Remote access

1. Obtain an SSL certificate to secure the connection (this is beyond the scope of this guide)
1. Either stick with the default port `38607` or change it with `admin set-port <port #>`
1. [Bind the SSL certificate to the port](https://docs.microsoft.com/en-us/dotnet/framework/wcf/feature-details/how-to-configure-a-port-with-an-ssl-certificate)
	- e.g. `netsh http add sslcert ipport=0.0.0.0:<port #> certhash=<certificate hash> appid={F32EDA25-0855-411C-AF5E-F0D042917E2D}`
	- The `appid` GUID actually doesn't matter, but for sanity, you should use the GUID of TGServerService.exe as printed above
	- Power shell users remember to quit the appid: `netsh http add sslcert ipport=0.0.0.0:<port #> certhash=<certificate hash> appid="{F32EDA25-0855-411C-AF5E-F0D042917E2D}"` as {} has special meaning in powershell
1. Ensure the port can be acccessed from the internet
1. Log in from any computer using a username and password from the service computer in either the CLI or GUI

## Updating

The service supports updates while running a DreamDaemon instance. Simply install an updated version as you normally would and process ownership will transfer smoothly to the new service.

### Folders and Files (None of these should be touched):
* `Game/<A/B>/`
	* This will house two copies of the game code, one for updating and one for live. When updating, it will automatically swap them.

* `Static/`
	* This contains the `data/` and `config/` folders from the code. They are stored here and a symbolic link is created in the `Game` folders pointing to here.
	* Resetting the repository will create a backup of this and reinitalize it

* `Game/Live/`
	* This is a symbolic link pointing to current "live" folder.
	* When the server is updated, we just point this to the updating folder so that the update takes place next round.

* `Diagnostics`
	* This contains various timestamped diagnostic information for DreamDaemon invocations

* `Repository/`
	* This contains the actual git repository, all changes in here will be overwritten during update operations.

* `RepoKey/`
	* This contains ssh key information for automatic changelog pushing. 

* `EventHandlers/`
	* This contains batch files that run after certain events, currently only precompile and postcompile events are implemented. If you'd like an event handler you should create a file in this folder with the following name: `{event_name}.bat` for example: if I want an event handler for precompile, I would name it `precompile.bat`.

* `BYOND/`
	* This contains the actual BYOND installation the server uses

* `BYOND_staging/`
	* This appears when a BYOND update is queued but can't currently be applied due to usage of the current BYOND version. It will be applied at the first possible moment. Restarting the service deletes this folder

* `BYOND_revision.zip`
	* This is a queued update downloaded from BYOND, it will be unzipped into BYOND_staging and deleted. Restarting the service deletes this file

* `TGDreamDaemonBridge.dll`
	* This is the .dll the TGS3 API `call()()`s into to RPC the server instance that runs it
	* Instance -> DreamDaemon communication is achieved via `world/Topic()`

* `prtestjob.json`
	* This contains information about current test merged pull requests in the Repository folder
	
* `TGS3.json`
	* This is a copy of `TGS3.json` from the Repository. If a repostory change creates differences between the two, update operations will be blocked until the user confirms they want to change it

* `Instance.json`
	* The internal configuration settings for a server instance. Note that access to this file bypasses API user restrictions

### Codebase integration
To get the TGS3 API for your code base, import the 3 .dm files in the `DMAPI` folder into your include structure, then fill out the configuration as documented in the comments of server_tools.dm. Then, you may want to add a `TGS3.json` file to specify any static directories and .dlls your codebase uses, along with the optional changelog compile options. Any changes to `TGS3.json` will block compiler operations until a server operator manually approves them.

### Starting the game server:
To run the game server, open the `Server` tab of the control panel and click either `Start`

It will restart the game server if it shutdowns for any reason, giving up after 5 tries in under a minute

### Updating the server:
To update the server, open the `Server` tab of the control panel. Click `Update Server`. (it will git pull, compile, all that jazz). Note that this button won't clear test merges.  

(Note: Updating automatically does a code reset, clearing ALL changes to the local git repo, including manual changes (This will not change any data in the `Static/` folder))  

Updates do not require the server to be shutdown, changes will apply next round if the server is currently running.  

All DM compilation will log to the server what commit they happened at and create a backup tag in the local repository.

### Locally merge GitHub Pull Requests (PR test merge):
This feature currently only works if github is the remote (git server).

Running these will merge the pull request then recompile the server, changes take effect the next round if the server is currently running.  

There are two flavors to this.
* The manual method is to use the repo page's merge PR button for fine grain control over PR merging. Each time this button is pressed, the latest commit of the specified PR# will be merged into the repo
* The managed method is the Server page's `Test Merge Manager` which uses the GitHub API to get information about available PRs.
	* Open PRs are listed by default
	* Currently test merged PRs are checked off and listed at the top when it's opened
* If a PR contains a label that contains the text `test` (case-insensitive) it will be listed on top and marked as `TESTING REQUESTED`
	* If a PR has been updated since it was test merged, two entries for it will appear. One listed as `OUTDATED` and specifying the commit it was merged at
	* You can change the initial update action of the server with the radio buttons in the bottom left. `Update to Remote` fully resets and updates the server before merging PRs. `Update To Origin` does the same based off the local repository's `origin` remote. `No Update` will merge the PRs without any prior action
	* Checking off PRs here and then hitting apply will run the selected update action, optionally generate and push a changelog (see below), merge the PRs, and then compile. `Update Server` will keep any active merged PRs until they are manually removed or merged on the `origin` remote.
	* Given that the control panel uses GitHub's API to populate the `Test Merge Manager` you may be prompted for your credentials if you make too many requests. This will create a personal access token with public access on your account specifying its use to bypass the GitHub API rate limit.

You can clear all active test merges using `Reset to Origin Branch` in the `Repository` tab and the using `Copy from Repo and Compile` in the server tab (explained below). You can also use the `Reset All Test Merges` button on the server tab for a concise solution.

### The Compiler
* `Server` -> `Copy from Repo and Compile`
	* Copies the local repository code, compiles it, and stages it to apply next round
* `Server` -> `Initialize Game Folders`
	* Requires the server not be running, rebuilds the `Game` folder and then does `Copy from Repo and Compile`
	* Required on first setup
 
### Starting everything when the computer/server boots
Just tick the `Autostart` option in the `Server` tab or run `dd autostart on` on the command line. As it's a windows service, it will automatically run without having to log ing

### Enabling the BYOND Webclient
Just tick the `Webclient` option in the `Server` tab or run `dd webclient on` on the command line.

### Static Configuration
* The `Static Files` page of the control panel lets you modify all files in directories you specified in `TGS3.json`
* The files are modified here using the Windows credentials of the active user, feel free to manually set ACLs on them
* You may optionally rebuild the entire `Static` folder from the repository using the `Recreate Static Directory` button. This will copy the original files from the repository based on the current `TGS3.json`. This requires DreamDaemon not be running

### Modifying Your Code

Any `.dm` files included in the root level of the `Static` directory are automatically copied over and included before anything else in your `.dme` for compilation. Use this to configure compile options as you see fit.

### Moving, Renaming, and Detaching instances

* Instances can be renamed, but this requires a temporary offlining of them (this includes interface access, DreamDaemon, and chat bots)
* Detaching an instance simply removes it from the main server's configuration and control, leaving it free for the user to manipulate
* Importing an instance will work as long as
	1. No other instance currently in the server has the same name
	1. It is not the same path as another instance
	Note that importing an instance as a different windows user (this is always different across machines) will result in a loss of chat configuration due to the encryption scheme

### Viewing Server Logs
* Service logs are stored in the Windows event viewer under `Windows Logs` -> `Application`. You'll need to filter this list for `TG Station Server`
* Every event type is keyed with an ID. A complete listing of these IDs and their purpose can be found [here](https://github.com/tgstation/tgstation-server/blob/master/TGS.Server/EventID.cs). Event IDs from different instances are offset by the logging ID the instance was assigned when it was started.
* You can also import the custom view `View TGS3 Logs.xml` in this folder to have them automatically filtered
* Servers running in console mode only use std_out

### Enabling upstream changelog generation
* The repository will automatically create an ssh version of the initial origin remote and can optionally push generated changelogs to your git through it
* The repository can only authenticate using ssh public key authentication
* To enable this feature, simply create `public_key.txt` and `private_key.txt` in a folder called RepoKey in the server directory
* The private key must be in `-----BEGIN RSA PRIVATE KEY-----` format and the public key must be in `ssh-rsa` format. You can generate a keypair like this using the converter in the dropdown menu of PuTTYGen. See github guidelines for setting this up here: https://help.github.com/articles/connecting-to-github-with-ssh/
* The server will be able to read these files regardless of their permissions, so the responsibility is on you to set their ACL's so they can't be read by those that shouldn't

### Synchronized test merge commits
* An instance can push branchless test merge commits to GitHub if the `RepoKey` folder is setup
* This pushes all test merge commits to the remote branch `___TGS3TempBranch` and then deletes it
* This provides public reference information about the test merge commit and time since it will appear in the PR in questing
* To enable this, check the `Sync Commits` button on the `Repository` page of the control panel or run `repo push-testmerges on --instance "<instance name>"` from TGCommandLine

## CONTRIBUTING

* See [CONTRIBUTING.md](https://github.com/tgstation/tgstation-server/blob/master/.github/CONTRIBUTING.md)

## LICENSING

* The DM API for the project is licensed under the MIT license.
* The /tg/station 13 icon is licensed under [Creative Commons 3.0 BY-SA](http://creativecommons.org/licenses/by-sa/3.0/).
* The remainder of the project is licensed under [GNU AGPL v3](http://www.gnu.org/licenses/agpl-3.0.html)

See the bottom of each file in the /DMAPI tree for the MIT license
