param(
    [string]$MsiPath = 'packaging/build/package/winget/Tgstation.Server.Host.Service.Msi/Release/tgstation-server.msi'
)

$ErrorActionPreference="Stop"

[XML]$versionXML = Get-Content build/Version.props -ErrorAction Stop

$tgsVersion = $versionXML.Project.PropertyGroup.TgsCoreVersion

$installerHash = Get-FileHash -Path $MsiPath -ErrorAction Stop # SHA256 is the default

cd build/package/winget/manifest

$devHash = 'F749672C1BDBAC8CD8AEF7352B97E54C21CF389D798D0ED245CA75EC72521460'
$devVersion = '0.22.475'
$devReleaseDate = '2023-06-24'
$releaseDate = Get-Date -format "yyyy-MM-dd"

(Get-Content Tgstation.Server.installer.yaml -ErrorAction Stop).Replace($devHash, $installerHash.Hash).Replace($devReleaseDate, $releaseDate).Replace($devVersion, $tgsVersion) | Set-Content Tgstation.Server.installer.yaml -ErrorAction Stop
(Get-Content Tgstation.Server.locale.en-US.yaml -ErrorAction Stop).Replace($devVersion, $tgsVersion) | Set-Content Tgstation.Server.locale.en-US.yaml -ErrorAction Stop
(Get-Content Tgstation.Server.yaml -ErrorAction Stop).Replace($devVersion, $tgsVersion) | Set-Content Tgstation.Server.yaml -ErrorAction Stop

winget validate --manifest .
if (-Not $?) {
    exit $lastexitcode
}

# I know this is late, but we actually need the package online to install it
winget install -m . -h --disable-interactivity
if (-Not $?) {
    exit $lastexitcode
}

wingetcreate submit -t $Env:WINGET_PUSH_TOKEN .
if (-Not $?) {
    exit $lastexitcode
}

cd ../../../..
