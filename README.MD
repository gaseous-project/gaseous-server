[![.NET](https://github.com/gaseous-project/gaseous-server/actions/workflows/dotnet.yml/badge.svg)](https://github.com/gaseous-project/gaseous-server/actions/workflows/dotnet.yml) [![Nightly](https://github.com/gaseous-project/gaseous-server/actions/workflows/BuildNightly.yml/badge.svg)](https://github.com/gaseous-project/gaseous-server/actions/workflows/BuildNightly.yml) [![Build Release Docker Image](https://github.com/gaseous-project/gaseous-server/actions/workflows/BuildDockerOnTag-Release.yml/badge.svg)](https://github.com/gaseous-project/gaseous-server/actions/workflows/BuildDockerOnTag-Release.yml)
# <img src="./logo.png" height="28" style="float: right;" /> Gaseous Server

This is the server for the Gaseous system. It offers ROM and title management, as well as some basic in browser emulation of those ROMs.

> Version 1.7.0 and later contain user authentication, and can be exposed to the internet. However, it is recommended to not expose the server to the internet if you're not actively using it remotely, or if you have alternative means to access it remotely like a VPN.

> While we do our best to stay on top of server security, if you expose the server to the internet **you do so at your own risk**.

## Screenshots
![Library](./gaseous-server/wwwroot/screenshots/Library.png)
![Game](./gaseous-server/wwwroot/screenshots/Game.png)
![Emulator](./gaseous-server/wwwroot/screenshots/Emulator.png)

## Requirements
* MariaDB 11.1.2+ (preferred) or MySQL Server 8+
  * These are the database versions Gaseous has been tested and developed against. Your mileage may vary with earlier versions.
  * MariaDB is the preferred database server, while MySQL will continue to be supported for existing users (they should be interchangable).
  * Note that due to the earlier database schema using MySQL specific features, moving to MariaDB from MySQL will require rebuilding your database from scratch. The "Library Scan" background task can be used to re-import all titles.
* Internet Game Database API Key. See: https://api-docs.igdb.com/#account-creation - only required when not using the Hasheous proxy

# Installation
See https://github.com/gaseous-project/gaseous-server/wiki/Installation for installation instructions.

## Publish output layout
When you publish `gaseous-server`, the `gaseous-cli` tool is also published automatically into the same publish directory (no subfolder). Examples:

- Linux (framework-dependent): `publish/linux-x64/` contains `gaseous-server.dll` and `gaseous-cli.dll` (or executable if self-contained).
- Windows (self-contained): `publish/win-x64-sc/` contains `gaseous-server.exe` and `gaseous-cli.exe`.

You can deploy both by copying the entire publish directory to your target location. The CLI runs alongside the server and uses the same configuration database.

# Running as a Windows Service

> **Note**: This feature is currently experimental.

Gaseous Server can run as a standard console app, in Docker, or as a Windows Service.

1) Publish for Windows (example: win-x64)
  - Use your preferred method to publish a self-contained or framework-dependent build.

2) Create the service (PowerShell as Administrator)
  - New-Service -Name "GaseousServer" -BinaryPathName "C:\path\to\gaseous-server.exe" -DisplayName "Gaseous Server" -Description "Gaseous metadata and library server" -StartupType Automatic
  - Start-Service GaseousServer

**Notes**
- Configure the listening URL via appsettings.Production.json (Kestrel Endpoints) or ASPNETCORE_URLS.
- Ensure the service account has permissions to the library paths and logs directory.
- Open the firewall for the chosen port.

# Logging behavior
- Console/normal runs (Windows/Linux): logs are written to stdout; optional DB/disk logging stays the same.
- Windows Service runs: logs are written to the Windows Application Event Log (source: GaseousServer). If the Event Log write fails (e.g., insufficient rights), logs fall back to disk under the configured Logs directory.

## Configuration directory override (recommended for services)
By default, configuration is stored under the current user's profile directory in a folder named `.gaseous-server`.

- Override this location by setting the environment variable `GASEOUS_CONFIG_PATH` to an absolute directory path.
- This is especially useful when running as a service where the default profile may be `LocalSystem`.

Examples

- Windows (PowerShell as Administrator, sets a machine-wide variable):

