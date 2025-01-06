using System;
using System.Data;
using System.IO.Compression;
using System.Security.Authentication;
using System.Security.Policy;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using gaseous_server.Classes.Metadata;
using gaseous_server.Models;
using NuGet.Common;
using NuGet.LibraryModel;
using static gaseous_server.Classes.Metadata.Games;
using static gaseous_server.Classes.FileSignature;
using HasheousClient.Models;
using HasheousClient.Models.Metadata.IGDB;

namespace gaseous_server.Classes
{
    public class ImportGame : QueueItemStatus
    {
        /// <summary>
        /// Scan the import directory for games and process them
        /// </summary>
        /// <param name="ImportPath">
        /// The path to the directory to scan
        /// </param>
        /// <exception cref="DirectoryNotFoundException">
        /// Thrown when the import directory does not exist
        /// </exception>
        public void ProcessDirectory(string ImportPath)
        {
            if (Directory.Exists(ImportPath))
            {
                string[] importContents = Directory.GetFiles(ImportPath, "*.*", SearchOption.AllDirectories);

                Logging.Log(Logging.LogType.Information, "Import Games", "Found " + importContents.Length + " files to process in import directory: " + ImportPath);

                // import files first
                int importCount = 1;
                foreach (string importContent in importContents)
                {
                    SetStatus(importCount, importContents.Length, "Importing file: " + importContent);

                    ImportGameFile(importContent, null);

                    importCount += 1;
                }
                ClearStatus();

                DeleteOrphanedDirectories(ImportPath);
            }
            else
            {
                Logging.Log(Logging.LogType.Critical, "Import Games", "The import directory " + ImportPath + " does not exist.");
                throw new DirectoryNotFoundException("Invalid path: " + ImportPath);
            }
        }

        /// <summary>
        /// Import a single game file
        /// </summary>
        /// <param name="GameFileImportPath">
        /// The path to the game file to import
        /// </param>
        /// <param name="OverridePlatform">
        /// The platform to use for the game file
        /// </param>
        /// <returns>
        /// A dictionary containing the results of the import
        /// </returns>
        public Dictionary<string, object> ImportGameFile(string GameFileImportPath, Platform? OverridePlatform)
        {
            Dictionary<string, object> RetVal = new Dictionary<string, object>();
            RetVal.Add("path", Path.GetFileName(GameFileImportPath));

            if (Common.SkippableFiles.Contains<string>(Path.GetFileName(GameFileImportPath), StringComparer.OrdinalIgnoreCase))
            {
                Logging.Log(Logging.LogType.Debug, "Import Game", "Skipping item " + GameFileImportPath);
            }
            else
            {
                Common.hashObject hash = new Common.hashObject(GameFileImportPath);

                Models.PlatformMapping.PlatformMapItem? IsBios = Classes.Bios.BiosHashSignatureLookup(hash.md5hash);

                if (IsBios == null)
                {
                    // file is a rom
                    _ImportGameFile(GameFileImportPath, hash, ref RetVal, OverridePlatform);
                }
                else
                {
                    // file is a bios
                    Bios.ImportBiosFile(GameFileImportPath, hash, ref RetVal);
                }
            }

            return RetVal;
        }

