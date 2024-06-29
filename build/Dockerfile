FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
ARG TARGETARCH
ARG BUILDPLATFORM
WORKDIR /App
EXPOSE 80

RUN echo "Target: $TARGETARCH"
RUN echo "Build: $BUILDPLATFORM"

# Copy everything
COPY .. ./
# Restore as distinct layers
RUN dotnet restore "gaseous-server/gaseous-server.csproj" -a $TARGETARCH
# Build and publish a release
RUN dotnet publish "gaseous-server/gaseous-server.csproj" --use-current-runtime --self-contained true -c Release -o out -a $TARGETARCH

# disabled for 1.7.4 as the next version EmulatorJS is not yet available
# # update apt-get
# RUN apt-get update

# # download and unzip EmulatorJS from CDN
# RUN apt-get install -y p7zip-full
# RUN mkdir -p out/wwwroot/emulators/EmulatorJS
# RUN wget https://cdn.emulatorjs.org/releases/4.0.12.7z
# RUN 7z x -y -oout/wwwroot/emulators/EmulatorJS 4.0.12.7z
RUN wget --recursive --no-parent https://cdn.emulatorjs.org/latest/
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

# start gaseous-server
ENTRYPOINT ["dotnet", "gaseous-server.dll"]
