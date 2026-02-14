FROM mcr.microsoft.com/devcontainers/dotnet:10.0-noble

# update apt-get
RUN apt-get update && curl -fsSL https://deb.nodesource.com/setup_20.x | bash - 

# download and unzip EmulatorJS from CDN
RUN apt-get install -y p7zip-full default-jdk nodejs wget && apt-get upgrade -y && apt-get clean
