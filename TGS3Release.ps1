$src = $Env:APPVEYOR_BUILD_FOLDER + "\TGInstallerWrapper\bin\Release"

Remove-Item $src + "\Microsoft.Deployment.WindowsInstaller.xml"
Remove-Item $src + "\TGInstallerWrapper.exe.config"
Remove-Item $src + "\TGInstallerWrapper.pdb"
Remove-Item $src + "\TGServiceInterface.pdb"

$destination = $Env:APPVEYOR_BUILD_FOLDER + "\TGS3.zip"

If(Test-path $destination) {Remove-item $destination}

Add-Type -assembly "system.io.compression.filesystem"

[io.compression.zipfile]::CreateFromDirectory($src, $destination) 

$destination_md5sha = $Env:APPVEYOR_BUILD_FOLDER + "\MD5-SHA1.txt"

& fciv -both $destination > $destination_md5sha
