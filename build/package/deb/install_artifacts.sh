#!/usr/bin/sh -e

cd artifacts && for f in $(find .); do install "$f" "$1/opt/tgstation-server/$f"; done
