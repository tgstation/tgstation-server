#!/bin/bash

set -e
retval=1
source $HOME/BYOND-${BYOND_MAJOR}.${BYOND_MINOR}/byond/bin/byondsetup

DreamMaker $DMEName 2>&1 | tee result.log
retval=$?
if ! grep '\- 0 errors, 0 warnings' result.log
then
	retval=1 #hard fail, due to warnings or errors
fi

exit $retval