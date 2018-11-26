FROM microsoft/dotnet:2.1-sdk AS build

ARG NODE_VERSION=11.1.0

# install node and npm
# replace shell with bash so we can source files
RUN rm /bin/sh && ln -s /bin/bash /bin/sh
RUN curl --silent -o- https://raw.githubusercontent.com/creationix/nvm/v0.31.2/install.sh | bash

RUN source /root/.nvm/nvm.sh \
    && nvm install $NODE_VERSION \
	&& nvm use $NODE_VERSION

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
EXPOSE 80

#needed for byond
RUN apt-get update \
	&& apt-get install -y \
	gcc-multilib \
	&& rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY --from=build /app .
COPY --from=build /src/build/tgs.docker.sh tgs.sh

VOLUME ["/config_data", "/tgs_logs", "/app/lib"]

ENTRYPOINT ["./tgs.sh"]
