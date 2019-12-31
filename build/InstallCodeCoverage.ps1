New-Item -Type Directory -Force -Path "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\Team Tools\Dynamic Code Coverage Tools"

mkdir C:\CodeCoverage
pushd C:\CodeCoverage
nuget install Microsoft.CodeCoverage -Version 16.4.0
cmd /c mklink "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\Team Tools\Dynamic Code Coverage Tools\CodeCoverage.exe" "Microsoft.CodeCoverage.16.4.0\build\netstandard1.0\CodeCoverage\CodeCoverage.exe"
popd

