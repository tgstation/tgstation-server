#!/bin/sh

cp -r /config_data/* ./

exec dotnet Tgstation.Server.Host.Console.dll "$@"
