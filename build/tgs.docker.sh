#!/bin/sh

ln -s /config_data/appsettings.Production.json /app/appsettings.Production.json

exec dotnet Tgstation.Server.Host.Console.dll "$@"