```powershell
[Environment]::SetEnvironmentVariable("GASEOUS_CONFIG_PATH", "C:\\ProgramData\\Gaseous", "Machine")
# Restart the Gaseous service after setting this
```

- Linux systemd unit (Environment entry): add to your unit file's [Service] section:

```ini
Environment=GASEOUS_CONFIG_PATH=/var/lib/gaseous
```

## Examples

Publish (framework-dependent)

```bash
dotnet publish ./gaseous-server/gaseous-server.csproj -c Release -r win-x64 --self-contained false -o ./publish/win-x64
```

Publish (self-contained single file)

```bash
dotnet publish ./gaseous-server/gaseous-server.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishTrimmed=false -o ./publish/win-x64-sc

CLI quick usage (run from the publish folder)

```bash
# Framework-dependent example
dotnet ./gaseous-cli.dll --help

# Self-contained example
./gaseous-cli --help
```
```

Install and start service (PowerShell, run as Administrator)

```powershell
$exe = "C:\apps\gaseous\gaseous-server.exe"
# Optional: set URLs via args (alternatively use appsettings.Production.json)
$args = "--urls http://*:8080"
New-Service -Name "GaseousServer" -BinaryPathName "$exe $args" -DisplayName "Gaseous Server" -Description "Gaseous metadata and library server" -StartupType Automatic
Start-Service GaseousServer
```

Install and start service (Command Prompt / sc.exe, run as Administrator)

```bat
set EXE=C:\apps\gaseous\gaseous-server.exe
set ARGS=--urls http://*:8080
sc create GaseousServer binPath= "%EXE% %ARGS%" DisplayName= "Gaseous Server" start= auto
sc start GaseousServer
```

Stop and remove service

```powershell
Stop-Service GaseousServer -ErrorAction SilentlyContinue
Remove-Service GaseousServer -ErrorAction SilentlyContinue
```

or using sc.exe:

```bat
sc stop GaseousServer
sc delete GaseousServer
```

View logs (Windows Service)

```powershell
# Event Viewer > Windows Logs > Application
# Filter by Source: GaseousServer
eventvwr.msc
```

Linux systemd service

1) Publish for Linux (example: linux-x64)

```bash
dotnet publish ./gaseous-server/gaseous-server.csproj -c Release -r linux-x64 --self-contained false -o ./publish/linux-x64
```

2) Deploy files (example path)

```bash
sudo mkdir -p /opt/gaseous
sudo rsync -a ./publish/linux-x64/ /opt/gaseous/
sudo chown -R gaseous:gaseous /opt/gaseous || true   # or pick your service user
```

3) Create systemd unit file: /etc/systemd/system/gaseous-server.service

```ini
[Unit]
Description=Gaseous Server
After=network.target

[Service]
Type=simple
# Choose one ExecStart:
# Framework-dependent:
ExecStart=/usr/bin/dotnet /opt/gaseous/gaseous-server.dll --urls http://0.0.0.0:8080
# Or if self-contained publish:
# ExecStart=/opt/gaseous/gaseous-server --urls http://0.0.0.0:8080
WorkingDirectory=/opt/gaseous
Environment=DOTNET_ENVIRONMENT=Production
# Optional: place config and logs under a service-friendly path (overrides default ~/.gaseous-server)
Environment=GASEOUS_CONFIG_PATH=/var/lib/gaseous
# Optional: ASPNETCORE_URLS can be used instead of --urls
# Environment=ASPNETCORE_URLS=http://0.0.0.0:8080
User=gaseous
Group=gaseous
Restart=always
RestartSec=5

[Install]
WantedBy=multi-user.target
```

4) Enable and start

```bash
sudo systemctl daemon-reload
sudo systemctl enable gaseous-server
sudo systemctl start gaseous-server
```

5) Logs (stdout goes to journald)

```bash
journalctl -u gaseous-server -f
```

# Kestrel configuration examples
Gaseous uses Kestrel. You can configure listeners via appsettings (recommended) or with ASPNETCORE_URLS/--urls. If both are set, command-line/ASPNETCORE_URLS take precedence.

Basic HTTP (all interfaces, port 8080) – put this in `gaseous-server/appsettings.Production.json`:

```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:8080"
      }
    }
  }
}
```

