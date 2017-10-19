$bf = $Env:APPVEYOR_BUILD_FOLDER
$src = "$bf\TGInstallerWrapper\bin\x86\Release"
$version = [System.Diagnostics.FileVersionInfo]::GetVersionInfo("$src\TG Station Server Installer.exe").FileVersion

$doxdir = "C:\tgsdox"

New-Item -Path $doxdir -ItemType directory

Add-Content "$bf\Tools\Doxyfile" "`nPROJECT_NUMBER = $version`nINPUT = $bf`nOUTPUT_DIRECTORY = $doxdir`nPROJECT_LOGO = $bf/tgs.ico"
doxygen.exe "$bf\Tools\Doxyfile"

$destination = "$bf\TGS3-Server-v$version.exe"

Move-Item -Path "$src\TG Station Server Installer.exe" -Destination "$destination"

Add-Type -assembly "system.io.compression.filesystem"

$destination_md5sha = $Env:APPVEYOR_BUILD_FOLDER + "\MD5-SHA1-Server-v$version.txt"

$src2 = $Env:APPVEYOR_BUILD_FOLDER + "\ClientApps"
[system.io.directory]::CreateDirectory($src2)
Copy-Item "$bf\TGCommandLine\bin\x86\Release\TGCommandLine.exe" "$src2\TGCommandLine.exe"
Copy-Item "$bf\TGControlPanel\bin\x86\Release\TGControlPanel.exe" "$src2\TGControlPanel.exe"
Copy-Item "$bf\TGServiceInterface\bin\x86\Release\TGServiceInterface.dll" "$src2\TGServiceInterface.dll"

$dest2 = "$bf\TGS3-Client-v$version.zip"

[io.compression.zipfile]::CreateFromDirectory($src2, $dest2) 
$destination_md5sha2 = $Env:APPVEYOR_BUILD_FOLDER + "\MD5-SHA1-Client-v$version.txt"

& fciv -both $destination > $destination_md5sha
& fciv -both $dest2 > $destination_md5sha2
