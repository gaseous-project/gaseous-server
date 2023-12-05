using System;
using System.Data;
using System.IO.Compression;
using System.Security.Policy;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using gaseous_server.Classes.Metadata;
using IGDB.Models;
using NuGet.Common;
using static gaseous_server.Classes.Metadata.Games;

namespace gaseous_server.Classes
{
	public class ImportGames
	{
		public ImportGames(string ImportPath)
		{
			if (Directory.Exists(ImportPath))
			{
				string[] importContents_Files = Directory.GetFiles(ImportPath);
                string[] importContents_Directories = Directory.GetDirectories(ImportPath);

				// import files first
				foreach (string importContent in importContents_Files) {
					ImportGame.ImportGameFile(importContent, null);
				}

                // import sub directories
                foreach (string importDir in importContents_Directories) {
                    Classes.ImportGames importGames = new Classes.ImportGames(importDir);
                }
            }
			else
			{
				Logging.Log(Logging.LogType.Critical, "Import Games", "The import directory " + ImportPath + " does not exist.");
				throw new DirectoryNotFoundException("Invalid path: " + ImportPath);
			}
		}


	}

	public class ImportGame
	{
        public static void ImportGameFile(string GameFileImportPath, IGDB.Models.Platform? OverridePlatform)
		{
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
			string sql = "";
			Dictionary<string, object> dbDict = new Dictionary<string, object>();

            if (Common.SkippableFiles.Contains<string>(Path.GetFileName(GameFileImportPath), StringComparer.OrdinalIgnoreCase))
			{ 
                Logging.Log(Logging.LogType.Debug, "Import Game", "Skipping item " + GameFileImportPath);
            }
			else
			{
                FileInfo fi = new FileInfo(GameFileImportPath);
                Common.hashObject hash = new Common.hashObject(GameFileImportPath);

                Models.PlatformMapping.PlatformMapItem? IsBios = Classes.Bios.BiosHashSignatureLookup(hash.md5hash);

                if (IsBios == null)
                {
                    // file is a rom
                    // check to make sure we don't already have this file imported
                    sql = "SELECT COUNT(Id) AS count FROM Games_Roms WHERE MD5=@md5 AND SHA1=@sha1";
                    dbDict.Add("md5", hash.md5hash);
                    dbDict.Add("sha1", hash.sha1hash);
                    DataTable importDB = db.ExecuteCMD(sql, dbDict);
                    if ((Int64)importDB.Rows[0]["count"] > 0)
                    {
                        // import source was the import directory
                        if (GameFileImportPath.StartsWith(Config.LibraryConfiguration.LibraryImportDirectory))
                        {
                            Logging.Log(Logging.LogType.Warning, "Import Game", "  " + GameFileImportPath + " already in database - moving to " + Config.LibraryConfiguration.LibraryImportDuplicatesDirectory);

                            string targetPathWithFileName = GameFileImportPath.Replace(Config.LibraryConfiguration.LibraryImportDirectory, Config.LibraryConfiguration.LibraryImportDuplicatesDirectory);
                            string targetPath = Path.GetDirectoryName(targetPathWithFileName);

                            if (!Directory.Exists(targetPath))
                            {
                                Directory.CreateDirectory(targetPath);
                            }
                            File.Move(GameFileImportPath, targetPathWithFileName, true);
                        }
                        
                        // import source was the upload directory
                        if (GameFileImportPath.StartsWith(Config.LibraryConfiguration.LibraryUploadDirectory))
                        {
                            Logging.Log(Logging.LogType.Warning, "Import Game", "  " + GameFileImportPath + " already in database - skipping import");
                        }
                    }
                    else
                    {
                        Logging.Log(Logging.LogType.Information, "Import Game", "  " + GameFileImportPath + " not in database - processing");

                        Models.Signatures_Games discoveredSignature = GetFileSignature(hash, fi, GameFileImportPath);

                        // get discovered platform
                        IGDB.Models.Platform? determinedPlatform = null;
                        if (OverridePlatform == null)
                        {
                            determinedPlatform = Metadata.Platforms.GetPlatform(discoveredSignature.Flags.IGDBPlatformId);
                            if (determinedPlatform == null)
                            {
                                determinedPlatform = new IGDB.Models.Platform();
                            }
                        }
                        else
                        {
                            determinedPlatform = OverridePlatform;
                            discoveredSignature.Flags.IGDBPlatformId = (long)determinedPlatform.Id;
                            discoveredSignature.Flags.IGDBPlatformName = determinedPlatform.Name;
                        }

                        IGDB.Models.Game determinedGame = SearchForGame(discoveredSignature.Game.Name, discoveredSignature.Flags.IGDBPlatformId);

                        // add to database
                        StoreROM(GameLibrary.GetDefaultLibrary, hash, determinedGame, determinedPlatform, discoveredSignature, GameFileImportPath);
                    }
                }
                else
                {
                    // file is a bios
                    if (IsBios.WebEmulator != null)
                    {
                        foreach (Classes.Bios.BiosItem biosItem in Classes.Bios.GetBios())
                        {
                            if (biosItem.Available == false && biosItem.hash == hash.md5hash)
                            {
                                string biosPath = biosItem.biosPath.Replace(biosItem.filename, "");
                                if (!Directory.Exists(biosPath))
                                {
                                    Directory.CreateDirectory(biosPath);
                                }

                                File.Move(GameFileImportPath, biosItem.biosPath, true);

                                break;
                            }
                        }
                    }
                }
			}
		}

