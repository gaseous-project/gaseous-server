﻿using System;
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

		public static GameRomObject GetRoms(long GameId, long PlatformId = -1, int pageNumber = 0, int pageSize = 0)
		{
			GameRomObject GameRoms = new GameRomObject();

            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "";
			Dictionary<string, object> dbDict = new Dictionary<string, object>();
            dbDict.Add("id", GameId);
            
			if (PlatformId == -1) {
				sql = "SELECT Games_Roms.*, Platform.`Name` AS platformname FROM Games_Roms LEFT JOIN Platform ON Games_Roms.PlatformId = Platform.Id WHERE Games_Roms.GameId = @id ORDER BY Platform.`Name`, Games_Roms.`Name`";
			} else {
				sql = "SELECT Games_Roms.*, Platform.`Name` AS platformname FROM Games_Roms LEFT JOIN Platform ON Games_Roms.PlatformId = Platform.Id WHERE Games_Roms.GameId = @id AND Games_Roms.PlatformId = @platformid ORDER BY Platform.`Name`, Games_Roms.`Name`";
				dbDict.Add("platformid", PlatformId);
			}
            DataTable romDT = db.ExecuteCMD(sql, dbDict);

            if (romDT.Rows.Count > 0)
            {
				// set count of roms
				GameRoms.Count = romDT.Rows.Count;
				
				// setup platforms list
				Dictionary<long, string> platformDict = new Dictionary<long, string>();

				int pageOffset = pageSize * (pageNumber - 1);
				for (int i = 0; i < romDT.Rows.Count; i++)
				{
					GameRomItem gameRomItem = BuildRom(romDT.Rows[i]);

					if ((i >= pageOffset && i < pageOffset + pageSize) || pageSize == 0)
					{
						GameRoms.GameRomItems.Add(gameRomItem);
					}

					if (!platformDict.ContainsKey(gameRomItem.PlatformId))
					{
						platformDict.Add(gameRomItem.PlatformId, gameRomItem.Platform);
					}
				}

				// get rom media groups
				GameRoms.MediaGroups = Classes.RomMediaGroup.GetMediaGroupsFromGameId(GameId);

				// sort the platforms
				GameRoms.Platforms = platformDict.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value).ToList<KeyValuePair<long, string>>();

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
                CRC = ((string)romDR["crc"]).ToLower(),
                MD5 = ((string)romDR["md5"]).ToLower(),
                SHA1 = ((string)romDR["sha1"]).ToLower(),
                DevelopmentStatus = (string)romDR["developmentstatus"],
                Attributes = Newtonsoft.Json.JsonConvert.DeserializeObject<List<KeyValuePair<string, object>>>((string)Common.ReturnValueIfNull(romDR["attributes"], "[ ]")),
                RomType = (int)romDR["romtype"],
                RomTypeMedia = (string)romDR["romtypemedia"],
                MediaLabel = (string)romDR["medialabel"],
                Path = (string)romDR["path"],
				Source = (gaseous_signature_parser.models.RomSignatureObject.RomSignatureObject.Game.Rom.SignatureSourceType)(Int32)romDR["metadatasource"],
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
			public List<GameRomMediaGroupItem> MediaGroups { get; set; } = new List<GameRomMediaGroupItem>();
			public List<GameRomItem> GameRomItems { get; set; } = new List<GameRomItem>();
			public int Count { get; set; }
			public List<KeyValuePair<long, string>> Platforms { get; set; }
		}

		public class GameRomItem
		{
			public long Id { get; set; }
			public long PlatformId { get; set; }
			public string Platform { get; set; }
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
			public List<KeyValuePair<string, object>>? Attributes { get; set;}
			public int RomType { get; set; }
			public string? RomTypeMedia { get; set; }
			public MediaType? MediaDetail {
				get
				{
					if (RomTypeMedia != null)
					{
						return new MediaType(Source, RomTypeMedia);
					}
					else
					{
						return null;
					}
				}
			}
			public string? MediaLabel { get; set; }
			public string? Path { get; set; }
            public RomSignatureObject.Game.Rom.SignatureSourceType Source { get; set; }
			public string? SignatureSourceGameTitle { get; set;}
			public GameLibrary.LibraryItem Library { get; set; }
        }

		public class MediaType
		{
			public MediaType(RomSignatureObject.Game.Rom.SignatureSourceType Source, string MediaTypeString)
			{
				switch (Source)
				{
					case RomSignatureObject.Game.Rom.SignatureSourceType.TOSEC:
						string[] typeString = MediaTypeString.Split(" ");

						string inType = "";
						foreach (string typeStringVal in typeString)
						{
							if (inType == "")
							{
								switch (typeStringVal.ToLower())
								{
									case "disk":
										Media = RomSignatureObject.Game.Rom.RomTypes.Disk;

										inType = typeStringVal;
										break;
									case "disc":
										Media = RomSignatureObject.Game.Rom.RomTypes.Disc;

										inType = typeStringVal;
										break;
									case "file":
										Media = RomSignatureObject.Game.Rom.RomTypes.File;

										inType = typeStringVal;
										break;
									case "part":
										Media = RomSignatureObject.Game.Rom.RomTypes.Part;

										inType = typeStringVal;
										break;
									case "tape":
										Media = RomSignatureObject.Game.Rom.RomTypes.Tape;

										inType = typeStringVal;
										break;
									case "of":
										inType = typeStringVal;
										break;
									case "side":
										inType = typeStringVal;
										break;
								}
							}
							else {
								switch (inType.ToLower())
								{
									case "disk":
									case "disc":
									case "file":
									case "part":
									case "tape":
										Number = int.Parse(typeStringVal);
										break;
									case "of":
										Count = int.Parse(typeStringVal);
										break;
									case "side":
										Side = typeStringVal;
										break;
								}
								inType = "";
							}
						}

						break;

					default:
						break;

				}
			}

			public RomSignatureObject.Game.Rom.RomTypes? Media { get; set; }

			public int? Number { get; set; }

			public int? Count { get; set; }

			public string? Side { get; set; }
		}
    }
}

