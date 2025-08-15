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
using HasheousClient.Models.Metadata.IGDB;
using Mono.TextTemplating;

namespace gaseous_server.Classes
{
    public class ImportGame : QueueItemStatus
    {
        public ImportGame()
        {

        }

        public ImportGame(object callingQueueItem)
        {
            CallingQueueItem = callingQueueItem;
        }

        /// <summary>
        /// List of import states
        /// </summary>
        private static readonly List<ImportStateItem> _importStates = new List<ImportStateItem>();

        /// <summary>
        /// Gets a read-only list of import state items.
        /// </summary>
        public static IReadOnlyList<ImportStateItem> ImportStates => _importStates.AsReadOnly();

        /// <summary>
        /// Add an import state to the list
        /// </summary>
        /// <param name="SessionId">
        /// The session ID of the import
        /// </param>
        /// <param name="FileName">
        /// The name of the file being imported
        /// </param>
        /// <param name="Method">
        /// The method of import (ImportDirectory or WebUpload)
        /// </param>
        /// <param name="PlatformOverride">
        /// The platform override for the import
        /// </param>
        public static void AddImportState(Guid SessionId, string FileName, ImportStateItem.ImportMethod Method, long? PlatformOverride = null)
        {
            _AddImportState(SessionId, FileName, Method, string.Empty, PlatformOverride);
        }

        /// <summary>
        /// Add an import state to the list
        /// </summary>
        /// <param name="SessionId">
        /// The session ID of the import
        /// </param>
        /// <param name="FileName">
        /// The name of the file being imported
        /// </param>
        /// <param name="Method">
        /// The method of import (ImportDirectory or WebUpload)
        /// </param>
        /// <param name="UserId">
        /// The user ID of the person importing the file, if the import is from the web
        /// </param>
        /// <param name="PlatformOverride">
        /// The platform override for the import
        /// </param>
        public static void AddImportState(Guid SessionId, string FileName, ImportStateItem.ImportMethod Method, string UserId, long? PlatformOverride = null)
        {
            _AddImportState(SessionId, FileName, Method, UserId, PlatformOverride);
        }

        private static void _AddImportState(Guid SessionId, string FileName, ImportStateItem.ImportMethod Method, string UserId = "", long? PlatformOverride = null)
        {
            // check if an import state for this session id or file exists
            ImportStateItem? existingImportState = _importStates.Find(x => x.SessionId == SessionId || x.FileName == FileName);
            if (existingImportState != null)
            {
                // update the existing import state
                existingImportState.FileName = FileName;
                existingImportState.Method = Method;
                existingImportState.UserId = UserId;
                existingImportState.PlatformOverride = PlatformOverride;
                existingImportState.LastUpdated = DateTime.UtcNow;
            }
            else
            {
                // create a new import state
                ImportStateItem importState = new ImportStateItem
                {
                    FileName = FileName,
                    Method = Method,
                    UserId = UserId,
                    PlatformOverride = PlatformOverride,
                    SessionId = SessionId
                };
                _importStates.Add(importState);

                Logging.Log(Logging.LogType.Information, "Import Game", String.Format("File {0} added to import queue via {1} by user {2}", FileName, Method.ToString(), UserId));

                // check if there is an ImportQueueProcessor running
                ProcessQueue.QueueItem? queueItem = ProcessQueue.QueueItems.Find(x => x.ItemType == ProcessQueue.QueueItemType.ImportQueueProcessor);
                if (queueItem != null)
                {
                    queueItem.ForceExecute();
                }
            }
        }

        /// <summary>
        /// Update the import state
        /// </summary>
        /// <param name="SessionId">
        /// The session ID of the import
        /// </param>
        /// <param name="State">
        /// The state of the import (Pending, Processing, Completed, Failed)
        /// </param>
        /// <param name="Type">
        /// The type of import (Unknown, Rom, BIOS)
        /// </param>
        /// <param name="ProcessData">
        /// A dictionary of process data to be added to the import state
        /// </param>
        public static void UpdateImportState(Guid SessionId, ImportStateItem.ImportState State, ImportStateItem.ImportType Type, Dictionary<string, object>? ProcessData = null)
        {
            ImportStateItem? importState = _importStates.Find(x => x.SessionId == SessionId);
            if (importState != null)
            {
                importState.State = State;
                importState.Type = Type;
                importState.LastUpdated = DateTime.UtcNow;
                if (ProcessData != null)
                {
                    foreach (KeyValuePair<string, object> kvp in ProcessData)
                    {
                        importState.ProcessData[kvp.Key] = kvp.Value;
                    }
                }
            }
        }

