$coverageFilePaths = Get-ChildItem -Path TestResults -Filter *.coverage -Recurse -ErrorAction SilentlyContinue -Force | %{ $_.fullname }

$coverageFilePathList = [string]$coverageFilePaths

Write-Host "Running CodeCoverage.exe..."
&"C:\Program Files (x86)\Microsoft Visual Studio\2017\TestAgent\Team Tools\Dynamic Code Coverage Tools\CodeCoverage.exe" analyze /output:service.coveragexml "$coverageFilePath"

rm -r TestResults

codecov -f 'api_coverage.xml client_coverage.xml host_coverage.xml console_coverage.xml watchdog_coverage.xml server_coverage.xml service.coveragexml'
