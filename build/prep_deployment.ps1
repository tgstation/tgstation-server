$env:TGSVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo("$env:APPVEYOR_BUILD_FOLDER/artifacts/ServerHost/Tgstation.Server.Host.dll").FileVersion

Write-Host "TGS Version: $env:TGSVersion"

if (($env:CONFIGURATION -match "Release") -And ($env:APPVEYOR_REPO_BRANCH -match "master") -And ($env:APPVEYOR_REPO_COMMIT_MESSAGE -match "\[TGSDeploy\]")) {
    Write-Host "Deploying..."
    $env:TGSDeploy = "Do it." 

    Write-Host "Generating release notes..."
    $bf = $env:APPVEYOR_BUILD_FOLDER
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
    
    if ($env:APPVEYOR_REPO_COMMIT_MESSAGE -match "\[NugetDeploy\]") {
        $env:NugetDeploy = "Do it."
        Write-Host "Nuget deployment enabled"
    }  
}
