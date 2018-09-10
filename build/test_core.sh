#!/bin/bash
set -e

cd tests/Tgstation.Server.Api.Tests

dotnet test -c $CONFIG

cd ../Tgstation.Server.Client.Tests

dotnet test -c $CONFIG

cd ../Tgstation.Server.Host.Tests

dotnet test -c $CONFIG

cd ../Tgstation.Server.Host.Watchdog.Tests

dotnet test -c $CONFIG

cd ../Tgstation.Server.Host.Console.Tests

dotnet test -c $CONFIG

cd ../Tgstation.Server.Tests

export TGS4_TEST_DATABASE_TYPE=MySql
export TGS4_TEST_CONNECTION_STRING="server=127.0.0.1;uid=root;pwd=;database=tgs_test"
#token set in CI settings
dotnet test -c $CONFIG
