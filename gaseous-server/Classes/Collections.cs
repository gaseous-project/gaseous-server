using System;
using System.Data;
using System.IO.Compression;
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

        public static CollectionItem NewCollection(CollectionItem item)
        {
            Database db = new gaseous_tools.Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "INSERT INTO RomCollections (`Name`, Description, Platforms, Genres, Players, PlayerPerspectives, Themes, MinimumRating, MaximumRating, MaximumRomsPerPlatform, MaximumBytesPerPlatform, MaximumCollectionSizeInBytes, BuiltStatus) VALUES (@name, @description, @platforms, @genres, @players, @playerperspectives, @themes, @minimumrating, @maximumrating, @maximumromsperplatform, @maximumbytesperplatform, @maximumcollectionsizeinbytes, @builtstatus); SELECT CAST(LAST_INSERT_ID() AS SIGNED);";
            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            dbDict.Add("name", item.Name);
            dbDict.Add("description", item.Description);
            dbDict.Add("platforms", Newtonsoft.Json.JsonConvert.SerializeObject(Common.ReturnValueIfNull(item.Platforms, new List<long>())));
            dbDict.Add("genres", Newtonsoft.Json.JsonConvert.SerializeObject(Common.ReturnValueIfNull(item.Genres, new List<long>())));
            dbDict.Add("players", Newtonsoft.Json.JsonConvert.SerializeObject(Common.ReturnValueIfNull(item.Players, new List<long>())));
            dbDict.Add("playerperspectives", Newtonsoft.Json.JsonConvert.SerializeObject(Common.ReturnValueIfNull(item.PlayerPerspectives, new List<long>())));
            dbDict.Add("themes", Newtonsoft.Json.JsonConvert.SerializeObject(Common.ReturnValueIfNull(item.Themes, new List<long>())));
            dbDict.Add("minimumrating", Common.ReturnValueIfNull(item.MinimumRating, -1));
            dbDict.Add("maximumrating", Common.ReturnValueIfNull(item.MaximumRating, -1));
            dbDict.Add("maximumromsperplatform", Common.ReturnValueIfNull(item.MaximumRomsPerPlatform, -1));
            dbDict.Add("maximumbytesperplatform", Common.ReturnValueIfNull(item.MaximumBytesPerPlatform, -1));
            dbDict.Add("maximumcollectionsizeinbytes", Common.ReturnValueIfNull(item.MaximumCollectionSizeInBytes, -1));
            dbDict.Add("builtstatus", CollectionItem.CollectionBuildStatus.NoStatus);
            DataTable romDT = db.ExecuteCMD(sql, dbDict);
            long CollectionId = (long)romDT.Rows[0][0];

            CollectionItem collectionItem = GetCollection(CollectionId);

            return collectionItem;
        }

        public static CollectionItem EditCollection(long Id, CollectionItem item)
        {
            Database db = new gaseous_tools.Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "UPDATE RomCollections SET `Name`=@name, Description=@description, Platforms=@platforms, Genres=@genres, Players=@players, PlayerPerspectives=@playerperspectives, Themes=@themes, MinimumRating=@minimumrating, MaximumRating=@maximumrating, MaximumRomsPerPlatform=@maximumromsperplatform, MaximumBytesPerPlatform=@maximumbytesperplatform, MaximumCollectionSizeInBytes=@maximumcollectionsizeinbytes, BuiltStatus=@builtstatus WHERE Id=@id";
            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            dbDict.Add("id", Id);
            dbDict.Add("name", item.Name);
            dbDict.Add("description", item.Description);
            dbDict.Add("platforms", Newtonsoft.Json.JsonConvert.SerializeObject(Common.ReturnValueIfNull(item.Platforms, new List<long>())));
            dbDict.Add("genres", Newtonsoft.Json.JsonConvert.SerializeObject(Common.ReturnValueIfNull(item.Genres, new List<long>())));
            dbDict.Add("players", Newtonsoft.Json.JsonConvert.SerializeObject(Common.ReturnValueIfNull(item.Players, new List<long>())));
            dbDict.Add("playerperspectives", Newtonsoft.Json.JsonConvert.SerializeObject(Common.ReturnValueIfNull(item.PlayerPerspectives, new List<long>())));
            dbDict.Add("themes", Newtonsoft.Json.JsonConvert.SerializeObject(Common.ReturnValueIfNull(item.Themes, new List<long>())));
            dbDict.Add("minimumrating", Common.ReturnValueIfNull(item.MinimumRating, -1));
            dbDict.Add("maximumrating", Common.ReturnValueIfNull(item.MaximumRating, -1));
            dbDict.Add("maximumromsperplatform", Common.ReturnValueIfNull(item.MaximumRomsPerPlatform, -1));
            dbDict.Add("maximumbytesperplatform", Common.ReturnValueIfNull(item.MaximumBytesPerPlatform, -1));
            dbDict.Add("maximumcollectionsizeinbytes", Common.ReturnValueIfNull(item.MaximumCollectionSizeInBytes, -1));
            dbDict.Add("builtstatus", CollectionItem.CollectionBuildStatus.NoStatus);
            db.ExecuteCMD(sql, dbDict);

            string CollectionZipFile = Path.Combine(Config.LibraryConfiguration.LibraryCollectionsDirectory, Id + ".zip");
            if (File.Exists(CollectionZipFile))
            {
                File.Delete(CollectionZipFile);
            }

            CollectionItem collectionItem = GetCollection(Id);

            return collectionItem;
        }

        public static void DeleteCollection(long Id)
        {
            Database db = new gaseous_tools.Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "DELETE FROM RomCollections WHERE Id=@id";
            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            dbDict.Add("id", Id);
            db.ExecuteCMD(sql, dbDict);

            string CollectionZipFile = Path.Combine(Config.LibraryConfiguration.LibraryCollectionsDirectory, Id + ".zip");
            if (File.Exists(CollectionZipFile))
            {
                File.Delete(CollectionZipFile);
            }
        }

        public static void StartCollectionItemBuild(long Id)
        {
            CollectionItem collectionItem = GetCollection(Id);

            if (collectionItem.BuildStatus != CollectionItem.CollectionBuildStatus.Building)
            {
                // set collection item to waitingforbuild
                Database db = new gaseous_tools.Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
                string sql = "UPDATE RomCollections SET BuiltStatus=@bs WHERE Id=@id";
                Dictionary<string, object> dbDict = new Dictionary<string, object>();
                dbDict.Add("id", Id);
                dbDict.Add("bs", CollectionItem.CollectionBuildStatus.WaitingForBuild);
                db.ExecuteCMD(sql, dbDict);

                // start background task
                foreach (ProcessQueue.QueueItem qi in ProcessQueue.QueueItems)
                {
                    if (qi.ItemType == ProcessQueue.QueueItemType.CollectionCompiler) { 
                        qi.ForceExecute();
                        break;
                    }
                }
            }
        }

        public static List<CollectionItem.CollectionPlatformItem> GetCollectionContent(long Id) {
            CollectionItem collectionItem = GetCollection(Id);
            List<CollectionItem.CollectionPlatformItem> collectionPlatformItems = new List<CollectionItem.CollectionPlatformItem>();

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
            List<CollectionItem.CollectionPlatformItem> platformItems = new List<CollectionItem.CollectionPlatformItem>();

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

                CollectionItem.CollectionPlatformItem collectionPlatformItem = new CollectionItem.CollectionPlatformItem(platform);
                collectionPlatformItem.Games = new List<CollectionItem.CollectionPlatformItem.CollectionGameItem>();

                foreach (Game game in games) {
                    CollectionItem.CollectionPlatformItem.CollectionGameItem collectionGameItem = new CollectionItem.CollectionPlatformItem.CollectionGameItem(game);

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
                            collectionGameItem.Roms = gameRoms;
                            collectionPlatformItem.Games.Add(collectionGameItem);
                        }
                    }
                }

                if (collectionPlatformItem.Games.Count > 0)
                {
                    bool AddPlatform = false;
                    if (collectionItem.MaximumCollectionSizeInBytes > 0)
                    {
                        if (TotalRomSize < collectionItem.MaximumCollectionSizeInBytes)
                        {
                            AddPlatform = true;
                        }
                    }
                    else
                    {
                        AddPlatform = true;
                    }

                    if (AddPlatform == true)
                    {
                        collectionPlatformItems.Add(collectionPlatformItem);
                    }
                }
            }

            return collectionPlatformItems;
        }

        public static void CompileCollections()
        {
            Database db = new gaseous_tools.Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);

            List<CollectionItem> collectionItems = GetCollections();
            foreach (CollectionItem collectionItem in collectionItems)
            {
                if (collectionItem.BuildStatus == CollectionItem.CollectionBuildStatus.WaitingForBuild)
                {
                    // set starting
                    string sql = "UPDATE RomCollections SET BuiltStatus=@bs WHERE Id=@id";
                    Dictionary<string, object> dbDict = new Dictionary<string, object>();
                    dbDict.Add("id", collectionItem.Id);
                    dbDict.Add("bs", CollectionItem.CollectionBuildStatus.Building);
                    db.ExecuteCMD(sql, dbDict);

                    List<CollectionItem.CollectionPlatformItem> collectionPlatformItems = GetCollectionContent(collectionItem.Id);
                    string ZipFilePath = Path.Combine(Config.LibraryConfiguration.LibraryCollectionsDirectory, collectionItem.Id + ".zip");
                    string ZipFileTempPath = Path.Combine(Config.LibraryConfiguration.LibraryTempDirectory, collectionItem.Id.ToString());

                    try
                    {
                        
                        // clean up if needed
                        if (File.Exists(ZipFilePath))
                        {
                            File.Delete(ZipFilePath);
                        }

                        if (Directory.Exists(ZipFileTempPath))
                        {
                            Directory.Delete(ZipFileTempPath, true);
                        }

                        // gather collection files
                        Directory.CreateDirectory(ZipFileTempPath);

                        foreach (CollectionItem.CollectionPlatformItem collectionPlatformItem in collectionPlatformItems)
                        {
                            // create platform directory
                            string ZipPlatformPath = Path.Combine(ZipFileTempPath, collectionPlatformItem.Slug);
                            if (!Directory.Exists(ZipPlatformPath))
                            {
                                Directory.CreateDirectory(ZipPlatformPath);
                            }

                            foreach (CollectionItem.CollectionPlatformItem.CollectionGameItem collectionGameItem in collectionPlatformItem.Games)
                            {
                                // create game directory
                                string ZipGamePath = Path.Combine(ZipPlatformPath, collectionGameItem.Slug);
                                if (!Directory.Exists(ZipGamePath))
                                {
                                    Directory.CreateDirectory(ZipGamePath);
                                }

                                // copy in roms
                                foreach (Roms.GameRomItem gameRomItem in collectionGameItem.Roms)
                                {
                                    if (File.Exists(gameRomItem.Path))
                                    {
                                        File.Copy(gameRomItem.Path, Path.Combine(ZipGamePath, gameRomItem.Name));
                                    }
                                }
                            }
                        }

                        // compress to zip
                        ZipFile.CreateFromDirectory(ZipFileTempPath, ZipFilePath, CompressionLevel.SmallestSize, false);

                        // clean up
                        if (Directory.Exists(ZipFileTempPath))
                        {
                            Directory.Delete(ZipFileTempPath, true);
                        }

                        // set completed
                        dbDict["bs"] = CollectionItem.CollectionBuildStatus.Completed;
                        db.ExecuteCMD(sql, dbDict);
                    }
                    catch (Exception ex)
                    {
                        // clean up
                        if (Directory.Exists(ZipFileTempPath))
                        {
                            Directory.Delete(ZipFileTempPath, true);
                        }

                        if (File.Exists(ZipFilePath))
                        {
                            File.Delete(ZipFilePath);
                        }

                        // set failed
                        dbDict["bs"] = CollectionItem.CollectionBuildStatus.Failed;
                        db.ExecuteCMD(sql, dbDict);

                        Logging.Log(Logging.LogType.Critical, "Collection Builder", "Collection building has failed", ex);
                    }
                }
            }
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
            item.BuildStatus = (CollectionItem.CollectionBuildStatus)(int)Common.ReturnValueIfNull(row["BuiltStatus"], 0);

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

                    List<CollectionItem.CollectionPlatformItem> collectionPlatformItems = new List<CollectionPlatformItem>();

                    if (Collection == null)
                    {
                        collectionPlatformItems = GetCollectionContent(Id);
                    }
                    else
                    {
                        collectionPlatformItems = Collection;
                    }

                    foreach (CollectionItem.CollectionPlatformItem platformItem in collectionPlatformItems)
                    {
                        CollectionSize += platformItem.RomSize;
                    }

                    return CollectionSize;
                }
            }

            [JsonIgnore]
            internal List<CollectionPlatformItem> Collection { get; set; }

            public class CollectionPlatformItem {
                public CollectionPlatformItem(IGDB.Models.Platform platform) {
                    string[] PropertyWhitelist = new string[] { "Id", "Name", "Slug" };

                    PropertyInfo[] srcProperties = typeof(IGDB.Models.Platform).GetProperties();
                    PropertyInfo[] dstProperties = typeof(CollectionPlatformItem).GetProperties();
                    foreach (PropertyInfo srcProperty in srcProperties) {
                        if (PropertyWhitelist.Contains<string>(srcProperty.Name))
                        {
                            foreach (PropertyInfo dstProperty in dstProperties)
                            {
                                if (srcProperty.Name == dstProperty.Name)
                                {
                                    dstProperty.SetValue(this, srcProperty.GetValue(platform));
                                }
                            }
                        }
                    }
                }

                public long Id { get; set; }
                public string Name { get; set; }
                public string Slug { get; set; }

                public List<CollectionGameItem> Games { get; set; }

                public int RomCount {
                    get {
                        int Counter = 0;
                        foreach (CollectionGameItem Game in Games) {
                            Counter += 1;
                        }

                        return Counter;
                    }
                }

                public long RomSize {
                    get {
                        long Size = 0;
                        foreach (CollectionGameItem Game in Games) {
                            foreach (Roms.GameRomItem Rom in Game.Roms) {
                                Size += Rom.Size;
                            }
                        }

                        return Size;
                    }
                }

                public class CollectionGameItem {
                    public CollectionGameItem(IGDB.Models.Game game) {
                        string[] PropertyWhitelist = new string[] { "Id", "Name", "Slug" };
                        PropertyInfo[] srcProperties = typeof(IGDB.Models.Game).GetProperties();
                        PropertyInfo[] dstProperties = typeof(CollectionPlatformItem.CollectionGameItem).GetProperties();
                        foreach (PropertyInfo srcProperty in srcProperties) {
                            if (PropertyWhitelist.Contains<string>(srcProperty.Name))
                            {
                                foreach (PropertyInfo dstProperty in dstProperties)
                                {
                                    if (srcProperty.Name == dstProperty.Name)
                                    {
                                        dstProperty.SetValue(this, srcProperty.GetValue(game));
                                    }
                                }
                            }
                        }
                    }

                    public long Id { get; set; }
                    public string Name { get; set; }
                    public string Slug { get; set; }

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
                WaitingForBuild = 1,
                Building = 2,
                Completed = 3,
                Failed = 4
            }
        }
    }
}