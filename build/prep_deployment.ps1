$env:TGSVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo("$env:APPVEYOR_BUILD_FOLDER/artifacts/ServerHost/Tgstation.Server.Host.dll").FileVersion
if (($env:CONFIGURATION -match "Release") -And ($env:APPVEYOR_REPO_BRANCH -match "master")) {
    if ($env:APPVEYOR_REPO_COMMIT_MESSAGE -match "\[TGSDeploy\]") {
        $env:TGSDeploy = "Do it." 
    }
    if ($env:APPVEYOR_REPO_COMMIT_MESSAGE -match "\[NugetDeploy\]") {
        $env:NugetDeploy = "Do it."
    }
}

dotnet run -p "$env:APPVEYOR_BUILD_FOLDER/tools/ReleaseNotes" 4.0.2.0 --no-close
$env:TGSGoodNotes = $?
$releaseNotesPath = "$env:APPVEYOR_BUILD_FOLDER/tools/ReleaseNotes/release_notes.md"
if (Test-Path $releaseNotesPath -PathType Leaf) {
    $env:TGSReleaseNotes = [IO.File]::ReadAllText($releaseNotesPath)
}
else {
    $env:TGSReleaseNotes = "Automatic generation failed, please fill manually!"
}
