using System;
using System.Data;
using gaseous_signature_parser.models.RomSignatureObject;
using static gaseous_server.Classes.RomMediaGroup;
using gaseous_server.Classes.Metadata;
using IGDB.Models;
using static HasheousClient.Models.FixMatchModel;
using NuGet.Protocol.Core.Types;
using static gaseous_server.Classes.FileSignature;

namespace gaseous_server.Classes
{
	public class Roms
	{
		public class InvalidRomId : Exception
		{
			public InvalidRomId(long Id) : base("Unable to find ROM by id " + Id)
			{ }
		}

		public class InvalidRomHash : Exception
		{
			public InvalidRomHash(String Hash) : base("Unable to find ROM by hash " + Hash)
			{ }
		}

		public static GameRomObject GetRoms(long GameId, long PlatformId = -1, string NameSearch = "", int pageNumber = 0, int pageSize = 0, string userid = "")
		{
			TimeSpan timeStart = DateTime.Now.TimeOfDay;

			GameRomObject GameRoms = new GameRomObject();

			Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
			string sql = "";
			string sqlCount = "";
			string sqlPlatform = "";
			Dictionary<string, object> dbDict = new Dictionary<string, object>();
			dbDict.Add("id", GameId);
			dbDict.Add("userid", userid);

			string NameSearchWhere = "";
			if (NameSearch.Length > 0)
			{
				NameSearchWhere = " AND Games_Roms.`Name` LIKE @namesearch";
				dbDict.Add("namesearch", '%' + NameSearch + '%');
			}

			// platform query
			sqlPlatform = "SELECT DISTINCT Games_Roms.PlatformId, Platform.`Name` FROM Games_Roms LEFT JOIN Platform ON Games_Roms.PlatformId = Platform.Id WHERE GameId = @id ORDER BY Platform.`Name`;";

			if (PlatformId == -1)
			{
				// data query
				sql = "SELECT DISTINCT Games_Roms.*, Platform.`Name` AS platformname, Game.`Name` AS gamename, GameState.RomId AS SavedStateRomId FROM Games_Roms LEFT JOIN Platform ON Games_Roms.PlatformId = Platform.Id LEFT JOIN Game ON Games_Roms.GameId = Game.Id LEFT JOIN GameState ON (Games_Roms.Id = GameState.RomId AND GameState.UserId = @userid AND GameState.IsMediaGroup = 0) WHERE Games_Roms.GameId = @id" + NameSearchWhere + " ORDER BY Platform.`Name`, Games_Roms.`Name` LIMIT 1000;";

				// count query
				sqlCount = "SELECT COUNT(Games_Roms.Id) AS RomCount FROM Games_Roms WHERE Games_Roms.GameId = @id" + NameSearchWhere + ";";
			}
			else
			{
				// data query
				sql = "SELECT DISTINCT Games_Roms.*, Platform.`Name` AS platformname, Game.`Name` AS gamename, GameState.RomId AS SavedStateRomId FROM Games_Roms LEFT JOIN Platform ON Games_Roms.PlatformId = Platform.Id LEFT JOIN Game ON Games_Roms.GameId = Game.Id LEFT JOIN GameState ON (Games_Roms.Id = GameState.RomId AND GameState.UserId = @userid AND GameState.IsMediaGroup = 0) WHERE Games_Roms.GameId = @id AND Games_Roms.PlatformId = @platformid" + NameSearchWhere + " ORDER BY Platform.`Name`, Games_Roms.`Name` LIMIT 1000;";

				// count query
				sqlCount = "SELECT COUNT(Games_Roms.Id) AS RomCount FROM Games_Roms WHERE Games_Roms.GameId = @id AND Games_Roms.PlatformId = @platformid" + NameSearchWhere + ";";

				dbDict.Add("platformid", PlatformId);
			}
			DataTable romDT = db.ExecuteCMD(sql, dbDict, new Database.DatabaseMemoryCacheOptions(true, (int)TimeSpan.FromHours(1).Ticks));
			Dictionary<string, object> rowCount = db.ExecuteCMDDict(sqlCount, dbDict, new Database.DatabaseMemoryCacheOptions(true, (int)TimeSpan.FromHours(1).Ticks))[0];

			if (romDT.Rows.Count > 0)
			{
				// set count of roms
				GameRoms.Count = int.Parse((string)rowCount["RomCount"]);

				int pageOffset = pageSize * (pageNumber - 1);
				for (int i = 0; i < romDT.Rows.Count; i++)
				{
					if ((i >= pageOffset && i < pageOffset + pageSize) || pageSize == 0)
					{
						GameRomItem gameRomItem = BuildRom(romDT.Rows[i]);
						GameRoms.GameRomItems.Add(gameRomItem);
					}
				}

				TimeSpan timeEnd = DateTime.Now.TimeOfDay;
				TimeSpan timeDiff = timeEnd - timeStart;
				Console.WriteLine("GetRoms took " + timeDiff.TotalMilliseconds + "ms");

				return GameRoms;
			}
			else
			{
				throw new Games.InvalidGameId(GameId);
			}
		}

