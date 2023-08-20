using System;
using System.Data;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using gaseous_server.Controllers;
using gaseous_tools;
using IGDB.Models;
using Newtonsoft.Json;

namespace gaseous_server.Classes
{
	public class Collections
	{
		public Collections()
		{
            
		}

        public static List<CollectionItem> GetCollections() {
            Database db = new gaseous_tools.Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "SELECT * FROM RomCollections ORDER BY `Name`";

            DataTable data = db.ExecuteCMD(sql);

            List<CollectionItem> collectionItems = new List<CollectionItem>();

            foreach(DataRow row in data.Rows) {
                collectionItems.Add(BuildCollectionItem(row));
            }

            return collectionItems;
        }

        public static CollectionItem GetCollection(long Id) {
            Database db = new gaseous_tools.Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
			string sql = "SELECT * FROM RomCollections WHERE Id = @id ORDER BY `Name`";
			Dictionary<string, object> dbDict = new Dictionary<string, object>();
			dbDict.Add("id", Id);
			DataTable romDT = db.ExecuteCMD(sql, dbDict);

			if (romDT.Rows.Count > 0)
			{
				DataRow row = romDT.Rows[0];
				CollectionItem collectionItem = BuildCollectionItem(row);

				return collectionItem;
			}
			else
			{
				throw new Exception("Unknown Collection Id");
			}
        }

        public static CollectionItem GetCollectionContent(long Id) {
            CollectionItem collectionItem = GetCollection(Id);

            List<Game> games = GamesController.GetGames(
                    "",
                    String.Join(",", collectionItem.Platforms)
            );
            collectionItem.GameData = new List<CollectionItem.GameDataItem>();
            foreach (Game game in games) {
                CollectionItem.GameDataItem gameDataItem = new CollectionItem.GameDataItem();
                gameDataItem.GameData = game;
                gameDataItem.RomItems = Roms.GetRoms((long)game.Id);

                collectionItem.GameData.Add(gameDataItem);
            }

            return collectionItem;
        }

        private static CollectionItem BuildCollectionItem(DataRow row) {
            string strPlatforms = (string)Common.ReturnValueIfNull(row["Platforms"], "[ ]");
            string strGenres = (string)Common.ReturnValueIfNull(row["Genres"], "[ ]");
            string strPlayers = (string)Common.ReturnValueIfNull(row["Players"], "[ ]");
            string strPlayerPerspectives = (string)Common.ReturnValueIfNull(row["PlayerPerspectives"], "[ ]");
            string strThemes = (string)Common.ReturnValueIfNull(row["Themes"], "[ ]");

            CollectionItem item = new CollectionItem();
            item.Id = (long)row["Id"];
            item.Name = (string)row["Name"];
            item.Description = (string)row["Description"];
            item.Platforms = Newtonsoft.Json.JsonConvert.DeserializeObject<List<long>>(strPlatforms);
            item.Genres = Newtonsoft.Json.JsonConvert.DeserializeObject<List<long>>(strGenres);
            item.Players = Newtonsoft.Json.JsonConvert.DeserializeObject<List<long>>(strPlayers);
            item.PlayerPerspectives = Newtonsoft.Json.JsonConvert.DeserializeObject<List<long>>(strPlayerPerspectives);
            item.Themes = Newtonsoft.Json.JsonConvert.DeserializeObject<List<long>>(strThemes);
            item.MinimumRating = (int)Common.ReturnValueIfNull(row["MinimumRating"], -1);
            item.MaximumRating = (int)Common.ReturnValueIfNull(row["MaximumRating"], -1);
            item.MaximumRomsPerPlatform = (int)Common.ReturnValueIfNull(row["MaximumRomsPerPlatform"], (int)-1);
            item.MaximumBytesPerPlatform = (long)Common.ReturnValueIfNull(row["MaximumBytesPerPlatform"], (long)-1);
            item.MaximumCollectionSizeInBytes = (long)Common.ReturnValueIfNull(row["MaximumCollectionSizeInBytes"], (long)-1);

            return item;
        }

        public class CollectionItem {
            public CollectionItem() {

            }

            public long Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public List<long>? Platforms { get; set; }
            public List<long>? Genres { get; set; }
            public List<long>? Players { get; set; }
            public List<long>? PlayerPerspectives { get; set; }
            public List<long>? Themes { get; set; }
            public int? MinimumRating { get; set; }
            public int? MaximumRating { get; set; }
            public int? MaximumRomsPerPlatform { get; set; }
            public long? MaximumBytesPerPlatform { get; set; }
            public long? MaximumCollectionSizeInBytes { get; set; }

            [JsonIgnore]
            public CollectionBuildStatus BuildStatus { get; set; }

            [JsonIgnore]
            public long CollectionBuiltSizeBytes { get; set; }

            [JsonIgnore]
            public long CollectionProjectedSizeBytes { get; set; }

            public CollectionSortField CollectionSortedField = CollectionSortField.Name;

            public class PlatformItem : IGDB.Models.Platform {
                public List<GameItem> Games { get; set; }

                public int RomCount {
                    get {
                        int Counter = 0;
                        foreach (GameItem Game in Games) {
                            foreach (Roms.GameRomItem Rom in Game.Roms) {
                                Counter += 1;
                            }
                        }

                        return Counter;
                    }
                }

                public long RomSize {
                    get {
                        long Size = 0;
                        foreach (GameItem Game in Games) {
                            foreach (Roms.GameRomItem Rom in Game.Roms) {
                                Size += Rom.Size;
                            }
                        }

                        return Size;
                    }
                }

                public class GameItem : IGDB.Models.Game {
                    public List<Roms.GameRomItem> Roms { get; set; }
                }
            }

            public enum CollectionBuildStatus {
                NoStatus = 0,
                NotBuilt = 1,
                Building = 2,
                Completed = 3
            }

            public enum CollectionSortField {
                Name = 0,
                Size = 1
            }
        }
    }
}