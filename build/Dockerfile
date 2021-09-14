FROM mcr.microsoft.com/dotnet/sdk:6.0-bullseye-slim AS build

# install node and npm
# replace shell with bash so we can source files
RUN curl --silent -o- https://raw.githubusercontent.com/creationix/nvm/v0.31.2/install.sh | sh

ENV NODE_VERSION 10.13.0
ENV NVM_DIR /root/.nvm

RUN . $NVM_DIR/nvm.sh \
    && nvm install $NODE_VERSION \
	&& nvm use $NODE_VERSION \
	&& apt-get update \
	&& apt-get install -y \
		dos2unix \
	&& rm -rf /var/lib/apt/lists/*

ENV NODE_PATH $NVM_DIR/v$NODE_VERSION/lib/node_modules
ENV PATH $NVM_DIR/versions/node/v$NODE_VERSION/bin:$PATH

# Build web control panel
WORKDIR /repo
COPY Directory.Build.props Directory.Build.props

WORKDIR /repo/build

COPY build/Version.props Version.props
COPY build/ControlPanelVersion.props ControlPanelVersion.props

WORKDIR /repo/src
COPY src/Directory.Build.props Directory.Build.props

WORKDIR /repo/src/Tgstation.Server.Host

COPY src/Tgstation.Server.Host/Tgstation.Server.Host.csproj ./

# I cant figure out how to run npm as non root so eh
RUN npm set unsafe-perm true
RUN dotnet msbuild -target:NpmBuild
RUN npm set unsafe-perm false

WORKDIR /repo

# Restore nuget packages
COPY tgstation-server.sln ./

COPY src/Tgstation.Server.Api/Tgstation.Server.Api.csproj src/Tgstation.Server.Api/
COPY src/Tgstation.Server.Helpers/Tgstation.Server.Helpers.csproj src/Tgstation.Server.Helpers/
COPY src/Tgstation.Server.Host/Tgstation.Server.Host.csproj src/Tgstation.Server.Host/
COPY src/Tgstation.Server.Host.Console/Tgstation.Server.Host.Console.csproj src/Tgstation.Server.Host.Console/
COPY src/Tgstation.Server.Host.Watchdog/Tgstation.Server.Host.Watchdog.csproj src/Tgstation.Server.Host.Watchdog/

RUN dotnet restore -nowarn:MSB3202,MSB4181,NU1503 -p:RestoreUseSkipNonexistentTargets=false

# Final copy for building
COPY . .

#run dos2unix on tgs.docker.sh so we can build without issue on windows
RUN dos2unix build/tgs.docker.sh

WORKDIR /repo/src/Tgstation.Server.Host.Console
RUN dotnet publish -c Release -o /app

WORKDIR /repo/src/Tgstation.Server.Host
RUN dotnet publish -c Release -o /app/lib/Default && mv /app/lib/Default/appsettings* /app

FROM mcr.microsoft.com/dotnet/aspnet:6.0-bullseye-slim

#needed for byond
RUN apt-get update \
	&& apt-get install -y \
		gcc-multilib \
		gdb \
		curl \
	&& rm -rf /var/lib/apt/lists/*

EXPOSE 5000

ENV General__ValidInstancePaths__0 /tgs_instances
ENV FileLogging__Directory /tgs_logs

WORKDIR /app

COPY --from=build /app .
COPY --from=build /repo/build/tgs.docker.sh tgs.sh

VOLUME ["/config_data", "/tgs_logs", "/app/lib"]

ENTRYPOINT ["./tgs.sh"]