HTTPS with PFX certificate:

```json
{
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://0.0.0.0:8443",
        "Certificate": {
          "Path": "/etc/gaseous/certs/site.pfx",
          "Password": "<pfx-password>"
        }
      }
    }
  }
}
```

HTTPS with PEM keypair:

```json
{
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://0.0.0.0:8443",
        "Certificate": {
          "Path": "/etc/gaseous/certs/fullchain.pem",
          "KeyPath": "/etc/gaseous/certs/privkey.pem"
        }
      }
    }
  }
}
```

Multiple endpoints (HTTP + HTTPS):

```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": { "Url": "http://0.0.0.0:8080" },
      "Https": {
        "Url": "https://0.0.0.0:8443",
        "Certificate": { "Path": "/etc/gaseous/certs/site.pfx", "Password": "<pfx-password>" }
      }
    }
  }
}
```

Windows certificate store example:

```json
{
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://0.0.0.0:8443",
        "Certificate": {
          "Subject": "CN=your.domain",
          "Store": "My",
          "Location": "CurrentUser",
          "AllowInvalid": false
        }
      }
    }
  }
}
```

Using environment variable or args instead of appsettings:

```bash
# Environment variable
export ASPNETCORE_URLS="http://0.0.0.0:8080"    # or "https://0.0.0.0:8443"

# Command-line (Windows or Linux)
./gaseous-server --urls http://0.0.0.0:8080
```

Behind a reverse proxy (Nginx/Apache/Traefik):
- Terminate TLS at the proxy and forward to Gaseous over HTTP (e.g., http://127.0.0.1:8080).
- Ensure proxy sends X-Forwarded-For and X-Forwarded-Proto. Gaseous will honor these and not force a redirect when original scheme is HTTPS.
- Whitelist your proxy addresses so forwarded headers are trusted. You can set these in `~/.gaseous-server/config.json` or via env when running in Docker:

```json
{
  "ReverseProxyConfiguration": {
    "RequireHeaderSymmetry": true,
    "KnownProxies": ["127.0.0.1", "10.0.0.10"],
    "KnownNetworks": ["10.0.0.0/8", "192.168.0.0/16"]
  }
}
```

Docker env equivalents:

```bash
-e knownproxies="127.0.0.1,10.0.0.10" \
-e knownnetworks="10.0.0.0/8,192.168.0.0/16"
```

Notes
- Precedence: --urls and ASPNETCORE_URLS override appsettings Kestrel:Endpoints.
- If you run HTTP only directly (no reverse proxy), be aware the app uses HTTPS redirection middleware; either configure an HTTPS endpoint or place it behind a proxy that sets X-Forwarded-Proto.

# Adding Content
1. Import signatures: see https://github.com/gaseous-project/gaseous-server/wiki/Signatures
2. Add ROMs: see https://github.com/gaseous-project/gaseous-server/wiki/Adding-ROMs

# Friends of Gaseous
* [EmulatorJS](https://github.com/EmulatorJS/EmulatorJS): A fantastic (and fast) Javascript based implementation of RetroArch, supporting a wide variety of platforms. Discord: https://discord.gg/6akryGkETU
* [RomM](https://github.com/zurdi15/romm): Another self hosted ROM manager. Discord: https://discord.gg/P5HtHnhUDH

# Discord Server
Join our Discord server: https://discord.gg/Nhu7wpT3k4

## Windows MSI installer (setup)

An MSI installer project named `setup` is included for Windows. It installs Gaseous Server into Program Files and prompts for MariaDB host, port, and username.

- Output: after publishing and building, the MSI is under `setup/bin/Release/net8.0/`.
- It sets machine-wide environment variables:
  - `dbhost`, `dbport`, `dbuser`
  - `GASEOUS_CONFIG_PATH` = `%ProgramData%\Gaseous` (the installer also creates this folder)

Build steps
1) Publish the server for Release to produce files to harvest.
2) Build the MSI project.

Install
- Run the MSI on Windows, select install folder, then enter DB settings when prompted.
- Binaries are placed under Program Files\Gaseous Server. Default config directory is ProgramData\Gaseous.
- Uninstall removes binaries and environment variables; ProgramData contents are preserved by default.
