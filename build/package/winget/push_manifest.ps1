$ErrorActionPreference="Stop"

[XML]$versionXML = Get-Content build/Version.props -ErrorAction Stop

$tgsVersion = $versionXML.Project.PropertyGroup.TgsCoreVersion

mkdir artifacts
$previousProgressPreference = $ProgressPreference
$ProgressPreference = 'SilentlyContinue'
try
{
    Invoke-WebRequest -Uri "https://github.com/tgstation/tgstation-server/releases/download/tgstation-server-v$tgsVersion/tgstation-server-installer.exe" -OutFile "artifacts/tgstation-server-installer.exe"
} finally {
    $ProgressPreference = $previousProgressPreference
}

$installerHash = Get-FileHash -Path "artifacts/tgstation-server-installer.exe" -ErrorAction Stop # SHA256 is the default

cd build/package/winget/manifest
try
{
    $devHash = 'CF0D4D2FD042098D826A226A05A9DAA5C792318CF48FD334F7FC06AF6A8A23B1'
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

    wingetcreate submit -t $Env:WINGET_PUSH_TOKEN .
    if (-Not $?) {
        exit $lastexitcode
    }
}
finally
{
    cd ../../../..
}
