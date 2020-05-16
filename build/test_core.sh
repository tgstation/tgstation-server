#!/bin/bash
set -e

dotnet tool install --global coverlet.console

mkdir TestResults

source ~/.nvm/nvm.sh && nvm install 10

if [[ ! -z "${TGS4_TEST_CONNECTION_STRING}" ]]; then
    build/integration_test.sh
    exit
fi

cd tests/Tgstation.Server.Api.Tests

dotnet build -c $CONFIG /p:CopyLocalLockFileAssemblies=true
$HOME/.dotnet/tools/coverlet bin/$CONFIG/netcoreapp3.1/Tgstation.Server.Api.Tests.dll --target "dotnet" --targetargs "test -c $CONFIG --no-build" --format opencover --output "../../TestResults/api.xml" --include "[Tgstation.Server*]*" --exclude "[Tgstation.Server.Api.Tests*]*"

cd ../Tgstation.Server.Client.Tests

dotnet build -c $CONFIG /p:CopyLocalLockFileAssemblies=true
$HOME/.dotnet/tools/coverlet bin/$CONFIG/netcoreapp3.1/Tgstation.Server.Client.Tests.dll --target "dotnet" --targetargs "test -c $CONFIG --no-build" --format opencover --output "../../TestResults/client.xml" --include "[Tgstation.Server*]*" --exclude "[Tgstation.Server.Client.Tests*]*"

cd ../Tgstation.Server.Host.Tests

dotnet build -c $CONFIG /p:CopyLocalLockFileAssemblies=true
$HOME/.dotnet/tools/coverlet bin/$CONFIG/netcoreapp3.1/Tgstation.Server.Host.Tests.dll --target "dotnet" --targetargs "test -c $CONFIG --no-build" --format opencover --output "../../TestResults/host.xml" --include "[Tgstation.Server*]*" --exclude "[Tgstation.Server.Host.Tests*]*" --exclude "[Tgstation.Server.Host]Tgstation.Server.Host.Database.Migrations.*"

cd ../Tgstation.Server.Host.Watchdog.Tests

dotnet build -c $CONFIG /p:CopyLocalLockFileAssemblies=true
$HOME/.dotnet/tools/coverlet bin/$CONFIG/netcoreapp3.1/Tgstation.Server.Host.Watchdog.Tests.dll --target "dotnet" --targetargs "test -c $CONFIG --no-build" --format opencover --output "../../TestResults/watchdog.xml" --include "[Tgstation.Server*]*" --exclude "[Tgstation.Server.Host.Watchdog.Tests*]*"

cd ../Tgstation.Server.Host.Console.Tests

dotnet build -c $CONFIG /p:CopyLocalLockFileAssemblies=true
$HOME/.dotnet/tools/coverlet bin/$CONFIG/netcoreapp3.1/Tgstation.Server.Host.Console.Tests.dll --target "dotnet" --targetargs "test -c $CONFIG --no-build" --format opencover --output "../../TestResults/console.xml" --include "[Tgstation.Server*]*" --exclude "[Tgstation.Server.Host.Console.Tests*]*"

cd ../../TestResults

bash <(curl -s https://codecov.io/bash) -f api.xml -F unittests
bash <(curl -s https://codecov.io/bash) -f client.xml -F unittests
bash <(curl -s https://codecov.io/bash) -f host.xml -F unittests
bash <(curl -s https://codecov.io/bash) -f watchdog.xml -F unittests
bash <(curl -s https://codecov.io/bash) -f console.xml -F unittests
