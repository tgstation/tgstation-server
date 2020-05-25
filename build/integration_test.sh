#!/bin/bash
set -e

export TGS4_TEST_DISCORD_CHANNEL=493119635319947269
export TGS4_TEST_IRC_CHANNEL=\#botbus
export TGS4_TEST_TEMP_DIRECTORY=~/tgs4_test
#token set in CI settings

cd tests/Tgstation.Server.Tests

dotnet build -c $CONFIG

$HOME/.dotnet/tools/coverlet bin/$CONFIG/netcoreapp3.1/Tgstation.Server.Tests.dll --target "dotnet" --targetargs "test -c $CONFIG --no-build" --format opencover --output "../../TestResults/integration_test.xml" --include "[Tgstation.Server*]*" --exclude "[Tgstation.Server.Tests*]*" --exclude "[Tgstation.Server.Host]Tgstation.Server.Host.Database.Migrations.*"

cd ../../TestResults

bash <(curl -s https://codecov.io/bash) -f integration_test.xml -F integration
