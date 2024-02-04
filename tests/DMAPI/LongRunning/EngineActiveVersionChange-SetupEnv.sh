#!/bin/bash

set -e

cd "../../Byond/$1"

echo -e '# This is a comment\nNOTA=Real Comment\n\n\n' > server.env
