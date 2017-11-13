$bf = $Env:APPVEYOR_BUILD_FOLDER

function CodeSign
{
	param($file)
	&'C:\Program Files (x86)\Microsoft SDKs\ClickOnce\SignTool\signtool.exe' sign /f "$bf/Tools/tgstation13.org.pfx" /p "$env:snk_passphrase" /t http://timestamp.verisign.com/scripts/timstamp.dll "$file"
}

#Sign the output files
if (Test-Path env:snk_passphrase)
{
	CodeSign "$bf/TGS.CommandLine/bin/Release/TGCommandLine.exe"
	CodeSign "$bf/TGS.ControlPanel/bin/Release/TGControlPanel.exe"
	CodeSign "$bf/TGS.Server.Service/bin/Release/TGServerService.exe"
	CodeSign "$bf/TGS.Interface.Bridge/bin/x86/Release/TGDreamDaemonBridge.dll"
	CodeSign "$bf/TGS.Interface/bin/Release/TGServiceInterface.dll"
}
