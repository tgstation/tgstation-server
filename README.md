# tgstation-server v4:

[![Build status](https://ci.appveyor.com/api/projects/status/7t1h7bvuha0p9j5f/branch/master?svg=true)](https://ci.appveyor.com/project/Cyberboss/tgstation-server-tools/branch/master) [![Build Status](https://travis-ci.org/tgstation/tgstation-server.svg?branch=master)](https://travis-ci.org/tgstation/tgstation-server) [![codecov](https://codecov.io/gh/tgstation/tgstation-server/branch/master/graph/badge.svg)](https://codecov.io/gh/tgstation/tgstation-server) [![Waffle.io - Columns and their card count](https://badge.waffle.io/tgstation/tgstation-server.png?columns=all)](https://waffle.io/tgstation/tgstation-server?utm_source=badge)

[![GitHub license](https://img.shields.io/github/license/tgstation/tgstation-server.svg)](https://github.com/tgstation/tgstation-server/blob/master/LICENSE) [![Average time to resolve an issue](http://isitmaintained.com/badge/resolution/tgstation/tgstation-server.svg)](http://isitmaintained.com/project/tgstation/tgstation-server "Average time to resolve an issue") [![NuGet version](https://img.shields.io/nuget/v/TGServiceInterface.svg)](https://www.nuget.org/packages/TGServiceInterface)

[![forthebadge](http://forthebadge.com/images/badges/made-with-c-sharp.svg)](http://forthebadge.com) [![forinfinityandbyond](https://user-images.githubusercontent.com/5211576/29499758-4efff304-85e6-11e7-8267-62919c3688a9.gif)](https://www.reddit.com/r/SS13/comments/5oplxp/what_is_the_main_problem_with_byond_as_an_engine/dclbu1a)

[![forthebadge](http://forthebadge.com/images/badges/built-with-love.svg)](http://forthebadge.com) [![forthebadge](http://forthebadge.com/images/badges/60-percent-of-the-time-works-every-time.svg)](http://forthebadge.com)


This is a toolset to manage a production BYOND server. It includes the ability to update the server without having to stop or shutdown the server (the update will take effect on a "reboot" of the server) the ability start the server and restart it if it crashes, as well as systems for fixing errors and merging GitHub Pull Requests locally.
  
Generally, updates force a live tracking of the configured git repo, resetting local modifications. If you plan to make modifications, set up a new git repo to store your version of the code in, and point this script to that in the config (explained below). This can be on GitHub or a local repo using file:/// urls.

### Legacy Servers
* Versions 3 and 4 can be found in the `legacy/` directory

## CONTRIBUTING

* See [CONTRIBUTING.md](https://github.com/tgstation/tgstation-server/blob/master/.github/CONTRIBUTING.md)

## LICENSING

* The DM API for the project is licensed under the MIT license.
* The /tg/station 13 icon is licensed under [Creative Commons 3.0 BY-SA](http://creativecommons.org/licenses/by-sa/3.0/).
* The remainder of the project is licensed under [GNU AGPL v3](http://www.gnu.org/licenses/agpl-3.0.html)

See the bottom of each file in the /DMAPI tree for the MIT license
