#!/bin/bash
# Run from git root, assumes dotnet-sdk-6.0 and npm are installed

set -e
set -x

dpkg --add-architecture i386
apt-get update
apt-get install -y build-essential binutils lintian debhelper dh-make devscripts xmlstarlet # needs cleanup probably, SO copypasta

export DEBEMAIL="Cyberboss@users.noreply.github.com"
export DEBFULLNAME="Jordan Dominion"

TGS_VERSION=$(xmlstarlet sel -N X="http://schemas.microsoft.com/developer/msbuild/2003" --template --value-of /X:Project/X:PropertyGroup/X:TgsCoreVersion build/Version.props)

dh_make -p tgstation-server_$TGS_VERSION -y --createorig -s

cp build/package/deb/debian/* debian/
cp build/tgstation-server.service debian/

dpkg-buildpackage
