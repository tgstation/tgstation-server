#!/usr/bin/sh -e

cd artifacts && for f in $(find * -type f); do install -D "$f" "$1/opt/tgstation-server/$f"; done
cd ../build/package/deb/man && for f in $(find * -type f); do install -D "$f" "$1/usr/local/man/$f"; done
