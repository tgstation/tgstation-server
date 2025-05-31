[Base Branch]: # "Please set the base branch of your pull request appropriately. If you only made a patch, code improvement, non-server code change, or comment/documentation update please target the `master` branch. Otherwise, target the `dev` branch."
[Release Notes]: # "Your PR should contain a detailed list of notable changes, titled appropriately. This includes any observable changes to the server or DMAPI. See examples below."

🆑
Description of your change.
Each newline corresponds to a release note in the release your change is included in.
/🆑

🆑
You can also have multiple sets of release notes per pull request.
They will be amalgamated together in the end.
/🆑

🆑 Categories
Categories are used by [the release notes tool](../tools/Tgstation.Server.ReleaseNotes) to generate formatted changelists used in releases.
The default category is Core.
Only one category may be specified for a 🆑 block.
Valid categories are Core, DreamMaker API, REST API, GraphQL API, Host Watchdog, Web Control Panel, Configuration, Nuget: Api, Nuget: Client, and Nuget: Common.
/🆑

[Why]: # "If this does not close or work on an existing GitHub issue, please add a short description [two lines down] of why you think these changes would benefit the server. If you can't justify it in words, it might not be worth adding."
