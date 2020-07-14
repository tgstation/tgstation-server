#!/bin/bash
set -e

# Needed so gcore can work
echo 0 | sudo tee /proc/sys/kernel/yama/ptrace_scope

export TGS4_TEST_DISCORD_CHANNEL=493119635319947269
export TGS4_TEST_IRC_CHANNEL=\#botbus
export TGS4_TEST_TEMP_DIRECTORY=~/tgs4_test
#token set in CI settings

cd tests/Tgstation.Server.Tests

dotnet build -c $CONFIG
sudo dotnet test Tgstation.Server.Tests.csproj -l "console;verbosity=detailed" --no-build -c $CONFIG /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:CoverletOutput='./integration_test.xml'

bash <(curl -s https://codecov.io/bash) -f integration_test.xml -F integration
