FROM microsoft/dotnet:2.1-sdk AS build

WORKDIR /src

COPY tgstation-server.sln ./

COPY src/Tgstation.Server.Host.Console/Tgstation.Server.Host.Console.csproj src/Tgstation.Server.Host.Console/
COPY src/Tgstation.Server.Host.Watchdog/Tgstation.Server.Host.Watchdog.csproj src/Tgstation.Server.Host.Watchdog/
COPY src/Tgstation.Server.Host/Tgstation.Server.Host.csproj src/Tgstation.Server.Host/
COPY src/Tgstation.Server.Api/Tgstation.Server.Api.csproj src/Tgstation.Server.Api/

RUN dotnet restore -nowarn:MSB3202,nu1503 -p:RestoreUseSkipNonexistentTargets=false

COPY . .

WORKDIR /src/src/Tgstation.Server.Host.Console
RUN dotnet publish -c Release -o /app

WORKDIR /src/src/Tgstation.Server.Host
RUN dotnet publish -c Release -o /app/lib/Default && mv /app/lib/Default/appsettings* /app

FROM microsoft/dotnet:2.1-aspnetcore-runtime
EXPOSE 5000


RUN apt-get update 
	&& apt-get install -y \
	gcc-multilib \	#needed for byond
	&& rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY --from=build /app .
COPY --from=build /src/build/tgs.docker.sh tgs.sh
COPY --from=build /src/src/Tgstation.Server.Host/appsettings.Docker.json .

RUN mkdir /config_data && mv appsettings.Docker.json /config_data/appsettings.Production.json
VOLUME ["/config_data", "/tgs_logs"]

ENTRYPOINT ["./tgs.sh"]
