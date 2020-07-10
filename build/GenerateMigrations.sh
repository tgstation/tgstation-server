#!/bin/bash

# Run from project root with the name of the new migration as a PascalCase parameter
# Migrations need to be tabified, have ArgumentNullException added, doc comments created, and manually reviewed

pushd src/Tgstation.Server.Host
dotnet tool restore
dotnet ef migrations add "MS$1" --context SqlServerDatabaseContext
dotnet ef migrations add "MY$1" --context MySqlDatabaseContext
dotnet ef migrations add "PG$1" --context PostgresSqlDatabaseContext
dotnet ef migrations add "SL$1" --context SqliteDatabaseContext
popd
