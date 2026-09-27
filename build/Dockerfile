FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
ARG TARGETARCH
ARG BUILDPLATFORM
WORKDIR /App
EXPOSE 80

RUN echo "Target: $TARGETARCH"
RUN echo "Build: $BUILDPLATFORM"

# update apt-get
RUN apt-get update \
    && apt-get upgrade -y \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists

# Copy everything
COPY .. ./

# Build Gaseous Web Server

# Restore as distinct layers
RUN dotnet restore "gaseous-server/gaseous-server.csproj" -a $TARGETARCH
# Build and publish a release
RUN dotnet publish "gaseous-server/gaseous-server.csproj" --use-current-runtime --self-contained true -c Release -o out -a $TARGETARCH

# download and unzip EmulatorJS from CDN
RUN bash build/scripts/get-ejs-git.sh
# Copy the downloaded cores to the output directory
RUN cp -r gaseous-server/wwwroot/emulators/EmulatorJS out/wwwroot/emulators/

# Build runtime image
#FROM mcr.microsoft.com/dotnet/aspnet:10.0
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0
ENV INDOCKER=1
WORKDIR /App

# variables
ARG PUID=1000
ARG PGID=1000
ARG dbhost=localhost
ARG dbuser=root
ARG dbpass=gaseous

ENV PUID=${PUID}
ENV PGID=${PGID}
ENV dbhost=${dbhost}
ENV dbuser=${dbuser}
ENV dbpass=${dbpass}

# install dependencies
RUN apt-get update \
    && apt-get upgrade -y \
    && apt-get install -y supervisor curl mariadb-client ffmpeg \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists

# setup supervisord
COPY ../build/standard/supervisord.conf /etc/supervisor/conf.d/supervisord.conf
RUN mkdir -p /var/run/supervisord
RUN mkdir -p /var/log/supervisord

# copy entrypoint
COPY ../build/standard/entrypoint.sh /usr/sbin/entrypoint.sh
RUN chmod +x /usr/sbin/entrypoint.sh

# Copy the built application from the build environment
COPY --from=build-env /App/out .

# volumes
VOLUME /home/gaseous/.gaseous-server

# Configure healthcheck
HEALTHCHECK --interval=30s --timeout=5s --start-period=60s --retries=3 CMD curl --fail http://localhost:80/api/v1.1/HealthCheck || exit 1

# start services
ENTRYPOINT [ "/usr/sbin/entrypoint.sh" ]