        /// <summary>
        /// Import a single game file
        /// </summary>
        /// <param name="FilePath">
        /// The path to the game file to import
        /// </param>
        /// <param name="Hash">
        /// The hash of the game file
        /// </param>
        /// <param name="GameFileInfo">
        /// A dictionary to store the results of the import
        /// </param>
        /// <param name="OverridePlatform">
        /// The platform to use for the game file
        /// </param>
        private static void _ImportGameFile(string FilePath, Common.hashObject Hash, ref Dictionary<string, object> GameFileInfo, Platform? OverridePlatform)
        {
            GameFileInfo.Add("type", "rom");

            // check to make sure we don't already have this file imported
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "";
            Dictionary<string, object> dbDict = new Dictionary<string, object>();

            sql = "SELECT COUNT(Id) AS count FROM view_Games_Roms WHERE MD5=@md5 AND SHA1=@sha1";
            dbDict.Add("md5", Hash.md5hash);
            dbDict.Add("sha1", Hash.sha1hash);
            DataTable importDB = db.ExecuteCMD(sql, dbDict);
            if ((Int64)importDB.Rows[0]["count"] > 0)
            {
                // import source was the import directory
                if (FilePath.StartsWith(Config.LibraryConfiguration.LibraryImportDirectory))
                {
                    Logging.Log(Logging.LogType.Warning, "Import Game", "  " + FilePath + " already in database - moving to " + Config.LibraryConfiguration.LibraryImportDuplicatesDirectory);

                    string targetPathWithFileName = FilePath.Replace(Config.LibraryConfiguration.LibraryImportDirectory, Config.LibraryConfiguration.LibraryImportDuplicatesDirectory);
                    string targetPath = Path.GetDirectoryName(targetPathWithFileName);

                    if (!Directory.Exists(targetPath))
                    {
                        Directory.CreateDirectory(targetPath);
                    }
                    File.Move(FilePath, targetPathWithFileName, true);
                }

                // import source was the upload directory
                if (FilePath.StartsWith(Config.LibraryConfiguration.LibraryUploadDirectory))
                {
                    Logging.Log(Logging.LogType.Warning, "Import Game", "  " + FilePath + " already in database - skipping import");
                }

                GameFileInfo.Add("status", "duplicate");
            }
            else
            {
                Logging.Log(Logging.LogType.Information, "Import Game", "  " + FilePath + " not in database - processing");

                FileInfo fi = new FileInfo(FilePath);
                FileSignature fileSignature = new FileSignature();
                gaseous_server.Models.Signatures_Games discoveredSignature = fileSignature.GetFileSignature(GameLibrary.GetDefaultLibrary, Hash, fi, FilePath);

                // add to database
                Platform? determinedPlatform = Metadata.Platforms.GetPlatform((long)discoveredSignature.Flags.PlatformId);
                Models.Game? determinedGame = Metadata.Games.GetGame(discoveredSignature.Flags.GameMetadataSource, discoveredSignature.Flags.GameId);
                long RomId = StoreGame(GameLibrary.GetDefaultLibrary, Hash, discoveredSignature, determinedPlatform, FilePath, 0, true);

                // build return value
                GameFileInfo.Add("romid", RomId);
                GameFileInfo.Add("platform", determinedPlatform);
                GameFileInfo.Add("game", determinedGame);
                GameFileInfo.Add("signature", discoveredSignature);
                GameFileInfo.Add("status", "imported");
            }
        }

        /// <summary>
        /// Store a game in the database and move the file to the library (if required)
        /// </summary>
        /// <param name="library">
        /// The library to store the game in
        /// </param>
        /// <param name="hash">
        /// The hash of the game file
        /// </param>
        /// <param name="signature">
        /// The signature of the game file
        /// </param>
        /// <param name="filePath">
        /// The path to the game file
        /// </param>
        /// <param name="romId">
        /// The ID of the ROM in the database (if it already exists, 0 if it doesn't)
        /// </param>
        /// <param name="SourceIsExternal">
        /// Whether the source of the file is external to the library
        /// </param>
        public static long StoreGame(GameLibrary.LibraryItem library, Common.hashObject hash, Signatures_Games signature, Platform platform, string filePath, long romId = 0, bool SourceIsExternal = false)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);

            string sql = "";

            Dictionary<string, object> dbDict = new Dictionary<string, object>();

            // add/get the metadata map
            MetadataMap? map = MetadataManagement.NewMetadataMap((long)platform.Id, signature.Game.Name);

            // add any metadata attributes that may be supplied as part of the signature
            if (signature.Game.UserManual != null)
            {
                if (signature.Game.UserManual.Length > 0)
                {
                    MetadataManagement.SetMetadataSupportData((long)map.Id, MetadataManagement.MetadataMapSupportDataTypes.UserManualLink, signature.Game.UserManual);
                }
            }

