$coverageFilePaths = Get-ChildItem -Path TestResults -Filter *.coverage -Recurse -ErrorAction SilentlyContinue -Force | %{ $_.fullname }

$coverageFilePathList = [string]$coverageFilePaths

Write-Host "Running CodeCoverage.exe on $coverageFilePathList"
&"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\Team Tools\Dynamic Code Coverage Tools\CodeCoverage.exe" analyze /output:service.coveragexml "$coverageFilePathList"

codecov -f api_coverage.xml --flag unittests
codecov -f client_coverage.xml --flag unittests
codecov -f host_coverage.xml  --flag unittests
codecov -f console_coverage.xml --flag unittests
codecov -f watchdog_coverage.xml --flag unittests
codecov -f service.coveragexml --flag unittests
codecov -f server_coverage.xml --flag integration
