# Gaseous Server

This is the server for the Gaseous system. It offers ROM and title management, as well as some basic in browser emulation of those ROM's.

## Warning

This project is currently not suitable for being exposed to the internet.
1. there is currently no authentication support, meaning anyone could trash your library
2. the server has not been hardened for exposure to the internet - so there maybe unknown vulnerabilities

If you expose the server to the internet, **you do so at your own risk**.

## Screenshots
![Library](./screenshots/Library.png)
![Game](./screenshots/Game.png)
![Emulator](./screenshots/Emulator.png)

## Requirements
* MySQL Server 8+*
* Internet Game Database API Key. See: https://api-docs.igdb.com/#account-creation

***Note**: MariaDB is currently not supported as Gaseous uses features present only in MySQL. This is being tracked in https://github.com/gaseous-project/gaseous-server/issues/93

## Third Party Projects
The following projects are used by Gaseous
* https://dotnet.microsoft.com/en-us/apps/aspnet
* https://github.com/JamesNK/Newtonsoft.Json
* https://www.nuget.org/packages/MySql.Data/8.0.32.1
* https://github.com/kamranayub/igdb-dotnet
* https://github.com/EmulatorJS/EmulatorJS

## Discord Server
[![Join our Discord server!](https://invite.casperiv.dev/?inviteCode=Nhu7wpT3k4&format=svg)](https://discord.gg/Nhu7wpT3k4)

# Setup

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

## Docker
### Deploy with the prebuilt Docker image
Dockerfile and docker-compose.yml files have been provided to make deployment of the server as easy as possible.
1. Download the docker-compose.yml file
2. Open the docker-compose.yml file and edit the igdbclientid and igdbclientsecret to the values retrieved from your IGDB account
3. Run the command ```docker-compose up -d```
4. Connect to the host on port 5198

### Build and deploy a Docker image from source
Dockerfile and docker-compose-build.yml files have been provided to make deployment of the server as easy as possible.
1. Clone the repo with ```git clone https://github.com/gaseous-project/gaseous-server.git```
2. Change into the gaseous-server directory
3. Clone the submodules with the command ```git submodule update --init```
4. Open the docker-compose-build.yml file and edit the igdbclientid and igdbclientsecret to the values retrieved from your IGDB account
5. Run the command ```docker-compose --file docker-compose-build.yml up -d```
6. Connect to the host on port 5198

## Source
### Build and deploy
1. Install and configure a MySQL instance
2. Install the dotnet 7.0 packages appropriate for your operating system
    * See: https://learn.microsoft.com/en-us/dotnet/core/install/linux
3. Create a database user with permission to create a databse. Gaseous will create the new database and apply the database schema on it's first startup.
4. Clone the repo with ```git clone https://github.com/gaseous-project/gaseous-server.git```
5. Change into the gaseous-server directory
6. As the main branch is the development branch, you might want to change to a stable version - these are tagged with a version number. For example to change to the 1.5.0 release, use the command ```git checkout v1.5.0```
    * Check the releases page for the version you would like to run: https://github.com/gaseous-project/gaseous-server/releases
7. Clone the submodules with the command ```git submodule update --init --recursive```
    * This command will clone the code that the server uses from other projects (currently only EmulatorJS)
8. Create a directory in the home directory of the user that will run the server. For example, if running as the user ```gaseous```, create the directory ```/home/gaseous/.gaseous-server```
9. Change into the ```.gaseous-server``` directory created in the previous step
10. Copy the JSON from the config file above into a new file named ```config.json```
11. Update the database section with the database server hostname, username, password, and port
12. Compile the server by changing back to the repo cloned earlier and executing:
    * ```dotnet restore "gaseous-server/gaseous-server.csproj"```
    * ```dotnet publish "gaseous-server/gaseous-server.csproj" --use-current-runtime --self-contained false -c Release -o <output directory>```
    * replace ```<output directory>``` with the directory of your choosing. The compiled application will be copied there. For this example we'll use ```/opt/gaseous-server```
13. The server can then be started by executing ```dotnet /opt/gaseous-server/gaseous-server.dll```
    * If you would like the server to run on a different ip address and port (for example 0.0.0.0:8080), add the --urls argument: ```dotnet /opt/gaseous-server/gaseous-server.dll --urls http://0.0.0.0:8080```

**Note**: The above instructions were tested on macOS Ventura, and Ubuntu 22.04.3. There was a report that Debian 11 had an issue with the git submodule commands (see: https://github.com/gaseous-project/gaseous-server/issues/71). This was possibly due to an older git package.

If the git submodule commands aren't working, you can:
1. change to the ```gaseous-server/wwwroot/emulators``` directory
2. delete the ```EmulatorJS``` directory
3. clone the EmulatorJS repository with ```git clone https://github.com/EmulatorJS/EmulatorJS.git```

### Updating from source
1. Stop the server
2. Switch to the source directory
3. Update your repo:
    * If running from the main branch, run ```git pull``` to update the repo
    * If running from another branch or tag, run:
      * ```git fetch```
      * ```git checkout <branch or tag name>```
4. Update the submodules with ```git submodule update --recursive```
5. Run steps 12 and 13 from the above Build guide

# Adding Content
While games can be added to the server without them, it is recommended adding some signature DAT files beforehand to allow for better matching of ROMs to games.

These signature DAT files contain a list of titles with hashes for many of the ROM images that have been found by the community.

Currently supported DAT's:
* TOSEC: https://www.tosecdev.org/downloads/category/56-2023-01-23
* MAME Arcade and MAME Mess: https://www.progettosnaps.net/dats/MAME

If there are other DAT's you'd like to see support for, please raise an issue with a link to the DAT's.

## Adding signature DAT files
### TOSEC
1. Download the DAT files from the source website. For example; from https://www.tosecdev.org/downloads/category/56-2023-01-23
2. Extract the archive
3. Copy the DAT files to ~/.gaseous-server/Data/Signatures/TOSEC/

### MAME Arcade
1. Download the DAT files from the source website. For example; from https://www.progettosnaps.net/dats/MAME
2. Extract the archive
3. Copy the file name "MAME 0.257 (arcade).dat" files to ~/.gaseous-server/Data/Signatures/MAME Arcade/

### MAME MESS
1. Download the DAT files from the source website. For example; from https://www.progettosnaps.net/dats/MAME
2. Extract the archive
3. Copy the file name "MAME 0.257 (mess).dat" files to ~/.gaseous-server/Data/Signatures/MAME MESS/

# Adding Game Images
1. Files can be presented as either stand alone files, or as zip files - currently 7z is unsupported.
2. Name the file appropriately.
   * Attempting a search for the game name on https://www.igdb.com can help with file naming. If a hash search is unsuccessful, Gaseous will fall back to attempting to search by the file name.
3. Add the file to the server:
   * Click the Upload button in the top right of the main Gaseous web page, and drag the files into the modal. The files will be uploaded and analyzed.
   * Copy the file to ~/.gaseous-server/Data/Import

# Game Image Title Matching
Image to game matching follows the following order of operations, stopping the process at the first match:
### Get the file signature
1. Attempt a hash search
2. Attempt to search the signature database for a rom matching the file name - sometimes the hash can not be matched as a highscore table for example was saved to the image
3. Attempt to parse the file name - clues such as the extension being used to define which platform the file belongs to are used to create a search criteria

**Note**: If the file being scanned is a zip, the file will be extracted and searched. The first file whose signature can be found will be used to match the entire zip archive - be sure that the zip only contains files related to one game.

### Create a list of search candidates
Before beginning, remove any version numbers, and anything in the search string that is between ()
1. Add the full name of the image
2. Add the name of the image with any " - " replaced by ": "
3. Add the name of the image with text after a " - " removed
4. Add the name of the image with text after a ": " removed

### Search IGDB for a game match
Loop through each of the search candidates searching using:
1. "where" - exact match as the search candidate
2. "wherefuzzy" - partial match using wildcards
3. "search" - uses a more flexible search method
4. "searchNoPlatform" - uses the "search" method, but does not constrain the search to the determined platform

**Note**: If more than one result is found, the seach will loop through the returned results:
* If an exact (case-insensitive) match is found, that result is used for the match
* If still no match, the image will be set as "Unknown" as there is no way for Gaseous to know which title is the correct one.
