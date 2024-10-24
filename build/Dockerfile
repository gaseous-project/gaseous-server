FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
ARG TARGETARCH
ARG BUILDPLATFORM
WORKDIR /App
EXPOSE 80

RUN echo "Target: $TARGETARCH"
RUN echo "Build: $BUILDPLATFORM"

# Copy everything
COPY .. ./

# Build Gaseous Web Server
# Restore as distinct layers
RUN dotnet restore "gaseous-server/gaseous-server.csproj" -a $TARGETARCH
# Build and publish a release
RUN dotnet publish "gaseous-server/gaseous-server.csproj" --use-current-runtime --self-contained true -c Release -o out -a $TARGETARCH

# update apt-get
RUN apt-get update

# download and unzip EmulatorJS from CDN
RUN apt-get install -y p7zip-full
RUN mkdir -p out/wwwroot/emulators/EmulatorJS
RUN cp -fr cdn.emulatorjs.org/latest/* out/wwwroot/emulators/EmulatorJS
RUN rm -Rf cdn.emulatorjs.org

# clean up apt-get
RUN apt-get clean && rm -rf /var/lib/apt/lists

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
ENV INDOCKER=1
WORKDIR /App
COPY --from=build-env /App/out .

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

# install supervisord
RUN apt-get update && apt-get install -y supervisor
COPY ../build/standard/supervisord.conf /etc/supervisor/conf.d/supervisord.conf
RUN mkdir -p /var/run/supervisord
RUN mkdir -p /var/log/supervisord

# clean up apt-get
RUN apt-get clean && rm -rf /var/lib/apt/lists

# copy entrypoint
COPY ../build/standard/entrypoint.sh /usr/sbin/entrypoint.sh
RUN chmod +x /usr/sbin/entrypoint.sh

# volumes
VOLUME /home/gaseous/.gaseous-server

# start services
ENTRYPOINT [ "/usr/sbin/entrypoint.sh" ]