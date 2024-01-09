using System;
using System.Data;
using gaseous_signature_parser.models.RomSignatureObject;
using static gaseous_server.Classes.RomMediaGroup;
using gaseous_server.Classes.Metadata;
using IGDB.Models;

namespace gaseous_server.Classes
{
	public class Roms
	{
		public class InvalidRomId : Exception
        { 
            public InvalidRomId(long Id) : base("Unable to find ROM by id " + Id)
            {}
        }

		public static GameRomObject GetRoms(long GameId, long PlatformId = -1, string NameSearch = "", int pageNumber = 0, int pageSize = 0)
		{
			GameRomObject GameRoms = new GameRomObject();

            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "";
			string sqlCount = "";
			string sqlPlatform = "";
			Dictionary<string, object> dbDict = new Dictionary<string, object>();
            dbDict.Add("id", GameId);
            
			string NameSearchWhere = "";
			if (NameSearch.Length > 0)
			{
				NameSearchWhere = " AND Games_Roms.`Name` LIKE @namesearch";
				dbDict.Add("namesearch", '%' + NameSearch + '%');
			}

			// platform query
			sqlPlatform = "SELECT DISTINCT Games_Roms.PlatformId, Platform.`Name` FROM Games_Roms LEFT JOIN Platform ON Games_Roms.PlatformId = Platform.Id WHERE GameId = @id ORDER BY Platform.`Name`;";

			if (PlatformId == -1) {
				// data query
				sql = "SELECT Games_Roms.*, Platform.`Name` AS platformname FROM Games_Roms LEFT JOIN Platform ON Games_Roms.PlatformId = Platform.Id WHERE Games_Roms.GameId = @id" + NameSearchWhere + " ORDER BY Platform.`Name`, Games_Roms.`Name` LIMIT 1000;";
				
				// count query
				sqlCount = "SELECT COUNT(Games_Roms.Id) AS RomCount FROM Games_Roms WHERE Games_Roms.GameId = @id" + NameSearchWhere + ";";
			} else {
				// data query
				sql = "SELECT Games_Roms.*, Platform.`Name` AS platformname FROM Games_Roms LEFT JOIN Platform ON Games_Roms.PlatformId = Platform.Id WHERE Games_Roms.GameId = @id AND Games_Roms.PlatformId = @platformid" + NameSearchWhere + " ORDER BY Platform.`Name`, Games_Roms.`Name` LIMIT 1000;";

				// count query
				sqlCount = "SELECT COUNT(Games_Roms.Id) AS RomCount FROM Games_Roms WHERE Games_Roms.GameId = @id AND Games_Roms.PlatformId = @platformid" + NameSearchWhere + ";";

				dbDict.Add("platformid", PlatformId);
			}
            DataTable romDT = db.ExecuteCMD(sql, dbDict);
			Dictionary<string, object> rowCount = db.ExecuteCMDDict(sqlCount, dbDict)[0];
			DataTable platformDT = db.ExecuteCMD(sqlPlatform, dbDict);

            if (romDT.Rows.Count > 0)
            {
				// set count of roms
				GameRoms.Count = int.Parse((string)rowCount["RomCount"]);

				int pageOffset = pageSize * (pageNumber - 1);
				for (int i = 0; i < romDT.Rows.Count; i++)
				{
					GameRomItem gameRomItem = BuildRom(romDT.Rows[i]);

					if ((i >= pageOffset && i < pageOffset + pageSize) || pageSize == 0)
					{
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

		public static GameRomItem GetRom(long RomId)
		{
			Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
			string sql = "SELECT Games_Roms.*, Platform.`Name` AS platformname FROM Games_Roms LEFT JOIN Platform ON Games_Roms.PlatformId = Platform.Id WHERE Games_Roms.Id = @id";
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
				string sql = "DELETE FROM Games_Roms WHERE Id = @id";
				Dictionary<string, object> dbDict = new Dictionary<string, object>();
				dbDict.Add("id", RomId);
				db.ExecuteCMD(sql, dbDict);
			}
        }

		private static GameRomItem BuildRom(DataRow romDR)
		{
			GameRomItem romItem = new GameRomItem
            {
                Id = (long)romDR["id"],
                PlatformId = (long)romDR["platformid"],
				Platform = (string)romDR["platformname"],
                GameId = (long)romDR["gameid"],
                Name = (string)romDR["name"],
                Size = (long)romDR["size"],
                Crc = ((string)romDR["crc"]).ToLower(),
                Md5 = ((string)romDR["md5"]).ToLower(),
                Sha1 = ((string)romDR["sha1"]).ToLower(),
                DevelopmentStatus = (string)romDR["developmentstatus"],
                Attributes = Newtonsoft.Json.JsonConvert.DeserializeObject<List<KeyValuePair<string, object>>>((string)Common.ReturnValueIfNull(romDR["attributes"], "[ ]")),
                RomType = (HasheousClient.Models.LookupResponseModel.RomItem.RomTypes)(int)romDR["romtype"],
                RomTypeMedia = (string)romDR["romtypemedia"],
                MediaLabel = (string)romDR["medialabel"],
                Path = (string)romDR["path"],
				SignatureSource = (gaseous_server.Models.Signatures_Games.RomItem.SignatureSourceType)(Int32)romDR["metadatasource"],
				SignatureSourceGameTitle = (string)Common.ReturnValueIfNull(romDR["MetadataGameName"], ""),
				Library = GameLibrary.GetLibrary((int)romDR["LibraryId"])
            };

			// check for a web emulator and update the romItem
			foreach (Models.PlatformMapping.PlatformMapItem platformMapping in Models.PlatformMapping.PlatformMap)
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

		public class GameRomItem : HasheousClient.Models.LookupResponseModel.RomItem
		{
			public long PlatformId { get; set; }
			public string Platform { get; set; }
            public Models.PlatformMapping.PlatformMapItem.WebEmulatorItem? Emulator { get; set; }
            public long GameId { get; set; }
			public string? Path { get; set; }
			public string? SignatureSourceGameTitle { get; set;}
			public GameLibrary.LibraryItem Library { get; set; }
        }
    }
}

