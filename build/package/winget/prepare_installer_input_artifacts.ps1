# Note: This script requires that Tgstation.Server.Host and Tgstation.Server.Host.Service be built in Release configuration beforehand

$ErrorActionPreference="stop"

$startDirectory=$pwd
try
{
    Remove-Item -Recurse -Force artifacts -ErrorAction SilentlyContinue
    Remove-Item -Recurse -Force build/package/winget/Tgstation.Server.Host.Service.Wix/bin -ErrorAction SilentlyContinue
    Remove-Item -Recurse -Force build/package/winget/Tgstation.Server.Host.Service.Wix/obj -ErrorAction SilentlyContinue
    Remove-Item -Recurse -Force build/package/winget/Tgstation.Server.Host.Service.Wix.Bundle/bin -ErrorAction SilentlyContinue
    Remove-Item -Recurse -Force build/package/winget/Tgstation.Server.Host.Service.Wix.Bundle/obj -ErrorAction SilentlyContinue

    [XML]$versionXML = Get-Content build/Version.props -ErrorAction Stop
    $redistUrl = $versionXML.Project.PropertyGroup.TgsDotnetRedistUrl

    $dbRedistVersion = $versionXML.Project.PropertyGroup.TgsMariaDBRedistVersion

    $dbRedistMinorVersion = $dbRedistVersion.Substring(0, $dbRedistVersion.LastIndexOf("."))

    $ProgressPreference = 'SilentlyContinue'
    try
    {
        $json=Invoke-RestMethod -Uri "https://downloads.mariadb.org/rest-api/mariadb/${dbRedistMinorVersion}/"
    } finally {
        $ProgressPreference = $previousProgressPreference
    }

    $msiFile = $json.releases.$dbRedistVersion.files | Where-Object { $_.package_type -eq "MSI Package" } | Select-Object -First 1
    $dbRedistUrl = $msiFile.file_download_url

    mkdir artifacts
    $previousProgressPreference = $ProgressPreference
    $ProgressPreference = 'SilentlyContinue'
    try
    {
        Invoke-WebRequest -Uri $redistUrl -OutFile artifacts/hosting-bundle.exe
        try
        {
            Invoke-WebRequest -Uri $dbRedistUrl -OutFile artifacts/mariadb.msi
        }
        catch
        {
            $dbRedistUrl = $versionXML.Project.PropertyGroup.TgsMariaDBFallbackRedist
            Invoke-WebRequest -Uri $dbRedistUrl -OutFile artifacts/mariadb.msi
        }
    } finally {
        $ProgressPreference = $previousProgressPreference
    }

    # Generating icons
    Set-Location src/Tgstation.Server.Common
    dotnet msbuild -t:IconGeneration

    Set-Location  ../Tgstation.Server.Host
    dotnet publish -c Release --no-build -o ../../artifacts/Tgstation.Server.Host
    if (-Not $?) {
        exit $lastexitcode
    }

    cd ../Tgstation.Server.Host.Service

    dotnet publish -c Release --no-build -o ../../artifacts/Tgstation.Server.Host.Service
    if (-Not $?) {
        exit $lastexitcode
    }

    cd ../..
    build/RemoveUnsupportedRuntimes.sh artifacts/Tgstation.Server.Host
    build/RemoveUnsupportedServiceRuntimes.ps1 artifacts/Tgstation.Server.Host.Service
    mv artifacts/Tgstation.Server.Host.Service/Tgstation.Server.Host.Service.exe artifacts/
}
finally
{
    cd $startDirectory
}
