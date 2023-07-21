# Run from repo root, expects git and rust to be installed, generates tests/libauxcov.so
$ErrorActionPreference="stop"

cd tests
try
{
    Remove-Item -Recurse -Force auxtools -ErrorAction SilentlyContinue
    git clone https://github.com/willox/auxtools --depth 1
    cd auxtools

    try
    {
        rustup target add i686-pc-windows-msvc
        cargo build --release --target i686-pc-windows-msvc -p auxcov
        mv target/i686-pc-windows-msvc/release/auxcov.dll ../
    }
    finally
    {
        cd ..
    }
}
finally
{
    cd ..
}
