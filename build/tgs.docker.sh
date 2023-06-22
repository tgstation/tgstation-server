#!/bin/sh

SCRIPT_VERSION="1.2.1"

echo "tgstation-server container startup script v$SCRIPT_VERSION"
echo "PWD: $PWD"

PROD_CONFIG=/config_data/appsettings.Production.yml
HOST_CONFIG=/app/appsettings.Production.yml

if [ ! -f $PROD_CONFIG ]; then
	if [ -z "${General__SetupWizardMode}" ]; then
		export General__SetupWizardMode=Only
	fi

	echo "$PROD_CONFIG not detected! Creating empty and running setup wizard..."
	echo "{}" > $PROD_CONFIG
fi

ln -v -s $PROD_CONFIG $HOST_CONFIG

if [ ! -f "$HOST_CONFIG" ]; then
	echo "ln failed to create symlink!"
	exit 1
fi

echo "Executing console runner..."
exec dotnet Tgstation.Server.Host.Console.dll $@