        public static Models.Signatures_Games GetFileSignature(Common.hashObject hash, FileInfo fi, string GameFileImportPath)
        {
            Models.Signatures_Games discoveredSignature = _GetFileSignature(hash, fi, GameFileImportPath);

            if ((Path.GetExtension(GameFileImportPath) == ".zip") && (fi.Length < 1073741824))
            {
                // file is a zip and less than 1 GiB
                // extract the zip file and search the contents
                string ExtractPath = Path.Combine(Config.LibraryConfiguration.LibraryTempDirectory, Path.GetRandomFileName());
                if (!Directory.Exists(ExtractPath)) { Directory.CreateDirectory(ExtractPath); }
                try
                {
                    ZipFile.ExtractToDirectory(GameFileImportPath, ExtractPath);

                    // loop through contents until we find the first signature match
                    foreach (string file in Directory.GetFiles(ExtractPath))
                    {
                        FileInfo zfi = new FileInfo(file);
                        Common.hashObject zhash = new Common.hashObject(file);

                        Models.Signatures_Games zDiscoveredSignature = _GetFileSignature(zhash, zfi, file);
                        zDiscoveredSignature.Rom.Name = Path.ChangeExtension(zDiscoveredSignature.Rom.Name, ".zip");

                        if (zDiscoveredSignature.Score > discoveredSignature.Score)
                        {
                            if (
                                zDiscoveredSignature.Rom.SignatureSource == gaseous_signature_parser.models.RomSignatureObject.RomSignatureObject.Game.Rom.SignatureSourceType.MAMEArcade || 
                                zDiscoveredSignature.Rom.SignatureSource == gaseous_signature_parser.models.RomSignatureObject.RomSignatureObject.Game.Rom.SignatureSourceType.MAMEMess
                            )
                            {
                                zDiscoveredSignature.Rom.Name = zDiscoveredSignature.Game.Description + ".zip";
                            }
                            zDiscoveredSignature.Rom.Crc = discoveredSignature.Rom.Crc;
                            zDiscoveredSignature.Rom.Md5 = discoveredSignature.Rom.Md5;
                            zDiscoveredSignature.Rom.Sha1 = discoveredSignature.Rom.Sha1;
                            zDiscoveredSignature.Rom.Size = discoveredSignature.Rom.Size;
                            discoveredSignature = zDiscoveredSignature;

                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logging.Log(Logging.LogType.Critical, "Get Signature", "Error processing zip file: " + GameFileImportPath, ex);
                }

                if (Directory.Exists(ExtractPath)) { Directory.Delete(ExtractPath, true); }
            }

            return discoveredSignature;
        }

		private static Models.Signatures_Games _GetFileSignature(Common.hashObject hash, FileInfo fi, string GameFileImportPath)
		{
            // check 1: do we have a signature for it?
            gaseous_server.Controllers.SignaturesController sc = new Controllers.SignaturesController();
            List<Models.Signatures_Games> signatures = sc.GetSignature(hash.md5hash);
            if (signatures.Count == 0)
            {
                // no md5 signature found - try sha1
                signatures = sc.GetSignature("", hash.sha1hash);
            }

            Models.Signatures_Games discoveredSignature = new Models.Signatures_Games();
            if (signatures.Count == 1)
            {
                // only 1 signature found!
                discoveredSignature = signatures.ElementAt(0);
                gaseous_server.Models.PlatformMapping.GetIGDBPlatformMapping(ref discoveredSignature, fi, false);
            }
            else if (signatures.Count > 1)
            {
                // more than one signature found - find one with highest score
                foreach (Models.Signatures_Games Sig in signatures)
                {
                    if (Sig.Score > discoveredSignature.Score)
                    {
                        discoveredSignature = Sig;
                        gaseous_server.Models.PlatformMapping.GetIGDBPlatformMapping(ref discoveredSignature, fi, false);
                    }
                }
            }
            else
            {
                // no signature match found - try name search
                signatures = sc.GetByTosecName(fi.Name);

                if (signatures.Count == 1)
                {
                    // only 1 signature found!
                    discoveredSignature = signatures.ElementAt(0);
                    gaseous_server.Models.PlatformMapping.GetIGDBPlatformMapping(ref discoveredSignature, fi, false);
                }
                else if (signatures.Count > 1)
                {
                    // more than one signature found - find one with highest score
                    foreach (Models.Signatures_Games Sig in signatures)
                    {
                        if (Sig.Score > discoveredSignature.Score)
                        {
                            discoveredSignature = Sig;
                            gaseous_server.Models.PlatformMapping.GetIGDBPlatformMapping(ref discoveredSignature, fi, false);
                        }
                    }
                }
                else
                {
                    // still no search - try alternate method
                    Models.Signatures_Games.GameItem gi = new Models.Signatures_Games.GameItem();
                    Models.Signatures_Games.RomItem ri = new Models.Signatures_Games.RomItem();

                    discoveredSignature.Game = gi;
                    discoveredSignature.Rom = ri;

                    // game title is the file name without the extension or path
                    gi.Name = Path.GetFileNameWithoutExtension(GameFileImportPath);

                    // remove everything after brackets - leaving (hopefully) only the name
                    if (gi.Name.Contains("("))
                    {
                        gi.Name = gi.Name.Substring(0, gi.Name.IndexOf("("));
                    }

                    // remove special characters like dashes
                    gi.Name = gi.Name.Replace("-", "");

                    // guess platform
                    gaseous_server.Models.PlatformMapping.GetIGDBPlatformMapping(ref discoveredSignature, fi, true);

                    // get rom data
                    ri.Name = Path.GetFileName(GameFileImportPath);
                    ri.Md5 = hash.md5hash;
                    ri.Sha1 = hash.sha1hash;
                    ri.Size = fi.Length;
                    ri.SignatureSource = gaseous_signature_parser.models.RomSignatureObject.RomSignatureObject.Game.Rom.SignatureSourceType.None;
                }
            }

            Logging.Log(Logging.LogType.Information, "Import Game", "  Determined import file as: " + discoveredSignature.Game.Name + " (" + discoveredSignature.Game.Year + ") " + discoveredSignature.Game.System);

            return discoveredSignature;
        }

        public static IGDB.Models.Game SearchForGame(string GameName, long PlatformId)
        {
            // search discovered game - case insensitive exact match first
            IGDB.Models.Game determinedGame = new IGDB.Models.Game();

            List<string> SearchCandidates = GetSearchCandidates(GameName);
            
            foreach (string SearchCandidate in SearchCandidates)
            {
                bool GameFound = false;

                Logging.Log(Logging.LogType.Information, "Import Game", "  Searching for title: " + SearchCandidate);

                foreach (Metadata.Games.SearchType searchType in Enum.GetValues(typeof(Metadata.Games.SearchType)))
                {
                    Logging.Log(Logging.LogType.Information, "Import Game", "  Search type: " + searchType.ToString());
                    IGDB.Models.Game[] games = Metadata.Games.SearchForGame(SearchCandidate, PlatformId, searchType);
                    if (games.Length == 1)
                    {
                        // exact match!
                        determinedGame = Metadata.Games.GetGame((long)games[0].Id, false, false, false);
                        Logging.Log(Logging.LogType.Information, "Import Game", "  IGDB game: " + determinedGame.Name);
                        GameFound = true;
                        break;
                    }
                    else if (games.Length > 0)
                    {
                        Logging.Log(Logging.LogType.Information, "Import Game", "  " + games.Length + " search results found");

                        // quite likely we've found sequels and alternate types
                        foreach (Game game in games) {
                            if (game.Name == SearchCandidate) {
                                // found game title matches the search candidate
                                determinedGame = Metadata.Games.GetGame((long)games[0].Id, false, false, false);
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
                if (GameFound == true) { break; }
            }
            if (determinedGame == null)
            {
                determinedGame = new IGDB.Models.Game();
            }

            string destSlug = "";
            if (determinedGame.Id == null)
            {
                Logging.Log(Logging.LogType.Information, "Import Game", "  Unable to determine game");
            }

            return determinedGame;
        }

        public static List<IGDB.Models.Game> SearchForGame_GetAll(string GameName, long PlatformId)
        {
            List<IGDB.Models.Game> searchResults = new List<IGDB.Models.Game>();

            List<string> SearchCandidates = GetSearchCandidates(GameName);

            foreach (string SearchCandidate in SearchCandidates)
            {
                foreach (Metadata.Games.SearchType searchType in Enum.GetValues(typeof(Metadata.Games.SearchType)))
                {
                    if ((PlatformId == 0 && searchType == SearchType.searchNoPlatform) || (PlatformId != 0 && searchType != SearchType.searchNoPlatform))
                    {
                        IGDB.Models.Game[] games = Metadata.Games.SearchForGame(SearchCandidate, PlatformId, searchType);
                        foreach (IGDB.Models.Game foundGame in games)
                        {
                            bool gameInResults = false;
                            foreach (IGDB.Models.Game searchResult in searchResults)
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
            if (idx >= 0) {
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

        public static long StoreROM(GameLibrary.LibraryItem library, Common.hashObject hash, IGDB.Models.Game determinedGame, IGDB.Models.Platform determinedPlatform, Models.Signatures_Games discoveredSignature, string GameFileImportPath, long UpdateId = 0)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);

            string sql = "";

            Dictionary<string, object> dbDict = new Dictionary<string, object>();

            if (UpdateId == 0)
            {
                sql = "INSERT INTO Games_Roms (PlatformId, GameId, Name, Size, CRC, MD5, SHA1, DevelopmentStatus, Attributes, RomType, RomTypeMedia, MediaLabel, Path, MetadataSource, MetadataGameName, MetadataVersion, LibraryId) VALUES (@platformid, @gameid, @name, @size, @crc, @md5, @sha1, @developmentstatus, @Attributes, @romtype, @romtypemedia, @medialabel, @path, @metadatasource, @metadatagamename, @metadataversion, @libraryid); SELECT CAST(LAST_INSERT_ID() AS SIGNED);";
            } else
            {
                sql = "UPDATE Games_Roms SET PlatformId=@platformid, GameId=@gameid, Name=@name, Size=@size, DevelopmentStatus=@developmentstatus, Attributes=@Attributes, RomType=@romtype, RomTypeMedia=@romtypemedia, MediaLabel=@medialabel, MetadataSource=@metadatasource, MetadataGameName=@metadatagamename, MetadataVersion=@metadataversion WHERE Id=@id;";
                dbDict.Add("id", UpdateId);
            }
            dbDict.Add("platformid", Common.ReturnValueIfNull(determinedPlatform.Id, 0));
            dbDict.Add("gameid", Common.ReturnValueIfNull(determinedGame.Id, 0));
            dbDict.Add("name", Common.ReturnValueIfNull(discoveredSignature.Rom.Name, 0));
            dbDict.Add("size", Common.ReturnValueIfNull(discoveredSignature.Rom.Size, 0));
            dbDict.Add("md5", hash.md5hash);
            dbDict.Add("sha1", hash.sha1hash);
            dbDict.Add("crc", Common.ReturnValueIfNull(discoveredSignature.Rom.Crc, ""));
            dbDict.Add("developmentstatus", Common.ReturnValueIfNull(discoveredSignature.Rom.DevelopmentStatus, ""));
            dbDict.Add("metadatasource", discoveredSignature.Rom.SignatureSource);
            dbDict.Add("metadatagamename", discoveredSignature.Game.Name);
            dbDict.Add("metadataversion", 2);
            dbDict.Add("libraryid", library.Id);

            if (discoveredSignature.Rom.Attributes != null)
            {
                if (discoveredSignature.Rom.Attributes.Count > 0)
                {
                    dbDict.Add("attributes", Newtonsoft.Json.JsonConvert.SerializeObject(discoveredSignature.Rom.Attributes));
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
            dbDict.Add("romtype", (int)discoveredSignature.Rom.RomType);
            dbDict.Add("romtypemedia", Common.ReturnValueIfNull(discoveredSignature.Rom.RomTypeMedia, ""));
            dbDict.Add("medialabel", Common.ReturnValueIfNull(discoveredSignature.Rom.MediaLabel, ""));
            dbDict.Add("path", GameFileImportPath);

            DataTable romInsert = db.ExecuteCMD(sql, dbDict);
            long romId = 0;
            if (UpdateId == 0)
            {
                romId = (long)romInsert.Rows[0][0];
            } else
            {
                romId = UpdateId;
            }

            // move to destination
            if (library.IsDefaultLibrary == true)
            {
                MoveGameFile(romId);
            }

            return romId;
        }

		public static string ComputeROMPath(long RomId)
		{
			Classes.Roms.GameRomItem rom = Classes.Roms.GetRom(RomId);

			// get metadata
			IGDB.Models.Platform platform = gaseous_server.Classes.Metadata.Platforms.GetPlatform(rom.PlatformId);
			IGDB.Models.Game game = gaseous_server.Classes.Metadata.Games.GetGame(rom.GameId, false, false, false);

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
			string DestinationPath = Path.Combine(GameLibrary.GetDefaultLibrary.Path, gameSlug, platformSlug);
			if (!Directory.Exists(DestinationPath))
			{
				Directory.CreateDirectory(DestinationPath);
			}

			string DestinationPathName = Path.Combine(DestinationPath, rom.Name);

			return DestinationPathName;
		}

		public static bool MoveGameFile(long RomId)
		{
            Classes.Roms.GameRomItem rom = Classes.Roms.GetRom(RomId);
			string romPath = rom.Path;

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
						string sql = "UPDATE Games_Roms SET Path=@path WHERE Id=@id";
						Dictionary<string, object> dbDict = new Dictionary<string, object>();
						dbDict.Add("id", RomId);
						dbDict.Add("path", DestinationPath);
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

		public static void OrganiseLibrary()
		{
            Logging.Log(Logging.LogType.Information, "Organise Library", "Starting default library organisation");

            GameLibrary.LibraryItem library = GameLibrary.GetDefaultLibrary;

            // move rom files to their new location
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "SELECT * FROM Games_Roms WHERE LibraryId = @libraryid";
            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            dbDict.Add("libraryid", library.Id);
            DataTable romDT = db.ExecuteCMD(sql, dbDict);

            if (romDT.Rows.Count > 0)
			{
				foreach (DataRow dr in romDT.Rows)
				{
					Logging.Log(Logging.LogType.Information, "Organise Library", "Processing ROM " + dr["name"]);
                    long RomId = (long)dr["id"];
					MoveGameFile(RomId);
				}
			}

			// clean up empty directories
			DeleteOrphanedDirectories(GameLibrary.GetDefaultLibrary.Path);

            Logging.Log(Logging.LogType.Information, "Organise Library", "Finsihed default library organisation");
        }

        public static void DeleteOrphanedDirectories(string startLocation)
        {
            foreach (var directory in Directory.GetDirectories(startLocation))
            {
                DeleteOrphanedDirectories(directory);
                if (Directory.GetFiles(directory).Length == 0 &&
                    Directory.GetDirectories(directory).Length == 0)
                {
                    Directory.Delete(directory, false);
                }
            }
        }

        public static void LibraryScan()
        {
            foreach (GameLibrary.LibraryItem library in GameLibrary.GetLibraries)
            {
                Logging.Log(Logging.LogType.Information, "Library Scan", "Starting library scan. Library " + library.Name);

                Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);

                Logging.Log(Logging.LogType.Information, "Library Scan", "Looking for duplicate library files to clean up");
                string duplicateSql = "DELETE r1 FROM Games_Roms r1 INNER JOIN Games_Roms r2 WHERE r1.Id > r2.Id AND r1.MD5 = r2.MD5 AND r1.LibraryId=@libraryid AND r2.LibraryId=@libraryid;";
                Dictionary<string, object> dupDict = new Dictionary<string, object>();
                dupDict.Add("libraryid", library.Id);
                db.ExecuteCMD(duplicateSql, dupDict);

                string sql = "SELECT * FROM Games_Roms WHERE LibraryId=@libraryid ORDER BY `name`";
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
                            Logging.Log(Logging.LogType.Information, "Library Scan", " Deleting database entry for files with incorrect directory " + romPath);
                            string deleteSql = "DELETE FROM Games_Roms WHERE Id=@id AND LibraryId=@libraryid";
                            Dictionary<string, object> deleteDict = new Dictionary<string, object>();
                            deleteDict.Add("Id", romId);
                            deleteDict.Add("libraryid", library.Id);
                            db.ExecuteCMD(deleteSql, deleteDict);
                        }
                    }
                }

                sql = "SELECT * FROM Games_Roms ORDER BY `name`";
                dtRoms = db.ExecuteCMD(sql, dbDict);

                // search for files in the library that aren't in the database
                Logging.Log(Logging.LogType.Information, "Library Scan", "Looking for orphaned library files to add");
                string[] LibraryFiles = Directory.GetFiles(library.Path, "*.*", SearchOption.AllDirectories);
                foreach (string LibraryFile in LibraryFiles)
                {
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
                            Common.hashObject hash = new Common.hashObject(LibraryFile);
                            FileInfo fi = new FileInfo(LibraryFile);

                            Models.Signatures_Games sig = GetFileSignature(hash, fi, LibraryFile);

                            Logging.Log(Logging.LogType.Information, "Library Scan", " Orphaned file found in library: " + LibraryFile);

                            // get discovered platform
                            IGDB.Models.Platform determinedPlatform = Metadata.Platforms.GetPlatform(sig.Flags.IGDBPlatformId);

                            IGDB.Models.Game determinedGame = new Game();
                            if (determinedPlatform == null)
                            {
                                if (library.DefaultPlatformId == 0)
                                {
                                    determinedPlatform = new IGDB.Models.Platform();
                                    determinedGame = SearchForGame(sig.Game.Name, sig.Flags.IGDBPlatformId);
                                }
                                else
                                {
                                    determinedPlatform = Platforms.GetPlatform(library.DefaultPlatformId);
                                    determinedGame = SearchForGame(sig.Game.Name, library.DefaultPlatformId);
                                }
                            }
                            else
                            {
                                determinedGame = SearchForGame(sig.Game.Name, (long)determinedPlatform.Id);
                            }

                            StoreROM(library, hash, determinedGame, determinedPlatform, sig, LibraryFile);
                        }
                    }
                }

                sql = "SELECT * FROM Games_Roms WHERE LibraryId=@libraryid ORDER BY `name`";
                dtRoms = db.ExecuteCMD(sql, dbDict);

                // check all roms to see if their local file still exists
                Logging.Log(Logging.LogType.Information, "Library Scan", "Checking library files exist on disk");
                if (dtRoms.Rows.Count > 0)
                {
                    for (var i = 0; i < dtRoms.Rows.Count; i++)
                    {
                        long romId = (long)dtRoms.Rows[i]["Id"];
                        string romPath = (string)dtRoms.Rows[i]["Path"];
                        gaseous_signature_parser.models.RomSignatureObject.RomSignatureObject.Game.Rom.SignatureSourceType romMetadataSource = (gaseous_signature_parser.models.RomSignatureObject.RomSignatureObject.Game.Rom.SignatureSourceType)(int)dtRoms.Rows[i]["MetadataSource"];

                        Logging.Log(Logging.LogType.Information, "Library Scan", " Processing ROM at path " + romPath);

                        if (File.Exists(romPath))
                        {
                            if (library.IsDefaultLibrary == true)
                            {
                                if (romPath != ComputeROMPath(romId))
                                {
                                    MoveGameFile(romId);
                                }
                            }
                        }
                        else
                        {
                            // file doesn't exist where it's supposed to be! delete it from the db
                            Logging.Log(Logging.LogType.Warning, "Library Scan", " Deleting orphaned database entry for " + romPath);

                            string deleteSql = "DELETE FROM Games_Roms WHERE Id = @id AND LibraryId = @libraryid";
                            Dictionary<string, object> deleteDict = new Dictionary<string, object>();
                            deleteDict.Add("id", romId);
                            deleteDict.Add("libraryid", library.Id);
                            db.ExecuteCMD(deleteSql, deleteDict);
                        }
                    }
                }

                Logging.Log(Logging.LogType.Information, "Library Scan", "Library scan completed");
            }
        }

        public static void Rematcher(bool ForceExecute = false)
        {
            // rescan all titles with an unknown platform or title and see if we can get a match
            Logging.Log(Logging.LogType.Information, "Rematch Scan", "Rematch scan starting");

            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "";
            if (ForceExecute == false)
            {
                sql = "SELECT * FROM Games_Roms WHERE ((PlatformId = 0 OR GameId = 0) AND MetadataSource = 0) AND (LastMatchAttemptDate IS NULL OR LastMatchAttemptDate < @lastmatchattemptdate) LIMIT 100;";
            }
            else
            {
                sql = "SELECT * FROM Games_Roms WHERE ((PlatformId = 0 OR GameId = 0) AND MetadataSource = 0);";
            }
            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            dbDict.Add("lastmatchattemptdate", DateTime.UtcNow.AddDays(-7));
            DataTable data = db.ExecuteCMD(sql, dbDict);
            foreach (DataRow row in data.Rows)
            {
                // get library
                GameLibrary.LibraryItem library = GameLibrary.GetLibrary((int)row["LibraryId"]);

                // get rom info
                long romId = (long)row["Id"];
                string romPath = (string)row["Path"];
                Common.hashObject hash = new Common.hashObject
                {
                    md5hash = (string)row["MD5"],
                    sha1hash = (string)row["SHA1"]
                };
                FileInfo fi = new FileInfo(romPath);

                Logging.Log(Logging.LogType.Information, "Rematch Scan", "Running rematch against " + romPath);

                // determine rom signature
                Models.Signatures_Games sig = GetFileSignature(hash, fi, romPath);

                // determine rom platform
                IGDB.Models.Platform determinedPlatform = Metadata.Platforms.GetPlatform(sig.Flags.IGDBPlatformId);
                if (determinedPlatform == null)
                {
                    determinedPlatform = new IGDB.Models.Platform();
                }

                IGDB.Models.Game determinedGame = SearchForGame(sig.Game.Name, sig.Flags.IGDBPlatformId);

                StoreROM(library, hash, determinedGame, determinedPlatform, sig, romPath, romId);

                string attemptSql = "UPDATE Games_Roms SET LastMatchAttemptDate=@lastmatchattemptdate WHERE Id=@id;";
                Dictionary<string, object> dbLastAttemptDict = new Dictionary<string, object>();
                dbLastAttemptDict.Add("id", romId);
                dbLastAttemptDict.Add("lastmatchattemptdate", DateTime.UtcNow);
                db.ExecuteCMD(attemptSql, dbLastAttemptDict);
            }

            Logging.Log(Logging.LogType.Information, "Rematch Scan", "Rematch scan completed");
        }
    }
}

