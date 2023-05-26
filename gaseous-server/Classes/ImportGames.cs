using System;
using System.Data;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using gaseous_tools;
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
        private Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);

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
					sql = "SELECT COUNT(Id) AS count FROM games_roms WHERE md5=@md5 AND sha1=@sha1";
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
							}
						}

                        Logging.Log(Logging.LogType.Information, "Import Game", "  Determined import file as: " + discoveredSignature.Game.Name + " (" + discoveredSignature.Game.Year + ") " + discoveredSignature.Game.System);
						// get discovered platform
						IGDB.Models.Platform determinedPlatform = Metadata.Platforms.GetPlatform(discoveredSignature.Flags.IGDBPlatformId);
						if (determinedPlatform == null)
						{
							determinedPlatform = new IGDB.Models.Platform();
						}

						// search discovered game - case insensitive exact match first
						IGDB.Models.Game determinedGame = new IGDB.Models.Game();

						// remove version numbers from name
						discoveredSignature.Game.Name = Regex.Replace(discoveredSignature.Game.Name, @"v(\d+\.)?(\d+\.)?(\*|\d+)$", "").Trim();
                        discoveredSignature.Game.Name = Regex.Replace(discoveredSignature.Game.Name, @"Rev (\d+\.)?(\d+\.)?(\*|\d+)$", "").Trim();

                        Logging.Log(Logging.LogType.Information, "Import Game", "  Searching for title: " + discoveredSignature.Game.Name);

						foreach (Metadata.Games.SearchType searchType in Enum.GetValues(typeof(Metadata.Games.SearchType)))
						{
                            Logging.Log(Logging.LogType.Information, "Import Game", "  Search type: " + searchType.ToString());
							IGDB.Models.Game[] games = Metadata.Games.SearchForGame(discoveredSignature.Game.Name, discoveredSignature.Flags.IGDBPlatformId, searchType);
							if (games.Length == 1)
							{
								// exact match!
								determinedGame = Metadata.Games.GetGame((long)games[0].Id, false, false);
                                Logging.Log(Logging.LogType.Information, "Import Game", "  IGDB game: " + determinedGame.Name);
								break;
							} else if (games.Length > 0)
							{
								Logging.Log(Logging.LogType.Information, "Import Game", "  " + games.Length + " search results found");
                            } else
							{
                                Logging.Log(Logging.LogType.Information, "Import Game", "  No search results found");
                            }
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

						// add to database
						sql = "INSERT INTO games_roms (platformid, gameid, name, size, crc, md5, sha1, developmentstatus, flags, romtype, romtypemedia, medialabel, path) VALUES (@platformid, @gameid, @name, @size, @crc, @md5, @sha1, @developmentstatus, @flags, @romtype, @romtypemedia, @medialabel, @path); SELECT CAST(LAST_INSERT_ID() AS SIGNED);";
						dbDict.Add("platformid", Common.ReturnValueIfNull(determinedPlatform.Id, 0));
						dbDict.Add("gameid", Common.ReturnValueIfNull(determinedGame.Id, 0));
                        dbDict.Add("name", Common.ReturnValueIfNull(discoveredSignature.Rom.Name, ""));
                        dbDict.Add("size", Common.ReturnValueIfNull(discoveredSignature.Rom.Size, 0));
                        dbDict.Add("crc", Common.ReturnValueIfNull(discoveredSignature.Rom.Crc, ""));
                        dbDict.Add("developmentstatus", Common.ReturnValueIfNull(discoveredSignature.Rom.DevelopmentStatus, ""));

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
						long romId = (long)romInsert.Rows[0][0];

                        // move to destination
						MoveGameFile(romId);
                    }
                }
			}
		}

		public static string ComputeROMPath(long RomId)
		{
			Classes.Roms.RomItem rom = Classes.Roms.GetRom(RomId);

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
            Classes.Roms.RomItem rom = Classes.Roms.GetRom(RomId);
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
						string sql = "UPDATE games_roms SET path=@path WHERE id=@id";
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
            string sql = "SELECT * FROM games_roms";
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
			processDirectory(Config.LibraryConfiguration.LibraryDataDirectory);

            Logging.Log(Logging.LogType.Information, "Organise Library", "Finsihed library organisation");
        }

        private static void processDirectory(string startLocation)
        {
            foreach (var directory in Directory.GetDirectories(startLocation))
            {
                processDirectory(directory);
                if (Directory.GetFiles(directory).Length == 0 &&
                    Directory.GetDirectories(directory).Length == 0)
                {
                    Directory.Delete(directory, false);
                }
            }
        }
    }
}

