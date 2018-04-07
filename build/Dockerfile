FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src

COPY tgstation-server.sln ./

COPY src/Tgstation.Server.Host.Console/Tgstation.Server.Host.Console.csproj src/Tgstation.Server.Host.Console/
COPY src/Tgstation.Server.Host.Watchdog/Tgstation.Server.Host.Watchdog.csproj src/Tgstation.Server.Host.Watchdog/
COPY src/Tgstation.Server.Host.Startup/Tgstation.Server.Host.Startup.csproj src/Tgstation.Server.Host.Startup/
COPY src/Tgstation.Server.Host/Tgstation.Server.Host.csproj src/Tgstation.Server.Host/
COPY src/Tgstation.Server.Api/Tgstation.Server.Api.csproj src/Tgstation.Server.Api/

COPY tests/Tgstation.Server.Api.Tests/Tgstation.Server.Api.Tests.csproj tests/Tgstation.Server.Api.Tests/
COPY tests/Tgstation.Server.Host.Tests/Tgstation.Server.Host.Tests.csproj tests/Tgstation.Server.Host.Tests/
COPY tests/Tgstation.Server.Host.Watchdog.Tests/Tgstation.Server.Host.Watchdog.Tests.csproj tests/Tgstation.Server.Host.Watchdog.Tests/
COPY tests/Tgstation.Server.Host.Console.Tests/Tgstation.Server.Host.Console.Tests.csproj tests/Tgstation.Server.Host.Console.Tests/

RUN dotnet restore -nowarn:MSB3202,nu1503 -p:RestoreUseSkipNonexistentTargets=false

COPY . .

WORKDIR /src/src/Tgstation.Server.Host.Startup
RUN dotnet build -c Release

WORKDIR /src/tests/Tgstation.Server.Api.Tests
RUN dotnet test -c Release

WORKDIR /src/tests/Tgstation.Server.Host.Tests
RUN dotnet test -c Release

WORKDIR /src/tests/Tgstation.Server.Host.Watchdog.Tests
RUN dotnet test -c Release

WORKDIR /src/tests/Tgstation.Server.Host.Console.Tests
RUN dotnet test -c Release

FROM build AS publish
RUN dotnet publish -c Release -o /app

FROM microsoft/dotnet:2.1-runtime
EXPOSE 80
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Tgstation.Server.Host.Console.dll"]