            // populate map with the sources from the signature if they don't already exist
            bool reloadMap = false;
            foreach (MetadataSources source in Enum.GetValues(typeof(MetadataSources)))
            {
                if (source != MetadataSources.None)
                {
                    // check the signature for the source, and if it exists, add it to the map if it's not already there
                    foreach (Signatures_Games.SourceValues.SourceValueItem signatureSource in signature.MetadataSources.Games)
                    {
                        // check if the metadata map contains the source
                        bool sourceExists = false;
                        foreach (MetadataMap.MetadataMapItem mapSource in map.MetadataMapItems)
                        {
                            if (mapSource.SourceType == source)
                            {
                                sourceExists = true;
                            }
                        }

                        if (sourceExists == false)
                        {
                            // add the source to the map
                            bool preferred = false;
                            if (source == Config.MetadataConfiguration.DefaultMetadataSource)
                            {
                                preferred = true;
                            }
                            MetadataManagement.AddMetadataMapItem((long)map.Id, source, signatureSource.Id, preferred);
                            reloadMap = true;
                        }
                    }
                }
            }
            if (reloadMap == true)
            {
                map = MetadataManagement.GetMetadataMap((long)map.Id);
            }

            // add or update the rom
            dbDict = new Dictionary<string, object>();
            if (romId == 0)
            {
                sql = "INSERT INTO Games_Roms (PlatformId, GameId, Name, Size, CRC, MD5, SHA1, DevelopmentStatus, Attributes, RomType, RomTypeMedia, MediaLabel, RelativePath, MetadataSource, MetadataGameName, MetadataVersion, LibraryId, RomDataVersion, MetadataMapId) VALUES (@platformid, @gameid, @name, @size, @crc, @md5, @sha1, @developmentstatus, @Attributes, @romtype, @romtypemedia, @medialabel, @path, @metadatasource, @metadatagamename, @metadataversion, @libraryid, @romdataversion, @metadatamapid); SELECT CAST(LAST_INSERT_ID() AS SIGNED);";
            }
            else
            {
                sql = "UPDATE Games_Roms SET PlatformId=@platformid, GameId=@gameid, Name=@name, Size=@size, DevelopmentStatus=@developmentstatus, Attributes=@Attributes, RomType=@romtype, RomTypeMedia=@romtypemedia, MediaLabel=@medialabel, MetadataSource=@metadatasource, MetadataGameName=@metadatagamename, MetadataVersion=@metadataversion, RomDataVersion=@romdataversion, MetadataMapId=@metadatamapid WHERE Id=@id;";
                dbDict.Add("id", romId);
            }
            dbDict.Add("platformid", Common.ReturnValueIfNull(platform.Id, 0));
            dbDict.Add("gameid", 0); // set to 0 - no longer required as game is mapped using the MetadataMapBridge table
            dbDict.Add("name", Common.ReturnValueIfNull(signature.Rom.Name, 0));
            dbDict.Add("size", Common.ReturnValueIfNull(signature.Rom.Size, 0));
            dbDict.Add("md5", hash.md5hash);
            dbDict.Add("sha1", hash.sha1hash);
            dbDict.Add("crc", Common.ReturnValueIfNull(signature.Rom.Crc, ""));
            dbDict.Add("developmentstatus", Common.ReturnValueIfNull(signature.Rom.DevelopmentStatus, ""));
            dbDict.Add("metadatasource", signature.Rom.SignatureSource);
            dbDict.Add("metadatagamename", signature.Game.Name);
            dbDict.Add("metadataversion", 2);
            dbDict.Add("libraryid", library.Id);
            dbDict.Add("romdataversion", 2);
            dbDict.Add("metadatamapid", map.Id);

            if (signature.Rom.Attributes != null)
            {
                if (signature.Rom.Attributes.Count > 0)
                {
                    dbDict.Add("attributes", Newtonsoft.Json.JsonConvert.SerializeObject(signature.Rom.Attributes));
                }
                else
                {
                    dbDict.Add("attributes", "[ ]");
                }
            }
            else
            {
                dbDict.Add("attributes", "[ ]");
            }
            dbDict.Add("romtype", (int)signature.Rom.RomType);
            dbDict.Add("romtypemedia", Common.ReturnValueIfNull(signature.Rom.RomTypeMedia, ""));
            dbDict.Add("medialabel", Common.ReturnValueIfNull(signature.Rom.MediaLabel, ""));

            string libraryRootPath = library.Path;
            if (libraryRootPath.EndsWith(Path.DirectorySeparatorChar.ToString()) == false)
            {
                libraryRootPath += Path.DirectorySeparatorChar;
            }
            dbDict.Add("path", filePath.Replace(libraryRootPath, ""));

