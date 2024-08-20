#!/bin/bash
# Run from git root, certified for ubuntu only since that's what gha uses

SCRIPT_DIR=$( cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )

set -e
set -x

dpkg --add-architecture i386
apt-get update
# This package set needs cleanup probably, StackOverflow copypasta
apt-get install -y \
    build-essential \
    binutils \
    lintian \
    debhelper \
    dh-make \
    devscripts \
    ca-certificates \
    curl \
    gnupg2 \
    xmlstarlet \
    libgdiplus

declare repo_version=$(if command -v lsb_release &> /dev/null; then lsb_release -r -s; else grep -oP '(?<=^VERSION_ID=).+' /etc/os-release | tr -d '"'; fi)
curl -L https://packages.microsoft.com/config/ubuntu/$repo_version/packages-microsoft-prod.deb -o packages-microsoft-prod.deb
dpkg -i ./packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# https://github.com/nodesource/distributions
mkdir -p /etc/apt/keyrings
curl -fsSL https://deb.nodesource.com/gpgkey/nodesource-repo.gpg.key | gpg --dearmor -o /etc/apt/keyrings/nodesource.gpg
export NODE_MAJOR=20
echo "deb [signed-by=/etc/apt/keyrings/nodesource.gpg] https://deb.nodesource.com/node_$NODE_MAJOR.x nodistro main" | tee /etc/apt/sources.list.d/nodesource.list
apt-get update
apt-get install nodejs dotnet-sdk-8.0 -y

corepack enable

CURRENT_COMMIT=$(git rev-parse HEAD)

rm -rf packaging

set +e
git worktree remove -f packaging
set -e

git worktree add -f packaging $CURRENT_COMMIT
cd packaging
rm -f .git

export DEBEMAIL="Cyberboss@users.noreply.github.com"
export DEBFULLNAME="Jordan Dominion"

TGS_VERSION=$(xmlstarlet sel -N X="http://schemas.microsoft.com/developer/msbuild/2003" --template --value-of /X:Project/X:PropertyGroup/X:TgsCoreVersion build/Version.props)

dh_make -p tgstation-server_$TGS_VERSION -y --createorig -s

rm -f debian/README* debian/changelog debian/*.ex debian/upstream/*.ex

pushd ..
if [[ -z "$RELEASE_NOTES_DLL_PATH" ]]; then
    dotnet run -c Release -p:TGS_HOST_NO_WEBPANEL=true --project tools/Tgstation.Server.ReleaseNotes $TGS_VERSION --debian packaging/debian/changelog $CURRENT_COMMIT
else
    dotnet $RELEASE_NOTES_DLL_PATH $TGS_VERSION --debian packaging/debian/changelog $CURRENT_COMMIT
fi
popd

cp -r build/package/deb/debian/* debian/

cp build/tgstation-server.service debian/

SIGN_COMMAND="$SCRIPT_DIR/wrap_gpg.sh"

rm -f /tmp/tgs_wrap_gpg_output.log
set +e

if [[ -z "$PACKAGING_KEYGRIP" ]]; then
    dpkg-buildpackage --no-sign
    EXIT_CODE=$?
else
    dpkg-buildpackage --sign-key=$PACKAGING_KEYGRIP --sign-command="$SIGN_COMMAND"
    cat /tmp/tgs_wrap_gpg_output.log
    EXIT_CODE=$?
fi

cd ..

exit $EXIT_CODE
