FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
ARG TARGETARCH
ARG BUILDPLATFORM
WORKDIR /App
EXPOSE 80

RUN echo "Target: $TARGETARCH"
RUN echo "Build: $BUILDPLATFORM"

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore "gaseous-server/gaseous-server.csproj" -a $TARGETARCH
# Build and publish a release
RUN dotnet publish "gaseous-server/gaseous-server.csproj" --use-current-runtime --self-contained true -c Release -o out -a $TARGETARCH

# download and unzip EmulatorJS from CDN
RUN apt-get update && apt-get install -y p7zip-full
RUN mkdir -p out/wwwroot/emulators/EmulatorJS
RUN wget https://cdn.emulatorjs.org/releases/4.0.9.7z
RUN 7z x -y -oout/wwwroot/emulators/EmulatorJS 4.0.9.7z

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /App
COPY --from=build-env /App/out .
ENTRYPOINT ["dotnet", "gaseous-server.dll"]
