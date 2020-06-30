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
dotnet test --collect:"XPlat Code Coverage" -c $CONFIG --settings "../../build/coverlet.runsettings" --logger:"console;noprogress=true" -r ../../TestResults
mv ../../TestResults/coverage.opencover.xml api.xml

cd ../Tgstation.Server.Client.Tests
dotnet test --collect:"XPlat Code Coverage" -c $CONFIG --settings "../../build/coverlet.runsettings"--logger:"console;noprogress=true" -r ../../TestResults
mv ../../TestResults/coverage.opencover.xml client.xml

cd ../Tgstation.Server.Host.Tests
dotnet test --collect:"XPlat Code Coverage" -c $CONFIG --settings "../../build/coverlet.runsettings"--logger:"console;noprogress=true" -r ../../TestResults
mv ../../TestResults/coverage.opencover.xml host.xml

cd ../Tgstation.Server.Host.Watchdog.Tests
dotnet test --collect:"XPlat Code Coverage" -c $CONFIG --settings "../../build/coverlet.runsettings"--logger:"console;noprogress=true" -r ../../TestResults
mv ../../TestResults/coverage.opencover.xml watchdog.xml

cd ../Tgstation.Server.Host.Console.Tests
dotnet test --collect:"XPlat Code Coverage" -c $CONFIG --settings "../../build/coverlet.runsettings"--logger:"console;noprogress=true" -r ../../TestResults
mv ../../TestResults/coverage.opencover.xml console.xml

cd ../../TestResults

bash <(curl -s https://codecov.io/bash) -f api.xml -F unittests
bash <(curl -s https://codecov.io/bash) -f client.xml -F unittests
bash <(curl -s https://codecov.io/bash) -f host.xml -F unittests
bash <(curl -s https://codecov.io/bash) -f watchdog.xml -F unittests
bash <(curl -s https://codecov.io/bash) -f console.xml -F unittests
