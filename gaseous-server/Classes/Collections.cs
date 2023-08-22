using System;
using System.Data;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using gaseous_server.Classes.Metadata;
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

            // get platforms
            List<Platform> platforms = new List<Platform>();
            if (collectionItem.Platforms.Count > 0) {
                foreach (long PlatformId in collectionItem.Platforms) {
                    platforms.Add(Platforms.GetPlatform(PlatformId));
                }
            } else {
                // get all platforms to pull from
                FilterController filterController = new FilterController();
                platforms.AddRange((List<Platform>)filterController.Filter()["platforms"]);
            }

            // build collection
            List<CollectionItem.PlatformItem> platformItems = new List<CollectionItem.PlatformItem>();

            foreach (Platform platform in platforms) {
                long TotalRomSize = 0;
                long TotalGameCount = 0;

                List<Game> games = GamesController.GetGames("",
                    platform.Id.ToString(),
                    string.Join(",", collectionItem.Genres),
                    string.Join(",", collectionItem.Players),
                    string.Join(",", collectionItem.PlayerPerspectives),
                    string.Join(",", collectionItem.Themes),
                    collectionItem.MinimumRating,
                    collectionItem.MaximumRating
                );

                foreach (Game game in games) {
                    List<Roms.GameRomItem> gameRoms = Roms.GetRoms((long)game.Id, (long)platform.Id);
                    
                    bool AddGame = false;

                    // calculate total rom size for the game
                    long GameRomSize = 0;
                    foreach (Roms.GameRomItem gameRom in gameRoms) {
                        GameRomSize += gameRom.Size;
                    }
                    if (collectionItem.MaximumBytesPerPlatform > 0) {
                        if ((TotalRomSize + GameRomSize) < collectionItem.MaximumBytesPerPlatform) {
                            AddGame = true;
                        }
                    }
                    else 
                    {
                        AddGame = true;
                    }

                    if (AddGame == true) {
                        TotalRomSize += GameRomSize;

                        // add platform if not present
                        bool AddPlatform = true;
                        foreach (CollectionItem.PlatformItem platformItem in platformItems) {
                            if (platformItem.Id == platform.Id) {
                                AddPlatform = false;
                                break;
                            }
                        }
                        if (AddPlatform == true) {
                            CollectionItem.PlatformItem item = new CollectionItem.PlatformItem(platform);
                            item.Games = new List<CollectionItem.PlatformItem.GameItem>();
                            platformItems.Add(item);
                        }

                        // add game to platform
                        foreach (CollectionItem.PlatformItem platformItem1 in platformItems) {
                            bool AddRoms = false;

                            if (collectionItem.MaximumRomsPerPlatform > 0) { 
                                if (TotalGameCount < collectionItem.MaximumRomsPerPlatform) {
                                    AddRoms = true;
                                }
                            }
                            else
                            {
                                AddRoms = true;
                            }

                            if (AddRoms == true) {
                                TotalGameCount += 1;
                                CollectionItem.PlatformItem.GameItem gameItem = new CollectionItem.PlatformItem.GameItem(game);
                                gameItem.Roms = gameRoms;
                                platformItem1.Games.Add(gameItem);
                            }
                        }
                    }
                }

                if (platformItems.Count > 0) {
                    if (collectionItem.Collection == null) {
                        collectionItem.Collection = new List<CollectionItem.PlatformItem>();
                    }
                    collectionItem.Collection.AddRange(platformItems);
                }
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
            public int MinimumRating { get; set; }
            public int MaximumRating { get; set; }
            public int? MaximumRomsPerPlatform { get; set; }
            public long? MaximumBytesPerPlatform { get; set; }
            public long? MaximumCollectionSizeInBytes { get; set; }

            [JsonIgnore]
            public CollectionBuildStatus BuildStatus { get; set; }

            [JsonIgnore]
            public long CollectionBuiltSizeBytes { get; set; }

            [JsonIgnore]
            public long CollectionProjectedSizeBytes { 
                get
                {
                    long CollectionSize = 0;
                    foreach (CollectionItem.PlatformItem platformItem in Collection) {
                        CollectionSize += platformItem.RomSize;
                    }

                    return CollectionSize;
                }
            }

            public CollectionSortField CollectionSortedField = CollectionSortField.Name;

            public List<PlatformItem> Collection { get; set; }

            public class PlatformItem : IGDB.Models.Platform {
                public PlatformItem(IGDB.Models.Platform platform) {
                    PropertyInfo[] srcProperties = typeof(IGDB.Models.Platform).GetProperties();
                    PropertyInfo[] dstProperties = typeof(PlatformItem).GetProperties();
                    foreach (PropertyInfo srcProperty in srcProperties) {
                        foreach (PropertyInfo dstProperty in dstProperties) {
                            if (srcProperty.Name == dstProperty.Name) {
                                dstProperty.SetValue(this, srcProperty.GetValue(platform));
                            }
                        }
                    }
                }

                public List<GameItem> Games { get; set; }

                public int RomCount {
                    get {
                        int Counter = 0;
                        foreach (GameItem Game in Games) {
                            Counter += 1;
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
                    public GameItem(IGDB.Models.Game game) {
                    PropertyInfo[] srcProperties = typeof(IGDB.Models.Game).GetProperties();
                    PropertyInfo[] dstProperties = typeof(PlatformItem.GameItem).GetProperties();
                    foreach (PropertyInfo srcProperty in srcProperties) {
                        foreach (PropertyInfo dstProperty in dstProperties) {
                            if (srcProperty.Name == dstProperty.Name) {
                                dstProperty.SetValue(this, srcProperty.GetValue(game));
                            }
                        }
                    }
                }

                    public List<Roms.GameRomItem> Roms { get; set; }

                    public long RomSize {
                    get {
                        long Size = 0;
                        foreach (Roms.GameRomItem Rom in Roms) {
                            Size += Rom.Size;
                        }
                    
                        return Size;
                    }
                }
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