            DataTable romInsert = db.ExecuteCMD(sql, dbDict);
            if (romId == 0)
            {
                romId = (long)romInsert.Rows[0][0];
            }

            // move to destination
            if (library.IsDefaultLibrary == true)
            {
                MoveGameFile(romId, SourceIsExternal);
            }

            return romId;
        }

        public static gaseous_server.Models.Game SearchForGame(gaseous_server.Models.Signatures_Games Signature, long PlatformId, bool FullDownload)
        {
            if (Signature.Flags != null)
            {
                if (Signature.Flags.GameId != null && Signature.Flags.GameId != 0)
                {
                    // game was determined elsewhere - probably a Hasheous server
                    try
                    {
                        return Games.GetGame(MetadataSources.IGDB, Signature.Flags.GameId);
                    }
                    catch (Exception ex)
                    {
                        Logging.Log(Logging.LogType.Warning, "Import Game", "Provided game id resulted in a failed game lookup", ex);
                    }
                }
            }

            // search discovered game - case insensitive exact match first
            gaseous_server.Models.Game determinedGame = new gaseous_server.Models.Game();

            string GameName = Signature.Game.Name;

            List<string> SearchCandidates = GetSearchCandidates(GameName);

            foreach (string SearchCandidate in SearchCandidates)
            {
                bool GameFound = false;

                Logging.Log(Logging.LogType.Information, "Import Game", "  Searching for title: " + SearchCandidate);

                foreach (Metadata.Games.SearchType searchType in Enum.GetValues(typeof(Metadata.Games.SearchType)))
                {
                    Logging.Log(Logging.LogType.Information, "Import Game", "  Search type: " + searchType.ToString());
                    gaseous_server.Models.Game[] games = Metadata.Games.SearchForGame(SearchCandidate, PlatformId, searchType);
                    if (games != null)
                    {
                        if (games.Length == 1)
                        {
                            // exact match!
                            determinedGame = Metadata.Games.GetGame(MetadataSources.IGDB, (long)games[0].Id);
                            Logging.Log(Logging.LogType.Information, "Import Game", "  IGDB game: " + determinedGame.Name);
                            GameFound = true;
                            break;
                        }
                        else if (games.Length > 0)
                        {
                            Logging.Log(Logging.LogType.Information, "Import Game", "  " + games.Length + " search results found");

                            // quite likely we've found sequels and alternate types
                            foreach (gaseous_server.Models.Game game in games)
                            {
                                if (game.Name == SearchCandidate)
                                {
                                    // found game title matches the search candidate
                                    determinedGame = Metadata.Games.GetGame(MetadataSources.IGDB, (long)games[0].Id);
                                    Logging.Log(Logging.LogType.Information, "Import Game", "Found exact match!");
                                    GameFound = true;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            Logging.Log(Logging.LogType.Information, "Import Game", "  No search results found");
                        }
                    }
                    else
                    {
                        Logging.Log(Logging.LogType.Information, "Import Game", "  No search results found");
                    }
                }
                if (GameFound == true) { break; }
            }
            if (determinedGame == null)
            {
                determinedGame = new gaseous_server.Models.Game();
            }

            string destSlug = "";
            if (determinedGame.Id == null)
            {
                Logging.Log(Logging.LogType.Information, "Import Game", "  Unable to determine game");
            }

            return determinedGame;
        }

        public static List<gaseous_server.Models.Game> SearchForGame_GetAll(string GameName, long PlatformId)
        {
            List<gaseous_server.Models.Game> searchResults = new List<gaseous_server.Models.Game>();

            List<string> SearchCandidates = GetSearchCandidates(GameName);

            foreach (string SearchCandidate in SearchCandidates)
            {
                foreach (Metadata.Games.SearchType searchType in Enum.GetValues(typeof(Metadata.Games.SearchType)))
                {
                    if ((PlatformId == 0 && searchType == SearchType.searchNoPlatform) || (PlatformId != 0 && searchType != SearchType.searchNoPlatform))
                    {
                        gaseous_server.Models.Game[] games = Metadata.Games.SearchForGame(SearchCandidate, PlatformId, searchType);
                        foreach (gaseous_server.Models.Game foundGame in games)
                        {
                            bool gameInResults = false;
                            foreach (gaseous_server.Models.Game searchResult in searchResults)
                            {
                                if (searchResult.Id == foundGame.Id)
                                {
                                    gameInResults = true;
                                }
                            }

                            if (gameInResults == false)
                            {
                                searchResults.Add(foundGame);
                            }
                        }
                    }
                }
            }

            return searchResults;

        }

        private static List<string> GetSearchCandidates(string GameName)
        {
            // remove version numbers from name
            GameName = Regex.Replace(GameName, @"v(\d+\.)?(\d+\.)?(\*|\d+)$", "").Trim();
            GameName = Regex.Replace(GameName, @"Rev (\d+\.)?(\d+\.)?(\*|\d+)$", "").Trim();

            // assumption: no games have () in their titles so we'll remove them
            int idx = GameName.IndexOf('(');
            if (idx >= 0)
            {
                GameName = GameName.Substring(0, idx);
            }

            List<string> SearchCandidates = new List<string>();
            SearchCandidates.Add(GameName.Trim());
            if (GameName.Contains(" - "))
            {
                SearchCandidates.Add(GameName.Replace(" - ", ": ").Trim());
                SearchCandidates.Add(GameName.Substring(0, GameName.IndexOf(" - ")).Trim());
            }
            if (GameName.Contains(": "))
            {
                SearchCandidates.Add(GameName.Substring(0, GameName.IndexOf(": ")).Trim());
            }

            Logging.Log(Logging.LogType.Information, "Import Game", "  Search candidates: " + String.Join(", ", SearchCandidates));

            return SearchCandidates;
        }

        public static string ComputeROMPath(long RomId)
        {
            Classes.Roms.GameRomItem rom = Classes.Roms.GetRom(RomId);

            // get metadata
            Platform? platform = gaseous_server.Classes.Metadata.Platforms.GetPlatform(rom.PlatformId);
            gaseous_server.Models.Game? game = gaseous_server.Classes.Metadata.Games.GetGame(Config.MetadataConfiguration.DefaultMetadataSource, rom.GameId);

            // build path
            string platformSlug = "Unknown Platform";
            if (platform != null)
            {
                platformSlug = platform.Slug;
            }
            string gameSlug = "Unknown Title";
            if (game != null)
            {
                gameSlug = game.Slug;
            }
            string DestinationPath = Path.Combine(GameLibrary.GetDefaultLibrary.Path, platformSlug);
            if (!Directory.Exists(DestinationPath))
            {
                Directory.CreateDirectory(DestinationPath);
            }

            string DestinationPathName = Path.Combine(DestinationPath, rom.Name);

            return DestinationPathName;
        }

        public static bool MoveGameFile(long RomId, bool SourceIsExternal)
        {
            Classes.Roms.GameRomItem rom = Classes.Roms.GetRom(RomId);
            string romPath = rom.Path;
            if (SourceIsExternal == true)
            {
                romPath = rom.RelativePath;
            }

            if (File.Exists(romPath))
            {
                string DestinationPath = ComputeROMPath(RomId);

                if (romPath == DestinationPath)
                {
                    Logging.Log(Logging.LogType.Debug, "Move Game ROM", "Destination path is the same as the current path - aborting");
                    return true;
                }
                else
                {
                    Logging.Log(Logging.LogType.Information, "Move Game ROM", "Moving " + romPath + " to " + DestinationPath);
                    if (File.Exists(DestinationPath))
                    {
                        Logging.Log(Logging.LogType.Information, "Move Game ROM", "A file with the same name exists at the destination - aborting");
                        return false;
                    }
                    else
                    {
                        File.Move(romPath, DestinationPath);

                        // update the db
                        Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
                        string sql = "UPDATE Games_Roms SET RelativePath=@path WHERE Id=@id";
                        Dictionary<string, object> dbDict = new Dictionary<string, object>();
                        dbDict.Add("id", RomId);

                        string libraryRootPath = rom.Library.Path;
                        if (libraryRootPath.EndsWith(Path.DirectorySeparatorChar.ToString()) == false)
                        {
                            libraryRootPath += Path.DirectorySeparatorChar;
                        }
                        dbDict.Add("path", DestinationPath.Replace(libraryRootPath, ""));
                        db.ExecuteCMD(sql, dbDict);

                        return true;
                    }
                }
            }
            else
            {
                Logging.Log(Logging.LogType.Warning, "Move Game ROM", "File " + romPath + " appears to be missing!");
                return false;
            }
        }

        public void OrganiseLibrary()
        {
            Logging.Log(Logging.LogType.Information, "Organise Library", "Starting default library organisation");

            GameLibrary.LibraryItem library = GameLibrary.GetDefaultLibrary;

            // move rom files to their new location
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "SELECT * FROM view_Games_Roms WHERE LibraryId = @libraryid";
            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            dbDict.Add("libraryid", library.Id);
            DataTable romDT = db.ExecuteCMD(sql, dbDict);

            if (romDT.Rows.Count > 0)
            {
                for (int i = 0; i < romDT.Rows.Count; i++)
                {
                    SetStatus(i, romDT.Rows.Count, "Processing file " + romDT.Rows[i]["name"]);
                    Logging.Log(Logging.LogType.Information, "Organise Library", "(" + i + "/" + romDT.Rows.Count + ") Processing ROM " + romDT.Rows[i]["name"]);
                    long RomId = (long)romDT.Rows[i]["id"];
                    MoveGameFile(RomId, false);
                }
            }
            ClearStatus();

            // clean up empty directories
            DeleteOrphanedDirectories(GameLibrary.GetDefaultLibrary.Path);

            Logging.Log(Logging.LogType.Information, "Organise Library", "Finsihed default library organisation");
        }

        public static void DeleteOrphanedDirectories(string startLocation)
        {
            foreach (var directory in Directory.GetDirectories(startLocation))
            {
                DeleteOrphanedDirectories(directory);

                string[] files = Directory.GetFiles(directory);
                string[] directories = Directory.GetDirectories(directory);

                if (files.Length == 0 &&
                    directories.Length == 0)
                {
                    Directory.Delete(directory, false);
                }
            }
        }

        public static List<GameLibrary.LibraryItem> LibrariesToScan = new List<GameLibrary.LibraryItem>();
        public void LibraryScan()
        {
            int maxWorkers = 4;

            if (LibrariesToScan.Count == 0)
            {
                LibrariesToScan.AddRange(GameLibrary.GetLibraries);
            }

            // setup background tasks for each library
            do
            {
                Logging.Log(Logging.LogType.Information, "Library Scan", "Library scan queue size: " + LibrariesToScan.Count);

                GameLibrary.LibraryItem library = LibrariesToScan[0];
                LibrariesToScan.RemoveAt(0);

                // check if library is already being scanned
                bool libraryAlreadyScanning = false;
                List<ProcessQueue.QueueItem> ProcessQueueItems = new List<ProcessQueue.QueueItem>();
                ProcessQueueItems.AddRange(ProcessQueue.QueueItems);
                foreach (ProcessQueue.QueueItem item in ProcessQueueItems)
                {
                    if (item.ItemType == ProcessQueue.QueueItemType.LibraryScanWorker)
                    {
                        if (((GameLibrary.LibraryItem)item.Options).Id == library.Id)
                        {
                            libraryAlreadyScanning = true;
                        }
                    }
                }

                if (libraryAlreadyScanning == false)
                {
                    Logging.Log(Logging.LogType.Information, "Library Scan", "Starting worker process for library " + library.Name);
                    ProcessQueue.QueueItem queue = new ProcessQueue.QueueItem(
                        ProcessQueue.QueueItemType.LibraryScanWorker,
                        1,
                        new List<ProcessQueue.QueueItemType>
                        {
                        ProcessQueue.QueueItemType.OrganiseLibrary
                        },
                        false,
                        true)
                    {
                        Options = library
                    };
                    queue.ForceExecute();

                    ProcessQueue.QueueItems.Add(queue);

                    // check number of running tasks is less than maxWorkers
                    bool allowContinue;
                    do
                    {
                        allowContinue = true;
                        int currentWorkerCount = 0;
                        List<ProcessQueue.QueueItem> LibraryScan_QueueItems = new List<ProcessQueue.QueueItem>();
                        LibraryScan_QueueItems.AddRange(ProcessQueue.QueueItems);
                        foreach (ProcessQueue.QueueItem item in LibraryScan_QueueItems)
                        {
                            if (item.ItemType == ProcessQueue.QueueItemType.LibraryScanWorker)
                            {
                                currentWorkerCount += 1;
                            }
                        }
                        if (currentWorkerCount >= maxWorkers)
                        {
                            allowContinue = false;
                            Thread.Sleep(60000);
                        }
                    } while (allowContinue == false);
                }
            } while (LibrariesToScan.Count > 0);

            bool WorkersStillWorking;
            do
            {
                WorkersStillWorking = false;
                List<ProcessQueue.QueueItem> queueItems = new List<ProcessQueue.QueueItem>();
                queueItems.AddRange(ProcessQueue.QueueItems);
                foreach (ProcessQueue.QueueItem item in queueItems)
                {
                    if (item.ItemType == ProcessQueue.QueueItemType.LibraryScanWorker)
                    {
                        // workers are still running - sleep and keep looping
                        WorkersStillWorking = true;
                        Thread.Sleep(30000);
                    }
                }
            } while (WorkersStillWorking == true);

            Logging.Log(Logging.LogType.Information, "Library Scan", "Library scan complete. All workers stopped");

            if (LibrariesToScan.Count > 0)
            {
                Logging.Log(Logging.LogType.Information, "Library Scan", "There are still libraries to scan. Restarting scan process");
                LibraryScan();
            }
        }

        public void LibrarySpecificScan(GameLibrary.LibraryItem library)
        {

            Logging.Log(Logging.LogType.Information, "Library Scan", "Starting scan of library: " + library.Name);

            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);

            Logging.Log(Logging.LogType.Information, "Library Scan", "Looking for duplicate library files to clean up");
            string duplicateSql = "DELETE r1 FROM Games_Roms r1 INNER JOIN Games_Roms r2 WHERE r1.Id > r2.Id AND r1.MD5 = r2.MD5 AND r1.LibraryId=@libraryid AND r2.LibraryId=@libraryid;";
            Dictionary<string, object> dupDict = new Dictionary<string, object>();
            dupDict.Add("libraryid", library.Id);
            db.ExecuteCMD(duplicateSql, dupDict);

            string sql = "SELECT * FROM view_Games_Roms WHERE LibraryId=@libraryid ORDER BY `name`";
            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            dbDict.Add("libraryid", library.Id);
            DataTable dtRoms = db.ExecuteCMD(sql, dbDict);

            // clean out database entries in the import folder
            if (dtRoms.Rows.Count > 0)
            {
                for (var i = 0; i < dtRoms.Rows.Count; i++)
                {
                    long romId = (long)dtRoms.Rows[i]["Id"];
                    string romPath = (string)dtRoms.Rows[i]["Path"];

                    if (!romPath.StartsWith(library.Path))
                    {
                        Logging.Log(Logging.LogType.Information, "Library Scan", "Deleting database entry for files with incorrect directory " + romPath);
                        string deleteSql = "DELETE FROM Games_Roms WHERE Id=@id AND LibraryId=@libraryid";
                        Dictionary<string, object> deleteDict = new Dictionary<string, object>();
                        deleteDict.Add("Id", romId);
                        deleteDict.Add("libraryid", library.Id);
                        db.ExecuteCMD(deleteSql, deleteDict);
                    }
                }
            }

            sql = "SELECT * FROM view_Games_Roms WHERE LibraryId=@libraryid ORDER BY `name`";
            dtRoms = db.ExecuteCMD(sql, dbDict);

            // search for files in the library that aren't in the database
            Logging.Log(Logging.LogType.Information, "Library Scan", "Looking for orphaned library files to add");
            string[] LibraryFiles = Directory.GetFiles(library.Path, "*.*", SearchOption.AllDirectories);
            int StatusCount = 0;
            foreach (string LibraryFile in LibraryFiles)
            {
                SetStatus(StatusCount, LibraryFiles.Length, "Processing file " + LibraryFile);
                if (!Common.SkippableFiles.Contains<string>(Path.GetFileName(LibraryFile), StringComparer.OrdinalIgnoreCase))
                {
                    Common.hashObject LibraryFileHash = new Common.hashObject(LibraryFile);

                    // check if file is in database
                    bool romFound = false;
                    for (var i = 0; i < dtRoms.Rows.Count; i++)
                    {
                        long romId = (long)dtRoms.Rows[i]["Id"];
                        string romPath = (string)dtRoms.Rows[i]["Path"];
                        string romMd5 = (string)dtRoms.Rows[i]["MD5"];

                        if ((LibraryFile == romPath) || (LibraryFileHash.md5hash == romMd5))
                        {
                            romFound = true;
                            break;
                        }
                    }

                    if (romFound == false)
                    {
                        // file is not in database - process it
                        Logging.Log(Logging.LogType.Information, "Library Scan", "Orphaned file found in library: " + LibraryFile);

                        Common.hashObject hash = new Common.hashObject(LibraryFile);
                        FileInfo fi = new FileInfo(LibraryFile);

                        FileSignature fileSignature = new FileSignature();
                        gaseous_server.Models.Signatures_Games sig = fileSignature.GetFileSignature(library, hash, fi, LibraryFile);

                        try
                        {
                            // get discovered platform
                            long PlatformId;
                            Platform determinedPlatform;

                            if (sig.Flags.PlatformId == null || sig.Flags.PlatformId == 0)
                            {
                                // no platform discovered in the signature
                                PlatformId = library.DefaultPlatformId;
                            }
                            else
                            {
                                // use the platform discovered in the signature
                                PlatformId = (long)sig.Flags.PlatformId;
                            }
                            determinedPlatform = Platforms.GetPlatform(PlatformId);

                            gaseous_server.Models.Game determinedGame = SearchForGame(sig, PlatformId, true);

                            StoreGame(library, hash, sig, determinedPlatform, LibraryFile, 0, false);
                        }
                        catch (Exception ex)
                        {
                            Logging.Log(Logging.LogType.Warning, "Library Scan", "An error occurred while matching orphaned file: " + LibraryFile + ". Skipping.", ex);
                        }
                    }
                }
                StatusCount += 1;
            }
            ClearStatus();

            sql = "SELECT * FROM view_Games_Roms WHERE LibraryId=@libraryid ORDER BY `name`";
            dtRoms = db.ExecuteCMD(sql, dbDict);

            // check all roms to see if their local file still exists
            Logging.Log(Logging.LogType.Information, "Library Scan", "Checking library files exist on disk");
            StatusCount = 0;
            if (dtRoms.Rows.Count > 0)
            {
                for (var i = 0; i < dtRoms.Rows.Count; i++)
                {
                    long romId = (long)dtRoms.Rows[i]["Id"];
                    string romPath = (string)dtRoms.Rows[i]["Path"];
                    gaseous_server.Models.Signatures_Games.RomItem.SignatureSourceType romMetadataSource = (gaseous_server.Models.Signatures_Games.RomItem.SignatureSourceType)(int)dtRoms.Rows[i]["MetadataSource"];

                    SetStatus(StatusCount, dtRoms.Rows.Count, "Processing file " + romPath);
                    Logging.Log(Logging.LogType.Information, "Library Scan", "Processing ROM at path " + romPath);

                    if (File.Exists(romPath))
                    {
                        if (library.IsDefaultLibrary == true)
                        {
                            if (romPath != ComputeROMPath(romId))
                            {
                                Logging.Log(Logging.LogType.Information, "Library Scan", "ROM at path " + romPath + " found, but needs to be moved");
                                MoveGameFile(romId, false);
                            }
                            else
                            {
                                Logging.Log(Logging.LogType.Information, "Library Scan", "ROM at path " + romPath + " found");
                            }
                        }
                    }
                    else
                    {
                        // file doesn't exist where it's supposed to be! delete it from the db
                        Logging.Log(Logging.LogType.Warning, "Library Scan", "Deleting orphaned database entry for " + romPath);

                        string deleteSql = "DELETE FROM Games_Roms WHERE Id = @id AND LibraryId = @libraryid";
                        Dictionary<string, object> deleteDict = new Dictionary<string, object>();
                        deleteDict.Add("id", romId);
                        deleteDict.Add("libraryid", library.Id);
                        db.ExecuteCMD(deleteSql, deleteDict);
                    }

                    StatusCount += 1;
                }
            }

            Logging.Log(Logging.LogType.Information, "Library Scan", "Library scan completed");
        }
    }
}

