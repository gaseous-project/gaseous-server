# Gaseous Server

This is the server for the Gaseous system. All your games and metadata are stored within.

## Requirements
* MySQL Server 8+
* Internet Game Database API Key. See: https://api-docs.igdb.com/#account-creation

## Third Party Projects
The following projects are used by Gaseous
* https://dotnet.microsoft.com/en-us/apps/aspnet
* https://github.com/JamesNK/Newtonsoft.Json
* https://www.nuget.org/packages/MySql.Data/8.0.32.1
* https://github.com/kamranayub/igdb-dotnet

## Configuration File
When Gaseous-Server is started for the first time, it creates a configuration file at ~/.gaseous-server/config.json if it doesn't exist. Some values can be filled in using environment variables (such as in the case of using docker).

### DatabaseConfiguration
| Attribute | Environment Variable |
| --------- | -------------------- |
| HostName  | dbhost               |
| UserName  | dbuser               |
| Password  | dbpass               |

### IGDBConfiguration
| Attribute | Environment Variable |
| --------- | -------------------- |
| ClientId  | igdbclientid         |
| Secret.   | igdbclientsecret     |

### config.json
```json
{
  "DatabaseConfiguration": {
    "HostName": "localhost",
    "UserName": "gaseous",
    "Password": "gaseous",
    "DatabaseName": "gaseous",
    "Port": 3306
  },
  "IGDBConfiguration": {
    "ClientId": "<clientid>",
    "Secret": "<secret>"
  },
  "LoggingConfiguration": {
    "DebugLogging": false,
    "LogFormat": "text"
  }
}

```

## Deploy with Docker
Dockerfile and docker-compose.yml files have been provided to make deployment of the server as easy as possible.
1. Clone the repo with "git clone https://github.com/gaseous-project/gaseous-server.git"
2. Change into the gaseous-server directory
3. Switch to the develop branch with "git checkout develop"
4. Open the docker-compose.yml file and edit the igdbclientid and igdbclientsecret to the values retrieved from your IGDB account
5. Run the command "docker-compose up -d"
6. Connect to the host on port 5198