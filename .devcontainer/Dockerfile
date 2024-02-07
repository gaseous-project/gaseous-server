FROM mcr.microsoft.com/devcontainers/dotnet:1-8.0-bookworm

RUN apt-get update && apt-get install -y p7zip-full
RUN mkdir -p /workspace/gaseous-server/wwwroot/emulators/EmulatorJS
RUN wget https://cdn.emulatorjs.org/releases/4.0.11.7z
RUN 7z x -y -o/workspace/gaseous-server/wwwroot/emulators/EmulatorJS 4.0.11.7z