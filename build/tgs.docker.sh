#!/bin/sh

PROD_CONFIG=/config_data/appsettings.Production.json
if [ ! -f $PROD_CONFIG ]; then
	echo "{}" > $PROD_CONFIG
fi
ln -s $PROD_CONFIG /app/appsettings.Production.json

exec dotnet Tgstation.Server.Host.Console.dll
