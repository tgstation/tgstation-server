mkdir Temp
cd Temp
nuget install Microsoft.CodeCoverage --version 16.4.0
cd ..

$coverageFilePaths = Get-ChildItem -Path TestResults -Filter *.coverage -Recurse -ErrorAction SilentlyContinue -Force | %{ $_.fullname }

$coverageFilePathList = [string]$coverageFilePaths

Write-Host "Running CodeCoverage.exe on: $coverageFilePathList"
&"Temp\Microsoft.CodeCoverage.16.4.0\build\netstandard1.0\CodeCoverage\CodeCoverage.exe" analyze /output:service.coveragexml /verbose "$coverageFilePathList"

rm -r TestResults

codecov -f api_coverage.xml --flag unittests
codecov -f client_coverage.xml --flag unittests
codecov -f host_coverage.xml  --flag unittests
codecov -f console_coverage.xml --flag unittests
codecov -f watchdog_coverage.xml --flag unittests
codecov -f service.coveragexml --flag unittests
codecov -f server_coverage.xml --flag integration
