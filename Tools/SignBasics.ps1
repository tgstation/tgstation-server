$bf = $Env:APPVEYOR_BUILD_FOLDER

function CodeSign
{
	param($file)
	&'C:\Program Files (x86)\Microsoft SDKs\ClickOnce\SignTool\signtool.exe' sign /f "$bf/Tools/tgstation13.org.pfx" /p "$env:snk_passphrase" /t http://timestamp.verisign.com/scripts/timstamp.dll "$file"
}

#Sign the output files
if (Test-Path env:snk_passphrase)
{
	CodeSign "$bf/TGCommandLine/bin/Release/TGCommandLine.exe"
	CodeSign "$bf/TGControlPanel/bin/Release/TGControlPanel.exe"
	CodeSign "$bf/TGServerService/bin/Release/TGServerService.exe"
	CodeSign "$bf/TGDreamDaemonBridge/bin/x86/Release/TGDreamDaemonBridge.dll"
	CodeSign "$bf/TGServiceInterface/bin/Release/TGServiceInterface.dll"
}
