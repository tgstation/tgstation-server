#!/bin/sh

$SCRIPT_VERSION="1.1.0"

echo "tgstation-server 4 container startup script v$SCRIPT_VERSION"
echo "PWD: $PWD"

PROD_CONFIG=/config_data/appsettings.Production.json
HOST_CONFIG=/app/appsettings.Production.json

if [ ! -f $PROD_CONFIG ]; then
	echo "$PROD_CONFIG not detected! Creating empty..."
	echo "{}" > $PROD_CONFIG
fi

echo "Linking $PROD_CONFIG to $HOST_CONFIG"
ln -s $PROD_CONFIG $HOST_CONFIG

echo "Executing console runner..."
exec dotnet Tgstation.Server.Host.Console.dll
