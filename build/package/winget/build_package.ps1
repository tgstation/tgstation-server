$currentCommit=git rev-parse HEAD

Remove-Item -Recurse -Force -ErrorAction SilentlyContinue packaging

git worktree remove -f packaging

$ErrorActionPreference="Stop"

git worktree add -f packaging $currentCommit
cd packaging
Remove-Item -Recurse -Force .git

[XML]$versionXML = Get-Content build/Version.props
$tgsVersion = $versionXML.Project.PropertyGroup.TgsCoreVersion

cd build/package/winget

$devProductCode = 'D24887FA-3228-4509-B5F3-4E07E349F278'
$devVersion = '0.22.475'

(Get-Content Tgstation.Server.Host.Service.Msi/Tgstation.Server.Host.Service.Msi.vdproj).Replace($devVersion, $tgsVersion) | Set-Content Tgstation.Server.Host.Service.Msi/Tgstation.Server.Host.Service.Msi.vdproj
(Get-Content Tgstation.Server.Host.Service.Msi/Tgstation.Server.Host.Service.Msi.vdproj).Replace($devProductCode, [guid]::NewGuid().ToString().ToUpperInvariant()) | Set-Content Tgstation.Server.Host.Service.Msi/Tgstation.Server.Host.Service.Msi.vdproj

# We make _some_ assumptions
DevEnv installer.sln /Project Tgstation.Server.Host.Service.Msi/Tgstation.Server.Host.Service.Msi.vdproj /build Release

cd ..
