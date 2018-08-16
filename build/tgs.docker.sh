#!/bin/sh

mkdir /config_data
cp -r /config_data/* ./

exec dotnet Tgstation.Server.Host.Console.dll "$@"
