FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env
WORKDIR /App
EXPOSE 80

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore "gaseous-server/gaseous-server.csproj"
# Build and publish a release
RUN dotnet publish "gaseous-server/gaseous-server.csproj" -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /App
COPY --from=build-env /App/out .
ENTRYPOINT ["dotnet", "gaseous-server.dll"]
