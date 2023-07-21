#!/bin/bash
# Run from repo root, expects git and rust to be installed, generates tests/libauxcov.so

set -e

cd tests
rm -rf auxtools
git clone https://github.com/willox/auxtools --depth 1

cd auxtools
rustup target add i686-unknown-linux-gnu
export PKG_CONFIG_ALLOW_CROSS=1

cargo build --release --target i686-unknown-linux-gnu -p auxcov
mv target/i686-unknown-linux-gnu/release/libauxcov.so ../
cd ../..
