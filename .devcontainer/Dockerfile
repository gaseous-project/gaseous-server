FROM mcr.microsoft.com/devcontainers/dotnet:1-8.0-noble

# update apt-get
RUN apt-get update && curl -fsSL https://deb.nodesource.com/setup_20.x | bash - 

# download and unzip EmulatorJS from CDN
RUN apt-get install -y p7zip-full default-jdk nodejs wget && apt-get upgrade -y && apt-get clean
RUN mkdir -p out/wwwroot/emulators/EmulatorJS
RUN wget https://cdn.emulatorjs.org/releases/4.2.3.7z
RUN 7z x -y -oout/wwwroot/emulators/EmulatorJS 4.2.3.7z