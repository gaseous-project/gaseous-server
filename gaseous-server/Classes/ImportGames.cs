using System;
using System.Data;
using System.Security.Policy;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using gaseous_tools;
using MySqlX.XDevAPI;
using Org.BouncyCastle.Utilities.IO.Pem;
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
					ImportGame.ImportGameFile(importContent);
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
        public static void ImportGameFile(string GameFileImportPath, bool IsDirectory = false, bool ForceImport = false)
		{
            Database db = new gaseous_tools.Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
			string sql = "";
			Dictionary<string, object> dbDict = new Dictionary<string, object>();

			string[] SkippableFiles = {
				".DS_STORE",
				"desktop.ini"
			};
            if (SkippableFiles.Contains<string>(Path.GetFileName(GameFileImportPath), StringComparer.OrdinalIgnoreCase))
			{ 
                Logging.Log(Logging.LogType.Debug, "Import Game", "Skipping item " + GameFileImportPath);
            }
			else
			{
				//Logging.Log(Logging.LogType.Information, "Import Game", "Processing item " + GameFileImportPath);
				if (IsDirectory == false)
				{
					FileInfo fi = new FileInfo(GameFileImportPath);
                    Common.hashObject hash = new Common.hashObject(GameFileImportPath);

					// check to make sure we don't already have this file imported
					sql = "SELECT COUNT(Id) AS count FROM Games_Roms WHERE MD5=@md5 AND SHA1=@sha1";
					dbDict.Add("md5", hash.md5hash);
					dbDict.Add("sha1", hash.sha1hash);
					DataTable importDB = db.ExecuteCMD(sql, dbDict);
					if ((Int64)importDB.Rows[0]["count"] > 0)
					{
						if (!GameFileImportPath.StartsWith(Config.LibraryConfiguration.LibraryImportDirectory))
						{
							Logging.Log(Logging.LogType.Warning, "Import Game", "  " + GameFileImportPath + " already in database - skipping");
						}
					}
					else
					{
						Logging.Log(Logging.LogType.Information, "Import Game", "  " + GameFileImportPath + " not in database - processing");

						// process as a single file
						Models.Signatures_Games discoveredSignature = GetFileSignature(hash, fi, GameFileImportPath);

						// get discovered platform
						IGDB.Models.Platform determinedPlatform = Metadata.Platforms.GetPlatform(discoveredSignature.Flags.IGDBPlatformId);
						if (determinedPlatform == null)
						{
							determinedPlatform = new IGDB.Models.Platform();
						}

                        IGDB.Models.Game determinedGame = SearchForGame(discoveredSignature.Game.Name, discoveredSignature.Flags.IGDBPlatformId);

                        // add to database
                        StoreROM(hash, determinedGame, determinedPlatform, discoveredSignature, GameFileImportPath);
                    }
                }
			}
		}

		public static Models.Signatures_Games GetFileSignature(Common.hashObject hash, FileInfo fi, string GameFileImportPath)
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
                    ri.SignatureSource = Models.Signatures_Games.RomItem.SignatureSourceType.None;
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
                        determinedGame = Metadata.Games.GetGame((long)games[0].Id, false, false);
                        Logging.Log(Logging.LogType.Information, "Import Game", "  IGDB game: " + determinedGame.Name);
                        GameFound = true;
                        break;
                    }
                    else if (games.Length > 0)
                    {
                        Logging.Log(Logging.LogType.Information, "Import Game", "  " + games.Length + " search results found");
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

            List<string> SearchCandidates = new List<string>();
            SearchCandidates.Add(GameName);
            if (GameName.Contains(" - "))
            {
                SearchCandidates.Add(GameName.Replace(" - ", ": "));
                SearchCandidates.Add(GameName.Substring(0, GameName.IndexOf(" - ")).Trim());
            }
            if (GameName.Contains(": "))
            {
                SearchCandidates.Add(GameName.Substring(0, GameName.IndexOf(": ")).Trim());
            }

            Logging.Log(Logging.LogType.Information, "Import Game", "  Search candidates: " + String.Join(", ", SearchCandidates));

            return SearchCandidates;
        }

        public static long StoreROM(Common.hashObject hash, IGDB.Models.Game determinedGame, IGDB.Models.Platform determinedPlatform, Models.Signatures_Games discoveredSignature, string GameFileImportPath, long UpdateId = 0)
        {
            Database db = new gaseous_tools.Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);

            string sql = "";

            Dictionary<string, object> dbDict = new Dictionary<string, object>();

            if (UpdateId == 0)
            {
                sql = "INSERT INTO Games_Roms (PlatformId, GameId, Name, Size, CRC, MD5, SHA1, DevelopmentStatus, Flags, RomType, RomTypeMedia, MediaLabel, Path, MetadataSource) VALUES (@platformid, @gameid, @name, @size, @crc, @md5, @sha1, @developmentstatus, @flags, @romtype, @romtypemedia, @medialabel, @path, @metadatasource); SELECT CAST(LAST_INSERT_ID() AS SIGNED);";
            } else
            {
                sql = "UPDATE Games_Roms SET PlatformId=platformid, GameId=@gameid, Name=@name, Size=@size, DevelopmentStatus=@developmentstatus, Flags=@flags, RomType=@romtype, RomTypeMedia=@romtypemedia, MediaLabel=@medialabel, MetadataSource=@metadatasource WHERE Id=@id;";
                dbDict.Add("id", UpdateId);
            }
            dbDict.Add("platformid", Common.ReturnValueIfNull(determinedPlatform.Id, 0));
            dbDict.Add("gameid", Common.ReturnValueIfNull(determinedGame.Id, 0));
            dbDict.Add("name", Common.ReturnValueIfNull(discoveredSignature.Rom.Name, ""));
            dbDict.Add("size", Common.ReturnValueIfNull(discoveredSignature.Rom.Size, 0));
            dbDict.Add("md5", hash.md5hash);
            dbDict.Add("sha1", hash.sha1hash);
            dbDict.Add("crc", Common.ReturnValueIfNull(discoveredSignature.Rom.Crc, ""));
            dbDict.Add("developmentstatus", Common.ReturnValueIfNull(discoveredSignature.Rom.DevelopmentStatus, ""));
            dbDict.Add("metadatasource", discoveredSignature.Rom.SignatureSource);

            if (discoveredSignature.Rom.flags != null)
            {
                if (discoveredSignature.Rom.flags.Count > 0)
                {
                    dbDict.Add("flags", Newtonsoft.Json.JsonConvert.SerializeObject(discoveredSignature.Rom.flags));
                }
                else
                {
                    dbDict.Add("flags", "[ ]");
                }
            }
            else
            {
                dbDict.Add("flags", "[ ]");
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
            MoveGameFile(romId);

            return romId;
        }

		public static string ComputeROMPath(long RomId)
		{
			Classes.Roms.GameRomItem rom = Classes.Roms.GetRom(RomId);

			// get metadata
			IGDB.Models.Platform platform = gaseous_server.Classes.Metadata.Platforms.GetPlatform(rom.PlatformId);
			IGDB.Models.Game game = gaseous_server.Classes.Metadata.Games.GetGame(rom.GameId, false, false);

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
			string DestinationPath = Path.Combine(Config.LibraryConfiguration.LibraryDataDirectory, gameSlug, platformSlug);
			if (!Directory.Exists(DestinationPath))
			{
				Directory.CreateDirectory(DestinationPath);
			}

			string DestinationPathName = Path.Combine(DestinationPath, rom.Name);

			return DestinationPathName;
		}

		public static void MoveGameFile(long RomId)
		{
            Classes.Roms.GameRomItem rom = Classes.Roms.GetRom(RomId);
			string romPath = rom.Path;

            if (File.Exists(romPath))
            {
                string DestinationPath = ComputeROMPath(RomId);

				if (romPath == DestinationPath)
				{
					Logging.Log(Logging.LogType.Debug, "Move Game ROM", "Destination path is the same as the current path - aborting");
				}
				else
				{
					Logging.Log(Logging.LogType.Information, "Move Game ROM", "Moving " + romPath + " to " + DestinationPath);
					if (File.Exists(DestinationPath))
					{
						Logging.Log(Logging.LogType.Information, "Move Game ROM", "A file with the same name exists at the destination - aborting");
					}
					else
					{
						File.Move(romPath, DestinationPath);

						// update the db
						Database db = new gaseous_tools.Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
						string sql = "UPDATE Games_Roms SET Path=@path WHERE Id=@id";
						Dictionary<string, object> dbDict = new Dictionary<string, object>();
						dbDict.Add("id", RomId);
						dbDict.Add("path", DestinationPath);
						db.ExecuteCMD(sql, dbDict);
					
					}
				}
            }
            else
            {
                Logging.Log(Logging.LogType.Warning, "Move Game ROM", "File " + romPath + " appears to be missing!");
            }
        }

		public static void OrganiseLibrary()
		{
            Logging.Log(Logging.LogType.Information, "Organise Library", "Starting library organisation");

            // move rom files to their new location
            Database db = new gaseous_tools.Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "SELECT * FROM Games_Roms";
            DataTable romDT = db.ExecuteCMD(sql);

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
			DeleteOrphanedDirectories(Config.LibraryConfiguration.LibraryDataDirectory);

            Logging.Log(Logging.LogType.Information, "Organise Library", "Finsihed library organisation");
        }

        private static void DeleteOrphanedDirectories(string startLocation)
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
            Logging.Log(Logging.LogType.Information, "Library Scan", "Starting library scan");

            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);

            // check all roms to see if their local file still exists
            string sql = "SELECT * FROM Games_Roms ORDER BY `name`";

            DataTable dtRoms = db.ExecuteCMD(sql);

            if (dtRoms.Rows.Count > 0)
            {
                for (var i = 0; i < dtRoms.Rows.Count; i++)
                {
                    long romId = (long)dtRoms.Rows[i]["Id"];
                    string romPath = (string)dtRoms.Rows[i]["Path"];
                    Classes.Roms.GameRomItem.SourceType romMetadataSource = (Classes.Roms.GameRomItem.SourceType)(int)dtRoms.Rows[i]["MetadataSource"];

                    Logging.Log(Logging.LogType.Information, "Library Scan", " Processing ROM at path " + romPath);

                    if (File.Exists(romPath))
                    {
                        // file exists, so lets check to make sure the signature was matched, and update if a signature can be found
                        if (romMetadataSource == Roms.GameRomItem.SourceType.None)
                        {
                            Common.hashObject hash = new Common.hashObject
                            {
                                md5hash = "",
                                sha1hash = ""
                            };
                            FileInfo fi = new FileInfo(romPath);

                            Models.Signatures_Games sig = GetFileSignature(hash, fi, romPath);
                            if (sig.Rom.SignatureSource != Models.Signatures_Games.RomItem.SignatureSourceType.None)
                            {
                                Logging.Log(Logging.LogType.Information, "Library Scan", " Update signature found for " + romPath);

                                // get discovered platform
                                IGDB.Models.Platform determinedPlatform = Metadata.Platforms.GetPlatform(sig.Flags.IGDBPlatformId);
                                if (determinedPlatform == null)
                                {
                                    determinedPlatform = new IGDB.Models.Platform();
                                }

                                IGDB.Models.Game determinedGame = SearchForGame(sig.Game.Name, sig.Flags.IGDBPlatformId);

                                StoreROM(hash, determinedGame, determinedPlatform, sig, romPath, romId);
                            }
                        }
                    }
                    else
                    {
                        // file doesn't exist where it's supposed to be! delete it from the db
                        Logging.Log(Logging.LogType.Warning, "Library Scan", " Deleting orphaned database entry for " + romPath);

                        string deleteSql = "DELETE FROM Games_Roms WHERE Id = @id";
                        Dictionary<string, object> deleteDict = new Dictionary<string, object>();
                        deleteDict.Add("id", romId);
                        db.ExecuteCMD(deleteSql, deleteDict);
                    }
                }
            }

            // search for files in the library that aren't in the database
            Logging.Log(Logging.LogType.Information, "Library Scan", "Looking for orphaned library files to add");
            string[] LibraryFiles = Directory.GetFiles(Config.LibraryConfiguration.LibraryDataDirectory, "*.*", SearchOption.AllDirectories);
            foreach (string LibraryFile in LibraryFiles)
            {
                // check if file is in database
                bool romFound = false;
                for (var i = 0; i < dtRoms.Rows.Count; i++)
                {
                    long romId = (long)dtRoms.Rows[i]["Id"];
                    string romPath = (string)dtRoms.Rows[i]["Path"];

                    if (LibraryFile == romPath)
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
                    if (determinedPlatform == null)
                    {
                        determinedPlatform = new IGDB.Models.Platform();
                    }

                    IGDB.Models.Game determinedGame = SearchForGame(sig.Game.Name, sig.Flags.IGDBPlatformId);

                    StoreROM(hash, determinedGame, determinedPlatform, sig, LibraryFile);
                }
            }

            Logging.Log(Logging.LogType.Information, "Library Scan", "Looking for duplicate library files to clean up");
            string duplicateSql = "DELETE r1 FROM Games_Roms r1 INNER JOIN Games_Roms r2 WHERE r1.Id > r2.Id AND r1.MD5 = r2.MD5;";
            db.ExecuteCMD(duplicateSql);

            Logging.Log(Logging.LogType.Information, "Library Scan", "Library scan completed");
        }
    }
}

