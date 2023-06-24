$currentCommit=git rev-parse HEAD

Remove-Item -Recurse -Force packaging

git worktree remove -f packaging

$ErrorActionPreference="Stop"

git worktree add -f packaging $currentCommit
cd packaging
Remove-Item -Recurse -Force .git

[XML]$versionXML = Get-Content build/Version.props
$tgsVersion = $versionXML.Project.PropertyGroup.TgsCoreVersion

(Get-Content build/package/winget/Tgstation.Server.Host.Service.Msi/Tgstation.Server.Host.Service.Msi.vdproj).Replace('22.47.5', $tgsVersion) | Set-Content build/package/winget/Tgstation.Server.Host.Service.Msi/Tgstation.Server.Host.Service.Msi.vdproj

# We make _some_ assumptions
DevEnv tgstation-server.sln /Project build/package/winget/Tgstation.Server.Host.Service.Msi/Tgstation.Server.Host.Service.Msi.vdproj /build Release

cd ..
