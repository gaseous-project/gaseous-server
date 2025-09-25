using System;
using System.Data;
using gaseous_signature_parser.models.RomSignatureObject;
using static gaseous_server.Classes.RomMediaGroup;
using gaseous_server.Classes.Metadata;
using static HasheousClient.Models.FixMatchModel;
using NuGet.Protocol.Core.Types;
using static gaseous_server.Classes.FileSignature;
using System.Threading.Tasks;

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

		public static async Task<GameRomObject> GetRomsAsync(long GameId, long PlatformId = -1, string NameSearch = "", int pageNumber = 0, int pageSize = 0, string userid = "")
		{
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

			string UserFields = "";
			string UserJoin = "";
			if (userid.Length > 0)
			{
				UserFields = ", User_RecentPlayedRoms.RomId AS MostRecentRomId, User_RecentPlayedRoms.IsMediaGroup AS MostRecentRomIsMediaGroup, User_GameFavouriteRoms.RomId AS FavouriteRomId, User_GameFavouriteRoms.IsMediaGroup AS FavouriteRomIsMediaGroup";
				UserJoin = @"
					LEFT JOIN
				User_RecentPlayedRoms ON User_RecentPlayedRoms.UserId = @userid
					AND User_RecentPlayedRoms.GameId = Games_Roms.MetadataMapId
					AND User_RecentPlayedRoms.PlatformId = Games_Roms.PlatformId
					AND User_RecentPlayedRoms.RomId = Games_Roms.Id
					AND User_RecentPlayedRoms.IsMediaGroup = 0
					LEFT JOIN
				User_GameFavouriteRoms ON User_GameFavouriteRoms.UserId = @userid
					AND User_GameFavouriteRoms.GameId = Games_Roms.MetadataMapId
					AND User_GameFavouriteRoms.PlatformId = Games_Roms.PlatformId
					AND User_GameFavouriteRoms.RomId = Games_Roms.Id
					AND User_GameFavouriteRoms.IsMediaGroup = 0
				";
			}

			// platform query
			sqlPlatform = "SELECT DISTINCT Games_Roms.PlatformId, Platform.`Name` FROM Games_Roms LEFT JOIN `Metadata_Platform` AS `Platform` ON Games_Roms.PlatformId = Platform.Id WHERE GameId = @id ORDER BY Platform.`Name`;";

			if (PlatformId == -1)
			{
				// data query
				sql = "SELECT DISTINCT view_Games_Roms.*, Platform.`Name` AS platformname, Game.`Name` AS gamename, GameState.RomId AS SavedStateRomId" + UserFields + " FROM view_Games_Roms LEFT JOIN `Metadata_Platform` AS `Platform` ON view_Games_Roms.PlatformId = Platform.Id LEFT JOIN Game ON view_Games_Roms.GameId = Game.Id LEFT JOIN GameState ON (view_Games_Roms.Id = GameState.RomId AND GameState.UserId = @userid AND GameState.IsMediaGroup = 0) " + UserJoin + " WHERE view_Games_Roms.MetadataMapId = @id" + NameSearchWhere + " ORDER BY Platform.`Name`, view_Games_Roms.`Name`;";

				// count query
				sqlCount = "SELECT COUNT(view_Games_Roms.Id) AS RomCount FROM view_Games_Roms WHERE view_Games_Roms.MetadataMapId = @id" + NameSearchWhere + ";";
			}
			else
			{
				// data query
				sql = @"
				SELECT DISTINCT
					Games_Roms.*,
					Platform.`Name` AS platformname,
					view_GamesWithRoms.`Name` AS gamename,
					GameState.RomId AS SavedStateRomId,
					CONCAT(`GameLibraries`.`Path`,
						'/',
						`Games_Roms`.`RelativePath`) AS `Path`,
					`GameLibraries`.`Name` AS `LibraryName`
					" + UserFields + @"
				FROM
					Games_Roms
				JOIN
					GameLibraries ON Games_Roms.LibraryId = GameLibraries.Id
				LEFT JOIN
					`Metadata_Platform` AS `Platform` ON Games_Roms.PlatformId = Platform.Id AND Platform.SourceId = @platformsource
				LEFT JOIN
					view_GamesWithRoms ON view_GamesWithRoms.MetadataMapId = Games_Roms.MetadataMapId
				LEFT JOIN
					GameState ON (Games_Roms.Id = GameState.RomId AND GameState.UserId = @userid AND GameState.IsMediaGroup = 0) " + UserJoin + @"
				WHERE
					Games_Roms.MetadataMapId = @id AND Games_Roms.PlatformId = @platformid" + NameSearchWhere + @"
				ORDER BY
					Platform.`Name`, Games_Roms.`Name`;
				";

				// count query
				sqlCount = "SELECT COUNT(Games_Roms.Id) AS RomCount FROM Games_Roms WHERE Games_Roms.MetadataMapId = @id AND Games_Roms.PlatformId = @platformid" + NameSearchWhere + ";";

				dbDict.Add("platformid", PlatformId);
				dbDict.Add("platformsource", (int)FileSignature.MetadataSources.None);
			}
			DataTable romDT = await db.ExecuteCMDAsync(sql, dbDict, new DatabaseMemoryCacheOptions(true, (int)TimeSpan.FromMinutes(1).Ticks));

			if (romDT.Rows.Count > 0)
			{
				// set count of roms
				var rowCountList = await db.ExecuteCMDDictAsync(sqlCount, dbDict, new DatabaseMemoryCacheOptions(true, (int)TimeSpan.FromMinutes(1).Ticks));
				Dictionary<string, object> rowCount = rowCountList[0];
				GameRoms.Count = int.Parse((string)rowCount["RomCount"]);

				int pageOffset = pageSize * (pageNumber - 1);
				for (int i = 0; i < romDT.Rows.Count; i++)
				{
					if ((i >= pageOffset && i < pageOffset + pageSize) || pageSize == 0)
					{
						GameRomItem gameRomItem = await BuildRomAsync(romDT.Rows[i]);
						GameRoms.GameRomItems.Add(gameRomItem);
					}
				}

				return GameRoms;
			}
			else
			{
				throw new Games.InvalidGameId(GameId);
			}
		}

		public static async Task<GameRomItem> GetRom(long RomId)
		{
			Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
			string sql = "SELECT DISTINCT view_Games_Roms.*, Platform.`Name` AS platformname, view_GamesWithRoms.`Name` AS gamename FROM view_Games_Roms LEFT JOIN `Metadata_Platform` AS `Platform` ON view_Games_Roms.PlatformId = Platform.Id LEFT JOIN view_GamesWithRoms ON view_Games_Roms.MetadataMapId = view_GamesWithRoms.MetadataMapId WHERE view_Games_Roms.Id = @id";
			Dictionary<string, object> dbDict = new Dictionary<string, object>();
			dbDict.Add("id", RomId);
			DataTable romDT = await db.ExecuteCMDAsync(sql, dbDict);

			if (romDT.Rows.Count > 0)
			{
				DataRow romDR = romDT.Rows[0];
				GameRomItem romItem = await BuildRomAsync(romDR);
				return romItem;
			}
			else
			{
				throw new InvalidRomId(RomId);
			}
		}

		public static async Task<GameRomItem> GetRom(string MD5)
		{
			Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
			string sql = "SELECT DISTINCT view_Games_Roms.*, Platform.`Name` AS platformname, view_GamesWithRoms.`Name` AS gamename FROM view_Games_Roms LEFT JOIN `Metadata_Platform` AS `Platform` ON view_Games_Roms.PlatformId = Platform.Id LEFT JOIN view_GamesWithRoms ON view_Games_Roms.MetadataMapId = view_GamesWithRoms.MetadataMapId WHERE view_Games_Roms.MD5 = @id";
			Dictionary<string, object> dbDict = new Dictionary<string, object>();
			dbDict.Add("id", MD5);
			DataTable romDT = await db.ExecuteCMDAsync(sql, dbDict);

			if (romDT.Rows.Count > 0)
			{
				DataRow romDR = romDT.Rows[0];
				GameRomItem romItem = await BuildRomAsync(romDR);
				return romItem;
			}
			else
			{
				throw new InvalidRomHash(MD5);
			}
		}

		public static async Task<GameRomItem> UpdateRomAsync(long RomId, long PlatformId, long GameId)
		{
			// ensure metadata for platformid is present
			HasheousClient.Models.Metadata.IGDB.Platform platform = await Classes.Metadata.Platforms.GetPlatform(PlatformId);

			// ensure metadata for gameid is present
			Models.Game game = await Classes.Metadata.Games.GetGame(FileSignature.MetadataSources.IGDB, GameId);

			Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
			string sql = "UPDATE Games_Roms SET PlatformId=@platformid, MetadataMapId=@gameid WHERE Id = @id";
			Dictionary<string, object> dbDict = new Dictionary<string, object>();
			dbDict.Add("id", RomId);
			dbDict.Add("platformid", PlatformId);
			dbDict.Add("gameid", GameId);
			await db.ExecuteCMDAsync(sql, dbDict);

			GameRomItem rom = await GetRom(RomId);

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

							HasheousClient.WebApp.HttpHelper.APIKey = Config.MetadataConfiguration.HasheousAPIKey;
							HasheousClient.WebApp.HttpHelper.ClientKey = Config.MetadataConfiguration.HasheousClientAPIKey;
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

		public static async void DeleteRom(long RomId)
		{
			GameRomItem rom = await GetRom(RomId);
			if (rom.Library.IsDefaultLibrary == true)
			{
				if (File.Exists(rom.Path))
				{
					File.Delete(rom.Path);
				}

				Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
				string sql = "DELETE FROM Games_Roms WHERE Id = @id; DELETE FROM GameState WHERE RomId = @id; DELETE FROM User_GameFavouriteRoms WHERE RomId = @id AND IsMediaGroup = 0; DELETE FROM User_RecentPlayedRoms WHERE RomId = @id AND IsMediaGroup = 0; UPDATE UserTimeTracking SET PlatformId = NULL, IsMediaGroup = NULL, RomId = NULL WHERE RomId = @id AND IsMediaGroup = 0;";
				Dictionary<string, object> dbDict = new Dictionary<string, object>();
				dbDict.Add("id", RomId);
				await db.ExecuteCMDAsync(sql, dbDict);
			}
		}

		private static async Task<GameRomItem> BuildRomAsync(DataRow romDR)
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
				MetadataMapId = (long)Common.ReturnValueIfNull(romDR["metadatamapid"], 0),
				MetadataSource = (FileSignature.MetadataSources)(int)romDR["metadatasource"],
				GameId = (long)romDR["gameid"],
				Game = (string)Common.ReturnValueIfNull(romDR["gamename"], ""),
				Name = (string)romDR["name"],
				Size = (long)romDR["size"],
				Crc = ((string)romDR["crc"]).ToLower(),
				Md5 = ((string)romDR["md5"]).ToLower(),
				Sha1 = ((string)romDR["sha1"]).ToLower(),
				Sha256 = ((string)romDR["sha256"]).ToLower(),
				DevelopmentStatus = (string)romDR["developmentstatus"],
				Attributes = romAttributes,
				RomType = (HasheousClient.Models.SignatureModel.RomItem.RomTypes)(int)romDR["romtype"],
				RomTypeMedia = (string)romDR["romtypemedia"],
				MediaLabel = (string)romDR["medialabel"],
				Path = (string)romDR["path"],
				RelativePath = (string)romDR["relativepath"],
				SignatureSource = (gaseous_server.Models.Signatures_Games.RomItem.SignatureSourceType)(Int32)romDR["metadatasource"],
				SignatureSourceGameTitle = (string)Common.ReturnValueIfNull(romDR["MetadataGameName"], ""),
				HasSaveStates = hasSaveStates,
				Library = await GameLibrary.GetLibrary((int)romDR["LibraryId"])
			};

			romItem.RomUserLastUsed = false;
			if (romDR.Table.Columns.Contains("MostRecentRomId"))
			{
				if (romDR["MostRecentRomId"] != DBNull.Value)
				{
					romItem.RomUserLastUsed = true;
				}
			}

			romItem.RomUserFavourite = false;
			if (romDR.Table.Columns.Contains("FavouriteRomId"))
			{
				if (romDR["FavouriteRomId"] != DBNull.Value)
				{
					romItem.RomUserFavourite = true;
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
			public long MetadataMapId { get; set; }
			public FileSignature.MetadataSources MetadataSource { get; set; }
			public long GameId { get; set; }
			public string Game { get; set; }
			public string? Path { get; set; }
			public string? RelativePath { get; set; }
			public string? SignatureSourceGameTitle { get; set; }
			public bool HasSaveStates { get; set; } = false;
			public GameLibrary.LibraryItem Library { get; set; }
			public bool RomUserLastUsed { get; set; }
			public bool RomUserFavourite { get; set; }
		}
	}
}

