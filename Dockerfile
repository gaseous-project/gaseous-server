FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env
WORKDIR /App
EXPOSE 80

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore "gaseous-server/gaseous-server.csproj"
# Build and publish a release
RUN dotnet publish "gaseous-server/gaseous-server.csproj" --use-current-runtime --self-contained false -c Release -o out

# download and unzip EmulatorJS from CDN
RUN apt-get update && apt-get install unzip
RUN mkdir -p out/wwwroot/emulators/EmulatorJS
RUN wget https://cdn.emulatorjs.org/releases/4.0.9.zip
RUN unzip 4.0.9.zip -d out/wwwroot/emulators/EmulatorJS

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /App
COPY --from=build-env /App/out .
ENTRYPOINT ["dotnet", "gaseous-server.dll"]
