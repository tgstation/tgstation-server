#!/bin/bash
set -e

dotnet tool install --global coverlet.console

mkdir TestResults

cd tests/Tgstation.Server.Api.Tests

dotnet build -c $CONFIG
$HOME/.dotnet/tools/coverlet bin/$CONFIG/netcoreapp2.0/Tgstation.Server.Api.Tests.dll --target "dotnet" --targetargs "test -c $CONFIG --no-build" --format opencover --output "../../TestResults/api.xml" --include "[Tgstation.Server*]*" --exclude "[Tgstation.Server.Api.Tests*]*"

cd ../Tgstation.Server.Client.Tests

dotnet build -c $CONFIG
$HOME/.dotnet/tools/coverlet bin/$CONFIG/netcoreapp2.0/Tgstation.Server.Client.Tests.dll --target "dotnet" --targetargs "test -c $CONFIG --no-build" --format opencover --output "../../TestResults/client.xml" --include "[Tgstation.Server*]*" --exclude "[Tgstation.Server.Client.Tests*]*"

cd ../Tgstation.Server.Host.Tests

dotnet build -c $CONFIG
$HOME/.dotnet/tools/coverlet bin/$CONFIG/netcoreapp2.0/Tgstation.Server.Host.Tests.dll --target "dotnet" --targetargs "test -c $CONFIG --no-build" --format opencover --output "../../TestResults/host.xml" --include "[Tgstation.Server*]*" --exclude "[Tgstation.Server.Host.Tests*]*" --exclude "[Tgstation.Server.Host]Tgstation.Server.Host.Models.Migrations.*"

cd ../Tgstation.Server.Host.Watchdog.Tests

dotnet build -c $CONFIG
$HOME/.dotnet/tools/coverlet bin/$CONFIG/netcoreapp2.0/Tgstation.Server.Host.Watchdog.Tests.dll --target "dotnet" --targetargs "test -c $CONFIG --no-build" --format opencover --output "../../TestResults/watchdog.xml" --include "[Tgstation.Server*]*" --exclude "[Tgstation.Server.Host.Watchdog.Tests*]*"

cd ../Tgstation.Server.Host.Console.Tests

dotnet build -c $CONFIG
$HOME/.dotnet/tools/coverlet bin/$CONFIG/netcoreapp2.0/Tgstation.Server.Host.Console.Tests.dll --target "dotnet" --targetargs "test -c $CONFIG --no-build" --format opencover --output "../../TestResults/console.xml" --include "[Tgstation.Server*]*" --exclude "[Tgstation.Server.Host.Console.Tests*]*"

export TGS4_TEST_DATABASE_TYPE=MySql
export TGS4_TEST_CONNECTION_STRING="server=127.0.0.1;uid=root;pwd=;database=tgs_test"
#token set in CI settings
dotnet build -c $CONFIG
$HOME/.dotnet/tools/coverlet bin/$CONFIG/netcoreapp2.0/Tgstation.Server.Tests.dll --target "dotnet" --targetargs "test -c $CONFIG --no-build" --format opencover --output "../../TestResults/server.xml" --include "[Tgstation.Server*]*" --exclude "[Tgstation.Server.Tests*]*" --exclude "[Tgstation.Server.Host]Tgstation.Server.Host.Models.Migrations.*"

cd ../../TestResults

bash <(curl -s https://codecov.io/bash) -f api.xml -F unittests
bash <(curl -s https://codecov.io/bash) -f client.xml -F unittests
bash <(curl -s https://codecov.io/bash) -f host.xml -F unittests
bash <(curl -s https://codecov.io/bash) -f watchdog.xml -F unittests
bash <(curl -s https://codecov.io/bash) -f console.xml -F unittests
bash <(curl -s https://codecov.io/bash) -f server.xml -F integration
