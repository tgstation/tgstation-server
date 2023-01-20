FROM mcr.microsoft.com/dotnet/sdk:6.0-bullseye-slim AS build

# install node and npm
# replace shell with bash so we can source files
RUN curl --silent -o- https://raw.githubusercontent.com/creationix/nvm/v0.39.1/install.sh | sh

ENV NODE_VERSION 14.18.1

ENV NVM_DIR /root/.nvm
ENV NODE_PATH $NVM_DIR/v$NODE_VERSION/lib/node_modules
ENV PATH $NVM_DIR/versions/node/v$NODE_VERSION/bin:$PATH

RUN . $NVM_DIR/nvm.sh \
    && nvm install $NODE_VERSION \
	&& nvm use $NODE_VERSION \
	&& apt-get update \
	&& apt-get install -y \
		dos2unix \
	&& rm -rf /var/lib/apt/lists/* \
	&& npm install -g npm

#You may wonder why this needs to be in a seperate step. I don't know... It just works(tm)
RUN npm install -g yarn

# Build web control panel
WORKDIR /repo/build

COPY build/Version.props Version.props
COPY build/ControlPanelVersion.props ControlPanelVersion.props

WORKDIR /repo/src/Tgstation.Server.Host

COPY src/Tgstation.Server.Host/Tgstation.Server.Host.csproj ./

# I cant figure out how to run npm as non root so eh
RUN dotnet msbuild -target:NpmBuild

WORKDIR /repo

# Restore nuget packages
COPY tgstation-server.sln ./

COPY src/Tgstation.Server.Host.Console/Tgstation.Server.Host.Console.csproj src/Tgstation.Server.Host.Console/
COPY src/Tgstation.Server.Host.Watchdog/Tgstation.Server.Host.Watchdog.csproj src/Tgstation.Server.Host.Watchdog/
COPY src/Tgstation.Server.Api/Tgstation.Server.Api.csproj src/Tgstation.Server.Api/

RUN dotnet restore -nowarn:MSB3202,nu1503 -p:RestoreUseSkipNonexistentTargets=false

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
	&& curl http://archive.ubuntu.com/ubuntu/pool/main/g/glibc/multiarch-support_2.27-3ubuntu1_amd64.deb --output multiarch-support_2.27.deb \
	&& curl http://security.debian.org/debian-security/pool/updates/main/o/openssl/libssl1.0.0_1.0.1t-1+deb8u12_amd64.deb --output libssl1.0.0.deb \
	&& dpkg -i multiarch-support_2.27.deb \
	&& dpkg -i libssl1.0.0.deb \
	&& rm multiarch-support_2.27.deb \
	&& rm libssl1.0.0.deb \
	&& rm -rf /var/lib/apt/lists/*

EXPOSE 5000

ENV General__ValidInstancePaths__0 /tgs_instances
ENV FileLogging__Directory /tgs_logs

WORKDIR /app

COPY --from=build /app .
COPY --from=build /repo/build/tgs.docker.sh tgs.sh

VOLUME ["/config_data", "/tgs_logs", "/app/lib"]

ENTRYPOINT ["./tgs.sh"]
