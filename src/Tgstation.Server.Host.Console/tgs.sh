#!/bin/sh -e

script_full_path=$(dirname "$0")
exec dotnet "$script_full_path/Tgstation.Server.Host.Console.dll" "$@"
