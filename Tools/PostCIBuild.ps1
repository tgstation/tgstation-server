$bf = $Env:APPVEYOR_BUILD_FOLDER


function CodeSign
{
	param($file)
	&'C:\Program Files (x86)\Microsoft SDKs\ClickOnce\SignTool\signtool.exe' sign /f "$bf/Tools/tgstation13.org.pfx" /p "$env:snk_passphrase" /t http://timestamp.verisign.com/scripts/timstamp.dll "$file"
}

#Sign the output files
if (Test-Path env:snk_passphrase)
{
	CodeSign "$bf/TGServiceTests/bin/Release/TGServiceTests.dll"
	CodeSign "$bf/TGInstallerWrapper/bin/Release/TG Station Server Installer.exe"
	$env:snk_passphrase = ""
}

$src = "$bf\TGInstallerWrapper\bin\Release"
$version = [System.Diagnostics.FileVersionInfo]::GetVersionInfo("$src\TG Station Server Installer.exe").FileVersion

$destination = "$bf\TGS3-Server-v$version.exe"

Move-Item -Path "$src\TG Station Server Installer.exe" -Destination "$destination"

Add-Type -assembly "system.io.compression.filesystem"

$destination_md5sha = "$bf\MD5-SHA1-Server-v$version.txt"

$src2 = "$bf\ClientApps"
[system.io.directory]::CreateDirectory($src2)
Copy-Item "$bf\TGCommandLine\bin\Release\TGCommandLine.exe" "$src2\TGCommandLine.exe"
Copy-Item "$bf\TGControlPanel\bin\Release\TGControlPanel.exe" "$src2\TGControlPanel.exe"
Copy-Item "$bf\TGServiceInterface\bin\Release\TGServiceInterface.dll" "$src2\TGServiceInterface.dll"

$dest2 = "$bf\TGS3-Client-v$version.zip"

[io.compression.zipfile]::CreateFromDirectory($src2, $dest2) 
$destination_md5sha2 = "$bf\MD5-SHA1-Client-v$version.txt"

& fciv -both $destination > $destination_md5sha
& fciv -both $dest2 > $destination_md5sha2
