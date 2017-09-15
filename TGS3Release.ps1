$bf = $Env:APPVEYOR_BUILD_FOLDER
$src = "$bf\TGInstallerWrapper\bin\Release"

Remove-Item "$src\Microsoft.Deployment.WindowsInstaller.xml"
Remove-Item "$src\TG Station Server Installer.exe.config"
Remove-Item "$src\TG Station Server Installer.pdb"
Remove-Item "$src\TGServiceInterface.pdb"

$destination = "$bf\TGS3-Server.zip"

If(Test-path $destination) {Remove-item $destination}

Add-Type -assembly "system.io.compression.filesystem"

[io.compression.zipfile]::CreateFromDirectory($src, $destination) 

$destination_md5sha = $Env:APPVEYOR_BUILD_FOLDER + "\MD5-SHA1-Server.txt"

$src2 = $Env:APPVEYOR_BUILD_FOLDER + "\ClientApps"
[system.io.directory]::CreateDirectory($src2)
Copy-Item "$bf\TGCommandLine\bin\Release\TGCommandLine.exe" "$src2\TGCommandLine.exe"
Copy-Item "$bf\TGControlPanel\bin\Release\TGControlPanel.exe" "$src2\TGControlPanel.exe"
Copy-Item "$bf\TGServiceInterface\bin\Release\TGServiceInterface.exe" "$src2\TGServiceInterface.exe"

$dest2 = "$bf\TGS3-Client.zip"

[io.compression.zipfile]::CreateFromDirectory($src2, $dest2) 
$destination_md5sha2 = $Env:APPVEYOR_BUILD_FOLDER + "\MD5-SHA1-Client.txt"

& fciv -both $destination > $destination_md5sha
& fciv -both $dest2 > $destination_md5sha2