		public static GameRomItem GetRom(long RomId)
		{
			Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
			string sql = "SELECT Games_Roms.*, Platform.`Name` AS platformname, Game.`Name` AS gamename FROM Games_Roms LEFT JOIN Platform ON Games_Roms.PlatformId = Platform.Id LEFT JOIN Game ON Games_Roms.GameId = Game.Id WHERE Games_Roms.Id = @id";
			Dictionary<string, object> dbDict = new Dictionary<string, object>();
			dbDict.Add("id", RomId);
			DataTable romDT = db.ExecuteCMD(sql, dbDict);

			if (romDT.Rows.Count > 0)
			{
				DataRow romDR = romDT.Rows[0];
				GameRomItem romItem = BuildRom(romDR);
				return romItem;
			}
			else
			{
				throw new InvalidRomId(RomId);
			}
		}

		public static GameRomItem GetRom(string MD5)
		{
			Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
			string sql = "SELECT Games_Roms.*, Platform.`Name` AS platformname, Game.`Name` AS gamename FROM Games_Roms LEFT JOIN Platform ON Games_Roms.PlatformId = Platform.Id LEFT JOIN Game ON Games_Roms.GameId = Game.Id WHERE Games_Roms.MD5 = @id";
			Dictionary<string, object> dbDict = new Dictionary<string, object>();
			dbDict.Add("id", MD5);
			DataTable romDT = db.ExecuteCMD(sql, dbDict);

			if (romDT.Rows.Count > 0)
			{
				DataRow romDR = romDT.Rows[0];
				GameRomItem romItem = BuildRom(romDR);
				return romItem;
			}
			else
			{
				throw new InvalidRomHash(MD5);
			}
		}

		public static GameRomItem UpdateRom(long RomId, long PlatformId, long GameId)
		{
			// ensure metadata for platformid is present
			IGDB.Models.Platform platform = Classes.Metadata.Platforms.GetPlatform(PlatformId);

			// ensure metadata for gameid is present
			IGDB.Models.Game game = Classes.Metadata.Games.GetGame(GameId, false, false, false);

			Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
			string sql = "UPDATE Games_Roms SET PlatformId=@platformid, GameId=@gameid WHERE Id = @id";
			Dictionary<string, object> dbDict = new Dictionary<string, object>();
			dbDict.Add("id", RomId);
			dbDict.Add("platformid", PlatformId);
			dbDict.Add("gameid", GameId);
			db.ExecuteCMD(sql, dbDict);

			GameRomItem rom = GetRom(RomId);

			// send update to Hasheous if enabled
			if (PlatformId != 0 && GameId != 0)
			{
				if (Config.MetadataConfiguration.HasheousSubmitFixes == true)
				{
					if (
						Config.MetadataConfiguration.SignatureSource == HasheousClient.Models.MetadataModel.SignatureSources.Hasheous &&
						(
							Config.MetadataConfiguration.HasheousAPIKey != null &&
							Config.MetadataConfiguration.HasheousAPIKey != "")
						)
					{
						try
						{
							// find signature used for identifing the rom
							string md5String = rom.Md5;
							string sha1String = rom.Sha1;
							if (rom.Attributes.ContainsKey("ZipContents"))
							{
								bool selectorFound = false;
								List<ArchiveData> archiveDataValues = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ArchiveData>>(rom.Attributes["ZipContents"].ToString());
								foreach (ArchiveData archiveData in archiveDataValues)
								{
									if (archiveData.isSignatureSelector == true)
									{
										md5String = archiveData.MD5;
										sha1String = archiveData.SHA1;
										selectorFound = true;
										break;
									}
								}
							}

							HasheousClient.WebApp.HttpHelper.AddHeader("X-API-Key", Config.MetadataConfiguration.HasheousAPIKey);
							HasheousClient.Hasheous hasheousClient = new HasheousClient.Hasheous();
							List<MetadataMatch> metadataMatchList = new List<MetadataMatch>();
							metadataMatchList.Add(new MetadataMatch(HasheousClient.Models.MetadataSources.IGDB, platform.Slug, game.Slug));
							hasheousClient.FixMatch(new HasheousClient.Models.FixMatchModel(md5String, sha1String, metadataMatchList));
						}
						catch (Exception ex)
						{
							Logging.Log(Logging.LogType.Critical, "Fix Match", "An error occurred while sending a fixed match to Hasheous.", ex);
						}
					}
				}
			}

			return rom;
		}