        /// <summary>
        /// Get the import state for a session ID
        /// </summary>
        /// <param name="SessionId">
        /// The session ID of the import
        /// </param>
        /// <returns>
        /// The import state item for the session ID
        /// </returns>
        public static ImportStateItem? GetImportState(Guid SessionId)
        {
            ImportStateItem? importState = _importStates.Find(x => x.SessionId == SessionId);
            if (importState != null)
            {
                return importState;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Remove an import state from the list
        /// </summary>
        /// <param name="SessionId">
        /// The session ID of the import
        /// </param>
        public static void RemoveImportState(Guid SessionId)
        {
            ImportStateItem? importState = _importStates.Find(x => x.SessionId == SessionId);
            if (importState != null)
            {
                _importStates.Remove(importState);
            }
        }

        /// <summary>
        /// Remove old import states from the list
        /// </summary>
        /// <remarks>
        /// This method removes import states that are older than 60 minutes and have a state of Completed or Failed.
        /// </remarks>
        public static void RemoveOldImportStates()
        {
            DateTime now = DateTime.UtcNow;
            TimeSpan timeSpan = new TimeSpan(0, 60, 0);
            DateTime cutoff = now.Subtract(timeSpan);

            // remove completed import states older than 60 minutes
            _importStates.RemoveAll(x => x.State == ImportStateItem.ImportState.Completed && x.LastUpdated < cutoff);
            // remove failed import states older than 60 minutes
            _importStates.RemoveAll(x => x.State == ImportStateItem.ImportState.Failed && x.LastUpdated < cutoff);
            // remove pending import states that don't have a file on disk
            _importStates.RemoveAll(x => x.State == ImportStateItem.ImportState.Pending && !File.Exists(x.FileName));
        }

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
                    AddImportState(Guid.NewGuid(), importContent, ImportStateItem.ImportMethod.ImportDirectory);

                    importCount += 1;
                }

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
        public static void ImportGameFile(string FilePath, HashObject Hash, ref Dictionary<string, object> GameFileInfo, Platform? OverridePlatform)
        {
            GameFileInfo.Add("type", "rom");

            // check to make sure we don't already have this file imported
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "";
            Dictionary<string, object> dbDict = new Dictionary<string, object>();

            sql = "SELECT COUNT(Id) AS count FROM view_Games_Roms WHERE MD5=@md5 AND SHA1=@sha1;";
            dbDict.Add("md5", Hash.md5hash);
            dbDict.Add("sha1", Hash.sha1hash);
            dbDict.Add("crc", Hash.crc32hash);
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
                gaseous_server.Models.Signatures_Games discoveredSignature = fileSignature.GetFileSignatureAsync(GameLibrary.GetDefaultLibrary, Hash, fi, FilePath).Result;
                if (discoveredSignature.Flags.GameId == 0)
                {
                    HasheousClient.Models.Metadata.IGDB.Game? discoveredGame = SearchForGame(discoveredSignature, discoveredSignature.Flags.PlatformId, false).Result;
                    if (discoveredGame != null && discoveredGame.Id != null)
                    {
                        discoveredSignature.MetadataSources.AddGame((long)discoveredGame.Id, discoveredGame.Name, MetadataSources.IGDB);
                    }
                }

                // add to database
                Platform? determinedPlatform = Metadata.Platforms.GetPlatform((long)discoveredSignature.Flags.PlatformId).Result;
                Models.Game? determinedGame = Metadata.Games.GetGame(discoveredSignature.Flags.GameMetadataSource, discoveredSignature.Flags.GameId).Result;
                MetadataMap? map = MetadataManagement.NewMetadataMap((long)determinedPlatform.Id, discoveredSignature.Game.Name);
                long RomId = StoreGame(GameLibrary.GetDefaultLibrary, Hash, discoveredSignature, determinedPlatform, FilePath, 0, true).Result;
                gaseous_server.Classes.Roms.GameRomItem romItem = Roms.GetRom(RomId).Result;

                // build return value
                GameFileInfo.Add("romid", RomId);
                GameFileInfo.Add("metadatamapid", map.Id);
                GameFileInfo.Add("platform", determinedPlatform);
                GameFileInfo.Add("game", determinedGame);
                GameFileInfo.Add("signature", discoveredSignature);
                GameFileInfo.Add("rom", romItem);
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
        public static async Task<long> StoreGame(GameLibrary.LibraryItem library, HashObject hash, Signatures_Games signature, Platform platform, string filePath, long romId = 0, bool SourceIsExternal = false)
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
            foreach (MetadataSources source in Enum.GetValues(typeof(MetadataSources)))
            {
                bool sourceExists = false;

                if (source != MetadataSources.None)
                {
                    // get the signature that matches this source
                    Signatures_Games.SourceValues.SourceValueItem? signatureSource = signature.MetadataSources.Games.Find(x => x.Source == source);
                    if (signatureSource == null)
                    {
                        Logging.Log(Logging.LogType.Information, "Import Game", "  No source found for " + source.ToString());
                        continue;
                    }

                    // get the metadata map for this source
                    map = MetadataManagement.NewMetadataMap((long)platform.Id, signature.Game.Name);
                    MetadataMap.MetadataMapItem? mapSource = map.MetadataMapItems.Find(x => x.SourceType == source);
                    if (mapSource == null)
                    {
                        // add the source to the map
                        bool preferred = false;
                        if (source == Config.MetadataConfiguration.DefaultMetadataSource)
                        {
                            preferred = true;
                        }
                        MetadataManagement.AddMetadataMapItem((long)map.Id, source, signatureSource.Id, preferred);
                    }
                    else
                    {
                        // update the source in the map - do not modify the preferred status
                        MetadataManagement.UpdateMetadataMapItem((long)map.Id, source, signatureSource.Id, null);
                    }
                }
            }

            // reload the map
            map = await MetadataManagement.GetMetadataMap((long)map.Id);

            // add or update the rom
            dbDict = new Dictionary<string, object>();
            if (romId == 0)
            {
                sql = "INSERT INTO Games_Roms (PlatformId, GameId, Name, Size, CRC, MD5, SHA1, SHA256, DevelopmentStatus, Attributes, RomType, RomTypeMedia, MediaLabel, RelativePath, MetadataSource, MetadataGameName, MetadataVersion, LibraryId, RomDataVersion, MetadataMapId) VALUES (@platformid, @gameid, @name, @size, @crc, @md5, @sha1, @sha256, @developmentstatus, @Attributes, @romtype, @romtypemedia, @medialabel, @path, @metadatasource, @metadatagamename, @metadataversion, @libraryid, @romdataversion, @metadatamapid); SELECT CAST(LAST_INSERT_ID() AS SIGNED);";
            }
            else
            {
                sql = "UPDATE Games_Roms SET PlatformId=@platformid, GameId=@gameid, Name=@name, Size=@size, CRC=@crc, MD5=@md5, SHA1=@sha1, SHA256=@sha256, DevelopmentStatus=@developmentstatus, Attributes=@Attributes, RomType=@romtype, RomTypeMedia=@romtypemedia, MediaLabel=@medialabel, MetadataSource=@metadatasource, MetadataGameName=@metadatagamename, MetadataVersion=@metadataversion, RomDataVersion=@romdataversion, MetadataMapId=@metadatamapid, DateUpdated=@dateupdated WHERE Id=@id;";
                dbDict.Add("id", romId);
            }
            dbDict.Add("platformid", Common.ReturnValueIfNull(platform.Id, 0));
            dbDict.Add("gameid", 0); // set to 0 - no longer required as game is mapped using the MetadataMapBridge table
            dbDict.Add("name", Common.ReturnValueIfNull(signature.Rom.Name, 0));
            dbDict.Add("size", Common.ReturnValueIfNull(signature.Rom.Size, 0));
            dbDict.Add("md5", hash.md5hash);
            dbDict.Add("sha1", hash.sha1hash);
            dbDict.Add("sha256", hash.sha256hash);
            dbDict.Add("crc", hash.crc32hash);
            dbDict.Add("developmentstatus", Common.ReturnValueIfNull(signature.Rom.DevelopmentStatus, ""));
            dbDict.Add("metadatasource", signature.Rom.SignatureSource);
            dbDict.Add("metadatagamename", Common.StripVersionsFromFileName(signature.Game.Name));
            dbDict.Add("metadataversion", 2);
            dbDict.Add("libraryid", library.Id);
            dbDict.Add("romdataversion", 2);
            dbDict.Add("metadatamapid", map.Id);
            dbDict.Add("dateupdated", DateTime.UtcNow);

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

            DataTable romInsert = await db.ExecuteCMDAsync(sql, dbDict);
            if (romId == 0)
            {
                romId = (long)romInsert.Rows[0][0];
            }

            // move to destination
            if (library.IsDefaultLibrary == true)
            {
                await MoveGameFile(romId, SourceIsExternal);
            }

            return romId;
        }

        public static async Task<gaseous_server.Models.Game> SearchForGame(gaseous_server.Models.Signatures_Games Signature, long PlatformId, bool FullDownload)
        {
            if (Signature.Flags != null)
            {
                if (Signature.Flags.GameId != null && Signature.Flags.GameId != 0)
                {
                    // game was determined elsewhere - probably a Hasheous server
                    try
                    {
                        return await Games.GetGame(MetadataSources.IGDB, Signature.Flags.GameId);
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
                    gaseous_server.Models.Game[] games = await Metadata.Games.SearchForGame(SearchCandidate, PlatformId, searchType);
                    if (games != null)
                    {
                        if (games.Length == 1)
                        {
                            // exact match!
                            determinedGame = await Metadata.Games.GetGame(MetadataSources.IGDB, (long)games[0].Id);
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
                                    determinedGame = await Metadata.Games.GetGame(MetadataSources.IGDB, (long)games[0].Id);
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

        public static async Task<List<gaseous_server.Models.Game>> SearchForGame_GetAll(string GameName, long PlatformId)
        {
            List<gaseous_server.Models.Game> searchResults = new List<gaseous_server.Models.Game>();

            List<string> SearchCandidates = GetSearchCandidates(GameName);

            foreach (string SearchCandidate in SearchCandidates)
            {
                foreach (Metadata.Games.SearchType searchType in Enum.GetValues(typeof(Metadata.Games.SearchType)))
                {
                    if ((PlatformId == 0 && searchType == SearchType.searchNoPlatform) || (PlatformId != 0 && searchType != SearchType.searchNoPlatform))
                    {
                        gaseous_server.Models.Game[] games = await Metadata.Games.SearchForGame(SearchCandidate, PlatformId, searchType);
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
            GameName = Common.StripVersionsFromFileName(GameName);

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

        public static async Task<string> ComputeROMPath(long RomId)
        {
            Classes.Roms.GameRomItem rom = await Classes.Roms.GetRom(RomId);

            // get metadata
            MetadataMap.MetadataMapItem metadataMap = (await Classes.MetadataManagement.GetMetadataMap(rom.MetadataMapId)).PreferredMetadataMapItem;
            Platform? platform = await gaseous_server.Classes.Metadata.Platforms.GetPlatform(rom.PlatformId);
            gaseous_server.Models.Game? game = await Classes.Metadata.Games.GetGame(metadataMap.SourceType, metadataMap.SourceId);

            // build path
            string platformSlug = "Unknown Platform";
            if (platform != null)
            {
                platformSlug = platform.Slug;
            }
            string gameSlug = "Unknown Title";
            if (game != null)
            {
                if (game.Slug != null)
                {
                    gameSlug = game.Slug;
                }
                else
                {
                    gameSlug = game.Name;
                }
            }
            string DestinationPath = Path.Combine(GameLibrary.GetDefaultLibrary.Path, platformSlug, gameSlug);
            if (!Directory.Exists(DestinationPath))
            {
                Directory.CreateDirectory(DestinationPath);
            }

            string DestinationPathName = Path.Combine(DestinationPath, rom.Name);

            return DestinationPathName;
        }

        public static async Task<bool> MoveGameFile(long RomId, bool SourceIsExternal)
        {
            Classes.Roms.GameRomItem rom = Classes.Roms.GetRom(RomId).Result;
            string romPath = rom.Path;
            if (SourceIsExternal == true)
            {
                romPath = rom.RelativePath;
            }

            if (File.Exists(romPath))
            {
                string DestinationPath = await ComputeROMPath(RomId);

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
                        await db.ExecuteCMDAsync(sql, dbDict);

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

        public async Task OrganiseLibrary()
        {
            Logging.Log(Logging.LogType.Information, "Organise Library", "Starting default library organisation");

            GameLibrary.LibraryItem library = GameLibrary.GetDefaultLibrary;

            // move rom files to their new location
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "SELECT * FROM view_Games_Roms WHERE LibraryId = @libraryid";
            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            dbDict.Add("libraryid", library.Id);
            DataTable romDT = await db.ExecuteCMDAsync(sql, dbDict);

            if (romDT.Rows.Count > 0)
            {
                for (int i = 0; i < romDT.Rows.Count; i++)
                {
                    SetStatus(i, romDT.Rows.Count, "Processing file " + romDT.Rows[i]["name"]);
                    Logging.Log(Logging.LogType.Information, "Organise Library", "(" + i + "/" + romDT.Rows.Count + ") Processing ROM " + romDT.Rows[i]["name"]);
                    long RomId = (long)romDT.Rows[i]["id"];
                    await MoveGameFile(RomId, false);
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

        public async Task LibrarySpecificScan(GameLibrary.LibraryItem library)
        {
            Logging.Log(Logging.LogType.Information, "Library Scan", "Starting scan of library: " + library.Name);

            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);

            // force load of platform mapping tables
            List<PlatformMapping.PlatformMapItem> _platformMap = PlatformMapping.PlatformMap;

            Logging.Log(Logging.LogType.Information, "Library Scan", "Looking for duplicate library files to clean up");
            string duplicateSql = "DELETE r1 FROM Games_Roms r1 INNER JOIN Games_Roms r2 WHERE r1.Id > r2.Id AND r1.MD5 = r2.MD5 AND r1.LibraryId=@libraryid AND r2.LibraryId=@libraryid;";
            Dictionary<string, object> dupDict = new Dictionary<string, object>();
            dupDict.Add("libraryid", library.Id);
            await db.ExecuteCMDAsync(duplicateSql, dupDict);

            string sql = "SELECT * FROM view_Games_Roms WHERE LibraryId=@libraryid ORDER BY `name`";
            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            dbDict.Add("libraryid", library.Id);
            DataTable dtRoms = await db.ExecuteCMDAsync(sql, dbDict);

            // Remove database entries for files not in the correct library directory
            if (dtRoms.Rows.Count > 0)
            {
                var rowsToDelete = dtRoms.AsEnumerable()
                    .Where(row => !((string)row["Path"]).StartsWith(library.Path))
                    .Select(row => (Id: (long)row["Id"], Path: (string)row["Path"]))
                    .ToList();

                foreach (var entry in rowsToDelete)
                {
                    Logging.Log(Logging.LogType.Information, "Library Scan", $"Deleting database entry for file with incorrect directory {entry.Path}");
                    string deleteSql = "DELETE FROM Games_Roms WHERE Id=@id AND LibraryId=@libraryid";
                    var deleteDict = new Dictionary<string, object>
                    {
                        { "id", entry.Id },
                        { "libraryid", library.Id }
                    };
                    await db.ExecuteCMDAsync(deleteSql, deleteDict);
                }
            }

            sql = "SELECT * FROM view_Games_Roms WHERE LibraryId=@libraryid ORDER BY `name`";
            dtRoms = await db.ExecuteCMDAsync(sql, dbDict);

            // search for files in the library that aren't in the database
            Logging.Log(Logging.LogType.Information, "Library Scan", "Looking for orphaned library files to add");
            string[] LibraryFiles = Directory.GetFiles(library.Path, "*.*", SearchOption.AllDirectories);
            int StatusCount = 0;
            foreach (string LibraryFile in LibraryFiles)
            {
                SetStatus(StatusCount, LibraryFiles.Length, "Processing file " + LibraryFile);
                if (
                    !Common.SkippableFiles.Contains<string>(Path.GetFileName(LibraryFile), StringComparer.OrdinalIgnoreCase) &&
                    PlatformMapping.SupportedFileExtensions.Contains(Path.GetExtension(LibraryFile), StringComparer.OrdinalIgnoreCase)
                    )
                {
                    // check if file is in database
                    bool romFound = dtRoms.AsEnumerable().Any(row =>
                        (string)row["Path"] == LibraryFile
                    );

                    if (romFound == false)
                    {
                        // file is not in database - process it
                        Logging.Log(Logging.LogType.Information, "Library Scan", "Orphaned file found in library: " + LibraryFile);

                        HashObject hash = new HashObject(LibraryFile);
                        FileInfo fi = new FileInfo(LibraryFile);

                        FileSignature fileSignature = new FileSignature();
                        gaseous_server.Models.Signatures_Games sig = await fileSignature.GetFileSignatureAsync(library, hash, fi, LibraryFile);

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
                            determinedPlatform = await Platforms.GetPlatform(PlatformId);

                            gaseous_server.Models.Game determinedGame = await SearchForGame(sig, PlatformId, true);

                            await StoreGame(library, hash, sig, determinedPlatform, LibraryFile, 0, false);
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
            dtRoms = await db.ExecuteCMDAsync(sql, dbDict);

            // Check all roms to see if their local file still exists
            Logging.Log(Logging.LogType.Information, "Library Scan", "Checking library files exist on disk");
            StatusCount = 0;

            if (dtRoms.Rows.Count > 0)
            {
                string[] supportedExtensions = PlatformMapping.SupportedFileExtensions.ToArray();
                HashSet<string> skippableFiles = new HashSet<string>(Common.SkippableFiles, StringComparer.OrdinalIgnoreCase);

                for (var i = 0; i < dtRoms.Rows.Count; i++, StatusCount++)
                {
                    long romId = (long)dtRoms.Rows[i]["Id"];
                    string romPath = (string)dtRoms.Rows[i]["Path"];

                    SetStatus(StatusCount, dtRoms.Rows.Count, $"Processing file {romPath}");
                    Logging.Log(Logging.LogType.Information, "Library Scan", $"Processing ROM at path {romPath}");

                    string fileName = Path.GetFileName(romPath);
                    string fileExt = Path.GetExtension(romPath);

                    bool fileExists = File.Exists(romPath);
                    bool isSkippable = skippableFiles.Contains(fileName);
                    bool isSupportedExt = supportedExtensions.Contains(fileExt, StringComparer.OrdinalIgnoreCase);

                    if (fileExists && !isSkippable && isSupportedExt)
                    {
                        if (library.IsDefaultLibrary && romPath != await ComputeROMPath(romId))
                        {
                            Logging.Log(Logging.LogType.Information, "Library Scan", $"ROM at path {romPath} found, but needs to be moved");
                            await MoveGameFile(romId, false);
                        }
                        else
                        {
                            Logging.Log(Logging.LogType.Information, "Library Scan", $"ROM at path {romPath} found");
                        }
                    }
                    else
                    {
                        // File doesn't exist where it's supposed to be! Delete it from the db
                        Logging.Log(Logging.LogType.Warning, "Library Scan", $"Deleting orphaned database entry for {romPath}");

                        string deleteSql = "DELETE FROM Games_Roms WHERE Id = @id AND LibraryId = @libraryid";
                        var deleteDict = new Dictionary<string, object>
                        {
                            { "id", romId },
                            { "libraryid", library.Id }
                        };
                        await db.ExecuteCMDAsync(deleteSql, deleteDict);
                    }
                }
            }

            Logging.Log(Logging.LogType.Information, "Library Scan", "Library scan completed");
        }
    }
}
