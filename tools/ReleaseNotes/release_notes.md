See https://tgstation.github.io/tgstation-server for installation instructions

## [Changelog for 2.x](https://github.com/tgstation/tgstation-server/milestone/13?closed=1)

- Added `/datum/tgs_version` to the DMAPI. `/world/TgsVersion()`, `/world/TgsMaximumAPIVersion()`, and `/world/TgsMinimumAPIVersion()` now return this datum (#827)

## [Changelog for 1.X](https://github.com/tgstation/tgstation-server/milestone/11?closed=1)

- Added the command line setup wizard. No more need to configure `appsettings.Production.json` manually
- You can now ignore files and folder for symlinking in the `GameStaticFiles` directory using the `.tgsignore` file
- If the repository is configured with valid GitHub synchronization credentials, the user account will now post [test merge comments](https://github.com/tgstation/tgstation/pull/40735#issuecomment-427503427) [when deployments involving them happen](https://github.com/tgstation/tgstation/pull/40736#issuecomment-427503428)
- Releases now contain a `.zip` of the latest DMAPI files
- Installing DirectX on Windows is no longer required for BYOND versions >= 512.1452
- Updating the server should no longer timeout clients
- Various kinks were ironed out in internal systems

# [Initial Release](https://github.com/tgstation/tgstation-server/milestone/3?closed=1)