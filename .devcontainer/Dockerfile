FROM mcr.microsoft.com/devcontainers/dotnet:1-8.0-bookworm

# update apt-get
RUN apt-get update

# download and unzip EmulatorJS from CDN
RUN apt-get install -y p7zip-full
RUN mkdir -p out/wwwroot/emulators/EmulatorJS
RUN wget https://cdn.emulatorjs.org/releases/4.1.1.7z
RUN 7z x -y -oout/wwwroot/emulators/EmulatorJS 4.1.1.7z