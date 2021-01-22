#!/bin/sh

SCRIPT_VERSION="1.2.0"

echo "tgstation-server 4 container startup script v$SCRIPT_VERSION"
echo "PWD: $PWD"

PROD_CONFIG=/config_data/appsettings.Production.yml
HOST_CONFIG=/app/appsettings.Production.yml

if [ ! -f $PROD_CONFIG ]; then
	echo "$PROD_CONFIG not detected! Creating empty and running setup wizard..."
	# Important, config reloading doesn't work with symlinks
	export General__SetupWizardMode=Only
	echo "{}" > $PROD_CONFIG
fi

ln -v -s $PROD_CONFIG $HOST_CONFIG

if [ ! -f "$HOST_CONFIG" ]; then
	echo "ln failed to create symlink!"
	exit 1
fi

echo "Executing console runner..."
exec dotnet Tgstation.Server.Host.Console.dll $@
