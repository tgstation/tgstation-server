#!/bin/bash

set -e

cd "$1/tests/DMAPI/LongRunning"

tr -dc A-Za-z0-9 </dev/urandom | head -c 13 > resource.txt
echo -e '\naljsdhfjahsfkjnsalkjdfhskljdackmcnvxkljhvkjsdanv,jdshlkufhklasjeFDhfjkalhdkjlfhalksfdjh\n' >> resource.txt
