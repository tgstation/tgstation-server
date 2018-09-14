#!/bin/bash
set -e

OpenCover.Console.exe -returntargetcode -register:user -target:"C:/Program Files/dotnet/dotnet.exe" -targetargs:"test -c %CONFIGURATION% --logger:trx;LogFileName=results.trx /p:DebugType=full tests/Tgstation.Server.Api.Tests/Tgstation.Server.Api.Tests.csproj" -filter:"+[Tgstation.Server*]* -[Tgstation.Server.Api.Tests*]*" -output:".\api_coverage.xml" -oldstyle
  - ps: $wc = New-Object 'System.Net.WebClient'
  - ps: $wc.UploadFile("https://ci.appveyor.com/api/testresults/mstest/$($env:APPVEYOR_JOB_ID)", (Resolve-Path .\tests\Tgstation.Server.Api.Tests\TestResults\results.trx))
  - OpenCover.Console.exe -returntargetcode -register:user -target:"C:/Program Files/dotnet/dotnet.exe" -targetargs:"test -c %CONFIGURATION% --logger:trx;LogFileName=results.trx /p:DebugType=full tests/Tgstation.Server.Client.Tests/Tgstation.Server.Client.Tests.csproj" -filter:"+[Tgstation.Server*]* -[Tgstation.Server.Client.Tests*]*" -output:".\client_coverage.xml" -oldstyle
  - ps: $wc = New-Object 'System.Net.WebClient'
  - ps: $wc.UploadFile("https://ci.appveyor.com/api/testresults/mstest/$($env:APPVEYOR_JOB_ID)", (Resolve-Path .\tests\Tgstation.Server.Client.Tests\TestResults\results.trx))
  - OpenCover.Console.exe -returntargetcode -register:user -target:"C:/Program Files/dotnet/dotnet.exe" -targetargs:"test -c %CONFIGURATION% --logger:trx;LogFileName=results.trx /p:DebugType=full tests/Tgstation.Server.Host.Tests/Tgstation.Server.Host.Tests.csproj" -filter:"+[Tgstation.Server*]* -[Tgstation.Server.Host.Tests*]* -[Tgstation.Server.Host]Tgstation.Server.Host.Models.Migrations*" -output:".\host_coverage.xml" -oldstyle
  - ps: $wc = New-Object 'System.Net.WebClient'
  - ps: $wc.UploadFile("https://ci.appveyor.com/api/testresults/mstest/$($env:APPVEYOR_JOB_ID)", (Resolve-Path .\tests\Tgstation.Server.Host.Tests\TestResults\results.trx))
  - OpenCover.Console.exe -returntargetcode -register:user -target:"C:/Program Files/dotnet/dotnet.exe" -targetargs:"test -c %CONFIGURATION% --logger:trx;LogFileName=results.trx /p:DebugType=full tests/Tgstation.Server.Host.Console.Tests/Tgstation.Server.Host.Console.Tests.csproj" -filter:"+[Tgstation.Server*]* -[Tgstation.Server.Host.Console.Tests*]*" -output:".\console_coverage.xml" -oldstyle
  - ps: $wc = New-Object 'System.Net.WebClient'
  - ps: $wc.UploadFile("https://ci.appveyor.com/api/testresults/mstest/$($env:APPVEYOR_JOB_ID)", (Resolve-Path .\tests\Tgstation.Server.Host.Console.Tests\TestResults\results.trx))
  - set path=%ProgramFiles(x86)%\Microsoft Visual Studio\2017\TestAgent\Common7\IDE\CommonExtensions\Microsoft\TestWindow;%path%
  - copy "%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\Extensions\appveyor.*" "%ProgramFiles(x86)%\Microsoft Visual Studio\2017\TestAgent\Common7\IDE\CommonExtensions\Microsoft\TestWindow\Extensions" /y
  - vstest.console /logger:trx;LogFileName=results.trx "tests\Tgstation.Server.Host.Service.Tests\bin\%CONFIGURATION%\Tgstation.Server.Host.Service.Tests.dll" /Enablecodecoverage /inIsolation /Platform:x64
  - ps: $wc = New-Object 'System.Net.WebClient'
  - ps: $wc.UploadFile("https://ci.appveyor.com/api/testresults/mstest/$($env:APPVEYOR_JOB_ID)", (Resolve-Path .\TestResults\results.trx))
  - OpenCover.Console.exe -returntargetcode -register:user -target:"C:/Program Files/dotnet/dotnet.exe" -targetargs:"test -c %CONFIGURATION% --logger:trx;LogFileName=results.trx /p:DebugType=full tests/Tgstation.Server.Host.Watchdog.Tests/Tgstation.Server.Host.Watchdog.Tests.csproj" -filter:"+[Tgstation.Server*]* -[Tgstation.Server.Host.Watchdog.Tests*]*" -output:".\watchdog_coverage.xml" -oldstyle
  - ps: $wc = New-Object 'System.Net.WebClient'
  - ps: $wc.UploadFile("https://ci.appveyor.com/api/testresults/mstest/$($env:APPVEYOR_JOB_ID)", (Resolve-Path .\tests\Tgstation.Server.Host.Watchdog.Tests\TestResults\results.trx))
  - OpenCover.Console.exe -returntargetcode -register:user -target:"C:/Program Files/dotnet/dotnet.exe" -targetargs:"test -c %CONFIGURATION% --logger:trx;LogFileName=results.trx /p:DebugType=full tests/Tgstation.Server.Tests/Tgstation.Server.Tests.csproj" -filter:"+[Tgstation.Server*]* -[Tgstation.Server.Tests*]* -[Tgstation.Server.Host]Tgstation.Server.Host.Models.Migrations..*" -output:".\server_coverage.xml" -oldstyle

