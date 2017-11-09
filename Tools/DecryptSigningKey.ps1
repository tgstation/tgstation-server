if (-not (Test-Path env:snk_passphrase))
{
	exit
}

$bf = $Env:APPVEYOR_BUILD_FOLDER

$flags = [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::Exportable -bor [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::PersistKeySet

$cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2 -ArgumentList "$bf/Tools/TGStationServer3.pfx", $env:snk_passphrase, $flags

$env:snk_passphrase = ""

$provider = [System.Security.Cryptography.RSACryptoServiceProvider]$cert.PrivateKey;

$rawstring = $provider.ExportCspBlob($true)

[System.IO.File]::WriteAllBytes("$bf/Tools/TGStationServer3.snk", $rawstring);

&'C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.1 Tools\sn.exe' -i "$bf/Tools/TGStationServer3.snk" TGStationServer3

#sign the discord binaries while we're here

function ReplaceTextInFile
{
	param($text, $replacement, $file)
	(Get-Content -Raw "$file").replace($text, $replacement) | Set-Content "$file"
}

function SignDLL
{
	param($path, $depends)

	&'C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.1 Tools\ildasm.exe' "$path" /OUTPUT="$path.il" > $null
	
	foreach ($dep in $depends) {
		$ILPKTokStr = "`r`n  .publickeytoken = (15 21 FB 3F E7 6E D9 10 )"
		ReplaceTextInFile ".assembly extern $dep`r`n{" ".assembly extern $dep`r`n{$ILPKTokStr" "$path.il"
	}

	&'C:\Windows\Microsoft.NET\Framework\v4.0.30319\ilasm.exe' "$path.il" /DLL /OUTPUT="$path" /KEY="$bf/Tools/TGStationServer3.snk" > $null
	Write-Host "Signed $path"
}

SignDLL "$bf/packages/Discord.Net.Core.1.0.2/lib/net45/Discord.Net.Core.dll" @()
SignDLL "$bf/packages/Discord.Net.Rest.1.0.2/lib/net45/Discord.Net.Rest.dll" @("Discord.Net.Core")
SignDLL "$bf/packages/Discord.Net.WebSocket.1.0.2/lib/net45/Discord.Net.WebSocket.dll" @("Discord.Net.Core", "Discord.Net.Rest")

Remove-Item "$bf/Tools/TGStationServer3.snk"

#Replace bad references with our PKTok 1521fb3fe76ed910
ReplaceTextInFile 'PublicKeyToken=null' 'PublicKeyToken=1521fb3fe76ed910' "$bf/TGServerService/TGServerService.csproj"
