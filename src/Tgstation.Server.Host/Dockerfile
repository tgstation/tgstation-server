#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

# THIS SHOULD NOT BE USED TO CREATE THE PRODUCTION BUILD IT'S FOR DEBUGGING ONLY

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["src/Tgstation.Server.Host/Tgstation.Server.Host.csproj", "src/Tgstation.Server.Host/"]
COPY ["src/Tgstation.Server.Api/Tgstation.Server.Api.csproj", "src/Tgstation.Server.Api/"]
RUN dotnet restore "src/Tgstation.Server.Host/Tgstation.Server.Host.csproj"
COPY . .
WORKDIR "/src/src/Tgstation.Server.Host"
RUN dotnet build "Tgstation.Server.Host.csproj" -c Debug -o /app/build

FROM build AS publish
RUN dotnet publish "Tgstation.Server.Host.csproj" -c Debug -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Tgstation.Server.Host.dll"]