dotnet tool install --global coverlet.console

mkdir TestResults

cd tests/Tgstation.Server.Api.Tests

coverlet bin/$CONFIG/netcoreapp2.0/Tgstation.Server.Api.Tests.dll --target "dotnet" --targetargs "test -c $CONFIG" --format opencover --output "../../TestResults/api.xml" --exclude "[Tgstation.Server.Api.Tests*]*"

cd ../Tgstation.Server.Client.Tests

coverlet bin/$CONFIG/netcoreapp2.0/Tgstation.Server.Client.Tests.dll --target "dotnet" --targetargs "test -c $CONFIG" --format opencover --output "../../TestResults/client.xml" --exclude "[Tgstation.Server.Client.Tests*]*"

cd ../Tgstation.Server.Host.Tests

coverlet bin/$CONFIG/netcoreapp2.0/Tgstation.Server.Host.Tests.dll --target "dotnet" --targetargs "test -c $CONFIG" --format opencover --output "../../TestResults/host.xml" --exclude "[Tgstation.Server.Host.Tests*]*" --exclude "[Tgstation.Server.Host]Tgstation.Server.Host.Models.Migrations.*"

cd ../Tgstation.Server.Host.Watchdog.Tests

coverlet bin/$CONFIG/netcoreapp2.0/Tgstation.Server.Host.Watchdog.Tests.dll --target "dotnet" --targetargs "test -c $CONFIG" --format opencover --output "../../TestResults/watchdog.xml" --exclude "[Tgstation.Server.Host.Watchdog.Tests*]*"

cd ../Tgstation.Server.Host.Console.Tests

coverlet bin/$CONFIG/netcoreapp2.0/Tgstation.Server.Host.Console.Tests.dll --target "dotnet" --targetargs "test -c $CONFIG" --format opencover --output "../../TestResults/console.xml" --exclude "[Tgstation.Server.Host.Console.Tests*]*"

export TGS4_TEST_DATABASE_TYPE=MySql
export TGS4_TEST_CONNECTION_STRING="server=127.0.0.1;uid=root;pwd=;database=tgs_test"
#token set in CI settings
coverlet bin/$CONFIG/netcoreapp2.0/Tgstation.Server.Tests.dll --target "dotnet" --targetargs "test -c $CONFIG" --format opencover --output "../../TestResults/server.xml" --exclude "[Tgstation.Server.Tests*]*" --exclude "[Tgstation.Server.Host]Tgstation.Server.Host.Models.Migrations.*"

cd ../../TestResults

bash <(curl -s https://codecov.io/bash) -f api.xml -F unittests
bash <(curl -s https://codecov.io/bash) -f client.xml -F unittests
bash <(curl -s https://codecov.io/bash) -f host.xml -F unittests
bash <(curl -s https://codecov.io/bash) -f watchdog.xml -F unittests
bash <(curl -s https://codecov.io/bash) -f console.xml -F unittests
bash <(curl -s https://codecov.io/bash) -f server.xml -F integration
