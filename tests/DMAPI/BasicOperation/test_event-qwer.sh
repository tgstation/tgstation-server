#!/bin/bash

set -e

echo "Running test_event script - $1 - $2"

sleep 5

cd $1
cd tests/DMAPI/BasicOperation

echo $2 > test_event_output.txt

