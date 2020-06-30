#!/bin/bash
set -e

# Needed so gcore can work
echo 0 | sudo tee /proc/sys/kernel/yama/ptrace_scope

export TGS4_TEST_DISCORD_CHANNEL=493119635319947269
export TGS4_TEST_IRC_CHANNEL=\#botbus
export TGS4_TEST_TEMP_DIRECTORY=~/tgs4_test
#token set in CI settings

cd tests/Tgstation.Server.Tests

dotnet test --collect:"XPlat Code Coverage" -c $CONFIG --settings "../../build/coverlet.runsettings"--logger:"console;noprogress=true" -r .TestResults

cd TestResults

bash <(curl -s https://codecov.io/bash) -f coverage.opencover.xml -F integration
