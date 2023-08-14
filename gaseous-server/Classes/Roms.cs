using System;
using System.Data;
using gaseous_tools;

namespace gaseous_server.Classes
{
	public class Roms
	{
		public static List<GameRomItem> GetRoms(long GameId)
		{
            Database db = new gaseous_tools.Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "SELECT * FROM Games_Roms WHERE GameId = @id ORDER BY `Name`";
            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            dbDict.Add("id", GameId);
            DataTable romDT = db.ExecuteCMD(sql, dbDict);

            if (romDT.Rows.Count > 0)
            {
				List<GameRomItem> romItems = new List<GameRomItem>();
				foreach (DataRow romDR in romDT.Rows)
				{
					romItems.Add(BuildRom(romDR));
				}

				return romItems;
            }
            else
            {
                throw new Exception("Unknown Game Id");
            }
        }

		public static GameRomItem GetRom(long RomId)
		{
			Database db = new gaseous_tools.Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
			string sql = "SELECT * FROM Games_Roms WHERE Id = @id";
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
				throw new Exception("Unknown ROM Id");
			}
		}

		public static GameRomItem UpdateRom(long RomId, long PlatformId, long GameId)
		{
			// ensure metadata for platformid is present
			IGDB.Models.Platform platform = Classes.Metadata.Platforms.GetPlatform(PlatformId);

			// ensure metadata for gameid is present
			IGDB.Models.Game game = Classes.Metadata.Games.GetGame(GameId, false, false, false);

            Database db = new gaseous_tools.Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
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
			if (File.Exists(rom.Path))
			{
				File.Delete(rom.Path);
			}

            Database db = new gaseous_tools.Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "DELETE FROM Games_Roms WHERE Id = @id";
			Dictionary<string, object> dbDict = new Dictionary<string, object>();
			dbDict.Add("id", RomId);
			db.ExecuteCMD(sql, dbDict);
        }

		private static GameRomItem BuildRom(DataRow romDR)
		{
            GameRomItem romItem = new GameRomItem
            {
                Id = (long)romDR["id"],
                PlatformId = (long)romDR["platformid"],
				Platform = Classes.Metadata.Platforms.GetPlatform((long)romDR["platformid"]),
                GameId = (long)romDR["gameid"],
                Name = (string)romDR["name"],
                Size = (long)romDR["size"],
                CRC = (string)romDR["crc"],
                MD5 = (string)romDR["md5"],
                SHA1 = (string)romDR["sha1"],
                DevelopmentStatus = (string)romDR["developmentstatus"],
                Flags = Newtonsoft.Json.JsonConvert.DeserializeObject<string[]>((string)romDR["flags"]),
                RomType = (int)romDR["romtype"],
                RomTypeMedia = (string)romDR["romtypemedia"],
                MediaLabel = (string)romDR["medialabel"],
                Path = (string)romDR["path"],
				Source = (GameRomItem.SourceType)(Int32)romDR["metadatasource"]
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

		public class GameRomItem
		{
			public long Id { get; set; }
			public long PlatformId { get; set; }
			public IGDB.Models.Platform Platform { get; set; }
			//public Dictionary<string, object>? Emulator { get; set; }
            public Models.PlatformMapping.PlatformMapItem.WebEmulatorItem? Emulator { get; set; }
            public long GameId { get; set; }
			public string? Name { get; set; }
			public long Size { get; set; }
			public string? CRC { get; set; }
			public string? MD5 { get; set; }
			public string? SHA1 { get; set; }
			public string? DevelopmentStatus { get; set; }
			public string[]? Flags { get; set; }
			public int RomType { get; set; }
			public string? RomTypeMedia { get; set; }
			public string? MediaLabel { get; set; }
			public string? Path { get; set; }
            public SourceType Source { get; set; }

            public enum SourceType
            {
                None = 0,
                TOSEC = 1
            }
        }
    }
}

