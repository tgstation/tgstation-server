$coverageFilePath = Resolve-Path -path "TestResults\*\*.coverage"
 
$coverageFilePath = $coverageFilePath.ToString()

Write-Host "Running CodeCoverage.exe..."
&"C:\Program Files (x86)\Microsoft Visual Studio\2017\TestAgent\Team Tools\Dynamic Code Coverage Tools\CodeCoverage.exe" analyze /output:coverage.coveragexml "$coverageFilePath"

rm -r TestResults

codecov -f coverage.coveragexml
