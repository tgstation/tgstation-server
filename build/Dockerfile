FROM mcr.microsoft.com/dotnet/sdk:10.0-preview-trixie-slim AS build

# Set in CI
ARG TGS_TELEMETRY_KEY_FILE=

# install node and npm
# replace shell with bash so we can source files
RUN curl --silent -o- https://raw.githubusercontent.com/creationix/nvm/v0.39.1/install.sh | sh

ENV NODE_VERSION 20.5.1

ENV NVM_DIR /root/.nvm
ENV NODE_PATH $NVM_DIR/v$NODE_VERSION/lib/node_modules
ENV PATH $NVM_DIR/versions/node/v$NODE_VERSION/bin:$PATH

RUN . $NVM_DIR/nvm.sh \
    && nvm install $NODE_VERSION \
	&& nvm use $NODE_VERSION \
	&& apt-get update \
	&& apt-get install -y \
		dos2unix \
		libgdiplus \
	&& rm -rf /var/lib/apt/lists/* \
	&& corepack enable

# Build web control panel
WORKDIR /repo/build

COPY build/*.props ./

WORKDIR /repo/src/Tgstation.Server.Host

COPY src/Tgstation.Server.Host/Tgstation.Server.Host.csproj ./

# I cant figure out how to run npm as non root so eh
RUN dotnet msbuild -target:NpmBuild

WORKDIR /repo

# Final copy for building
COPY . .

#run dos2unix on tgs.docker.sh so we can build without issue on windows
RUN dos2unix build/tgs.docker.sh build/RemoveUnsupportedRuntimes.sh && dotnet restore

WORKDIR /repo/src/Tgstation.Server.Host.Console
RUN dotnet publish -c Release -o /app \
	&& cd ../.. \
	&& build/RemoveUnsupportedRuntimes.sh /app

WORKDIR /repo/src/Tgstation.Server.Host

RUN export TGS_TELEMETRY_KEY_FILE="../../${TGS_TELEMETRY_KEY_FILE}" \
	&& dotnet publish -c Release -o /app/lib/Default \
	&& cd ../.. \
	&& build/RemoveUnsupportedRuntimes.sh /app/lib/Default \
	&& mv /app/lib/Default/appsettings* /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview-trixie-slim

#needed for byond, curl for healthchecks
RUN apt-get update \
	&& apt-get install -y \
		gcc-multilib \
		gdb \
		curl \
	&& rm -rf /var/lib/apt/lists/*

EXPOSE 5000

ENV General__ValidInstancePaths__0 /tgs_instances
ENV FileLogging__Directory /tgs_logs
ENV Internal__UsingDocker true

WORKDIR /app

COPY --from=build /app .
COPY --from=build /repo/build/tgs.docker.sh tgs.sh

VOLUME ["/config_data", "/tgs_logs", "/app/lib"]

HEALTHCHECK --start-interval=60s CMD curl --fail http://localhost:5000/health || exit

ENTRYPOINT ["./tgs.sh"]
