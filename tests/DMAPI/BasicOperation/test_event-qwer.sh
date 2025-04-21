#!/bin/bash

set -e

echo "Running test_event script - $1 - $2"

if [[ -z "${TGS_INSTANCE_ROOT}" ]]; then
	echo "TEST ERROR: TGS_INSTANCE_ROOT env var not defined"
	exit 1
fi

echo "TGS_INSTANCE_ROOT: ${TGS_INSTANCE_ROOT}"

sleep 5

cd $1
cd tests/DMAPI/BasicOperation

echo $2 > test_event_output.txt