		public static void DeleteRom(long RomId)
		{
			GameRomItem rom = GetRom(RomId);
			if (rom.Library.IsDefaultLibrary == true)
			{
				if (File.Exists(rom.Path))
				{
					File.Delete(rom.Path);
				}

				Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
				string sql = "DELETE FROM Games_Roms WHERE Id = @id; DELETE FROM GameState WHERE RomId = @id;";
				Dictionary<string, object> dbDict = new Dictionary<string, object>();
				dbDict.Add("id", RomId);
				db.ExecuteCMD(sql, dbDict);
			}
		}

		private static GameRomItem BuildRom(DataRow romDR)
		{
			bool hasSaveStates = false;
			if (romDR.Table.Columns.Contains("SavedStateRomId"))
			{
				if (romDR["SavedStateRomId"] != DBNull.Value)
				{
					hasSaveStates = true;
				}
			}

			Dictionary<string, object> romAttributes = new Dictionary<string, object>();
			if (romDR["attributes"] != DBNull.Value)
			{
				try
				{
					if ((string)romDR["attributes"] != "[ ]")
					{
						romAttributes = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>((string)romDR["attributes"]);
					}
				}
				catch (Exception ex)
				{
					Logging.Log(Logging.LogType.Warning, "Roms", "Error parsing rom attributes: " + ex.Message);
				}
			}

			GameRomItem romItem = new GameRomItem
			{
				Id = (long)romDR["id"],
				PlatformId = (long)romDR["platformid"],
				Platform = (string)romDR["platformname"],
				GameId = (long)romDR["gameid"],
				Game = (string)romDR["gamename"],
				Name = (string)romDR["name"],
				Size = (long)romDR["size"],
				Crc = ((string)romDR["crc"]).ToLower(),
				Md5 = ((string)romDR["md5"]).ToLower(),
				Sha1 = ((string)romDR["sha1"]).ToLower(),
				DevelopmentStatus = (string)romDR["developmentstatus"],
				Attributes = romAttributes,
				RomType = (HasheousClient.Models.SignatureModel.RomItem.RomTypes)(int)romDR["romtype"],
				RomTypeMedia = (string)romDR["romtypemedia"],
				MediaLabel = (string)romDR["medialabel"],
				Path = (string)romDR["path"],
				SignatureSource = (gaseous_server.Models.Signatures_Games.RomItem.SignatureSourceType)(Int32)romDR["metadatasource"],
				SignatureSourceGameTitle = (string)Common.ReturnValueIfNull(romDR["MetadataGameName"], ""),
				HasSaveStates = hasSaveStates,
				Library = GameLibrary.GetLibrary((int)romDR["LibraryId"])
			};

			// check for a web emulator and update the romItem
			List<Models.PlatformMapping.PlatformMapItem> pMap = Models.PlatformMapping.PlatformMap;
			foreach (Models.PlatformMapping.PlatformMapItem platformMapping in pMap)
			{
				if (platformMapping.IGDBId == romItem.PlatformId)
				{
					if (platformMapping.WebEmulator != null)
					{
						romItem.Emulator = platformMapping.WebEmulator;
					}
				}
			}

			return romItem;
		}

		public class GameRomObject
		{
			public List<GameRomItem> GameRomItems { get; set; } = new List<GameRomItem>();
			public int Count { get; set; }
		}

		public class GameRomItem : HasheousClient.Models.SignatureModel.RomItem
		{
			public long PlatformId { get; set; }
			public string Platform { get; set; }
			public Models.PlatformMapping.PlatformMapItem.WebEmulatorItem? Emulator { get; set; }
			public long GameId { get; set; }
			public string Game { get; set; }
			public string? Path { get; set; }
			public string? SignatureSourceGameTitle { get; set; }
			public bool HasSaveStates { get; set; } = false;
			public GameLibrary.LibraryItem Library { get; set; }
		}
	}
}

