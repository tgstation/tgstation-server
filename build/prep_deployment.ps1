$bf = $env:APPVEYOR_BUILD_FOLDER
[XML]$versionXML = Get-Content "$bf/build/Version.props"
$env:TGSVersion = $versionXML.Project.PropertyGroup.TgsCoreVersion
$env:APIVersion = $versionXML.Project.PropertyGroup.TgsApiVersion
$env:DMVersion = $versionXML.Project.PropertyGroup.TgsDmapiVersion

Write-Host "TGS Version: $env:TGSVersion"

if (($env:CONFIGURATION -match "Release") -And ($env:APPVEYOR_REPO_BRANCH -match "master") -And ($env:APPVEYOR_REPO_COMMIT_MESSAGE -match "\[TGSDeploy\]")) {
    Write-Host "Deploying TGS..."
    $env:TGSDeploy = "Do it." 

    Write-Host "Generating release notes..."
    dotnet run -p "$bf/tools/ReleaseNotes" $env:TGSVersion
    $env:TGSDraftNotes = !($?)
    $releaseNotesPath = "$bf/release_notes.md"
    Write-Host "Reading release notes from $releaseNotesPath..."
    if (Test-Path $releaseNotesPath -PathType Leaf) {
        $env:TGSReleaseNotes = [IO.File]::ReadAllText($releaseNotesPath)
    }
    else {
        Write-Host "Release note generation failed, release will be created as a draft!"
        $env:TGSReleaseNotes = "Automatic generation failed, please fill manually!"
    }
}

if (($env:CONFIGURATION -match "Release") -And ($env:APPVEYOR_REPO_COMMIT_MESSAGE -match "\[APIDeploy\]")) {
    Write-Host "Deploying API..."
    $env:APIDeploy = "Do it." 
    $env:APIReleaseNotes = "# tgstation-server 4 API v$env:APIVersion"
}

if (($env:CONFIGURATION -match "Release") -And ($env:APPVEYOR_REPO_COMMIT_MESSAGE -match "\[DMDeploy\]")) {
    Write-Host "Deploying DMAPI..."
    $env:DMDeploy = "Do it." 
    $env:DMReleaseNotes = "# tgstation-server 4 DMAPI v$env:DMReleaseNotes"
}
    
if (($env:CONFIGURATION -match "Release") -And ($env:APPVEYOR_REPO_COMMIT_MESSAGE -match "\[NugetDeploy\]")) {
    $env:NugetDeploy = "Do it."
    Write-Host "Nuget deployment enabled"
}  