$coverageFilePath = Resolve-Path -path "TestResults\*\*.coverage"
 
$coverageFilePath = $coverageFilePath.ToString()

Write-Host "Running CodeCoverage.exe..."
&"C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\Team Tools\Dynamic Code Coverage Tools\CodeCoverage.exe" analyze /output:coverage.coveragexml "$coverageFilePath"

rm -r TestResults

Write-Host "Downloading PathCapitalizationCorrector v0.1.4..."
appveyor DownloadFile https://github.com/Cyberboss/PathCapitalizationCorrector/releases/download/0.1.4/PathCapitalizationCorrector.exe

Write-Host "Fixing Window's terrible case ignorance..."
&"./PathCapitalizationCorrector.exe" coverage.coveragexml
codecov -f coverage.coveragexml
