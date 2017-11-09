if (-not (Test-Path env:snk_passphrase))
{
	exit
}

$bf = $Env:APPVEYOR_BUILD_FOLDER

$flags = [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::Exportable -bor [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::PersistKeySet

$cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2 -ArgumentList "$bf/Tools/TGStationServer3.pfx", $env:snk_passphrase, $flags

$provider = [System.Security.Cryptography.RSACryptoServiceProvider]$cert.PrivateKey;

$rawstring = $provider.ExportCspBlob($true)

[System.IO.File]::WriteAllBytes("$bf/Tools/TGStationServer3.snk", $rawstring);

&'C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.1 Tools\sn.exe' -i "$bf/Tools/TGStationServer3.snk" TGStationServer3

Remove-Item "$bf/Tools/TGStationServer3.snk"

$env:snk_passphrase = ""
