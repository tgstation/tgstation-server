$currentCommit=git rev-parse HEAD

Remove-Item -Recurse -Force -ErrorAction SilentlyContinue packaging

git worktree remove -f packaging

git worktree add -f packaging $currentCommit
cd packaging
Remove-Item -Recurse -Force .git

dotnet restore

[XML]$versionXML = Get-Content build/Version.props -ErrorAction Stop

$tgsVersion = $versionXML.Project.PropertyGroup.TgsCoreVersion

cd build/package/winget

$devVersion = '0.22.475'

(Get-Content Tgstation.Server.Host.Service.Msi/Tgstation.Server.Host.Service.Msi.vdproj -ErrorAction Stop).Replace($devVersion, $tgsVersion) | Set-Content Tgstation.Server.Host.Service.Msi/Tgstation.Server.Host.Service.Msi.vdproj -ErrorAction Stop

# We make _some_ assumptions
DevEnv installer.sln /Project Tgstation.Server.Host.Service.Msi/Tgstation.Server.Host.Service.Msi.vdproj /build Release
if (-Not $?) {
    exit $lastexitcode
}

cd ../../../..
