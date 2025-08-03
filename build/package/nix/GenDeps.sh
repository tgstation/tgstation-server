#!/bin/sh

cd ../../../src/Tgstation.Server.Host.Console
dotnet restore --packages out
nuget-to-json out > ../../build/package/nix/deps.json
rm -rf out
