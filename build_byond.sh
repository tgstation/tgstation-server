#!/bin/bash

set -e
retval=1
source $HOME/BYOND-${BYOND_MAJOR}.${BYOND_MINOR}/byond/bin/byondsetup

ls $HOME/BYOND-${BYOND_MAJOR}.${BYOND_MINOR}/byond/bin
file $HOME/BYOND-${BYOND_MAJOR}.${BYOND_MINOR}/byond/bin/DreamMaker

if hash $HOME/BYOND-${BYOND_MAJOR}.${BYOND_MINOR}/byond/bin/DreamMaker 2>/dev/null
then
	$HOME/BYOND-${BYOND_MAJOR}.${BYOND_MINOR}/byond/bin/DreamMaker $DMEName 2>&1 | tee result.log
	retval=$?
	if ! grep '\- 0 errors, 0 warnings' result.log
	then
		retval=1 #hard fail, due to warnings or errors
	fi
else
	echo "Couldn't find the DreamMaker executable, aborting."
	retval=2
fi
exit $retval