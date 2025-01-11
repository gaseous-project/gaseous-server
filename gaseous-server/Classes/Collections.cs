using System;
using System.Data;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Authentication;
using gaseous_server.Classes.Metadata;
using gaseous_server.Controllers;
using gaseous_server.Controllers.v1_1;
using gaseous_server.Models;
using HasheousClient.Models.Metadata.IGDB;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using SharpCompress.Common;
using static gaseous_server.Classes.Metadata.Games;

namespace gaseous_server.Classes
{
    public class Collections
    {
        public static List<CollectionItem> GetCollections(string userid)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "SELECT * FROM RomCollections WHERE OwnedBy=@ownedby ORDER BY `Name`";
            Dictionary<string, object> dbDict = new Dictionary<string, object>{
                { "ownedby", userid }
            };
            DataTable data = db.ExecuteCMD(sql, dbDict);

            List<CollectionItem> collectionItems = new List<CollectionItem>();

            foreach (DataRow row in data.Rows)
            {
                collectionItems.Add(BuildCollectionItem(row));
            }

            return collectionItems;
        }

        public static CollectionItem GetCollection(long Id, string userid)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql;
            if (userid == "")
            {
                // reserved for internal operations
                sql = "SELECT * FROM RomCollections WHERE Id = @id ORDER BY `Name`";
            }
            else
            {
                // instigated by a user
                sql = "SELECT * FROM RomCollections WHERE Id = @id AND OwnedBy = @ownedby ORDER BY `Name`";
            }
            Dictionary<string, object> dbDict = new Dictionary<string, object>
            {
                { "id", Id },
                { "ownedby", userid }
            };
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

        public static CollectionItem NewCollection(CollectionItem item, string userid)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "INSERT INTO RomCollections (`Name`, Description, Platforms, Genres, Players, PlayerPerspectives, Themes, MinimumRating, MaximumRating, MaximumRomsPerPlatform, MaximumBytesPerPlatform, MaximumCollectionSizeInBytes, FolderStructure, IncludeBIOSFiles, ArchiveType, AlwaysInclude, BuiltStatus, OwnedBy) VALUES (@name, @description, @platforms, @genres, @players, @playerperspectives, @themes, @minimumrating, @maximumrating, @maximumromsperplatform, @maximumbytesperplatform, @maximumcollectionsizeinbytes, @folderstructure, @includebiosfiles, @archivetype, @alwaysinclude, @builtstatus, @ownedby); SELECT CAST(LAST_INSERT_ID() AS SIGNED);";
            Dictionary<string, object> dbDict = new Dictionary<string, object>
            {
                { "name", item.Name },
                { "description", item.Description },
                { "platforms", Newtonsoft.Json.JsonConvert.SerializeObject(Common.ReturnValueIfNull(item.Platforms, new List<long>())) },
                { "genres", Newtonsoft.Json.JsonConvert.SerializeObject(Common.ReturnValueIfNull(item.Genres, new List<long>())) },
                { "players", Newtonsoft.Json.JsonConvert.SerializeObject(Common.ReturnValueIfNull(item.Players, new List<long>())) },
                { "playerperspectives", Newtonsoft.Json.JsonConvert.SerializeObject(Common.ReturnValueIfNull(item.PlayerPerspectives, new List<long>())) },
                { "themes", Newtonsoft.Json.JsonConvert.SerializeObject(Common.ReturnValueIfNull(item.Themes, new List<long>())) },
                { "minimumrating", Common.ReturnValueIfNull(item.MinimumRating, -1) },
                { "maximumrating", Common.ReturnValueIfNull(item.MaximumRating, -1) },
                { "maximumromsperplatform", Common.ReturnValueIfNull(item.MaximumRomsPerPlatform, -1) },
                { "maximumbytesperplatform", Common.ReturnValueIfNull(item.MaximumBytesPerPlatform, -1) },
                { "maximumcollectionsizeinbytes", Common.ReturnValueIfNull(item.MaximumCollectionSizeInBytes, -1) },
                { "folderstructure", Common.ReturnValueIfNull(item.FolderStructure, CollectionItem.FolderStructures.Gaseous) },
                { "includebiosfiles", Common.ReturnValueIfNull(item.IncludeBIOSFiles, 0) },
                { "archivetype", Common.ReturnValueIfNull(item.ArchiveType, CollectionItem.ArchiveTypes.Zip) },
                { "alwaysinclude", Newtonsoft.Json.JsonConvert.SerializeObject(Common.ReturnValueIfNull(item.AlwaysInclude, new List<CollectionItem.AlwaysIncludeItem>())) },
                { "builtstatus", CollectionItem.CollectionBuildStatus.WaitingForBuild },
                { "ownedby", userid }
            };
            DataTable romDT = db.ExecuteCMD(sql, dbDict);
            long CollectionId = (long)romDT.Rows[0][0];

            CollectionItem collectionItem = GetCollection(CollectionId, userid);

            StartCollectionItemBuild(CollectionId, userid);

            return collectionItem;
        }

        public static CollectionItem EditCollection(long Id, CollectionItem item, string userid, bool ForceRebuild = true)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "UPDATE RomCollections SET `Name`=@name, Description=@description, Platforms=@platforms, Genres=@genres, Players=@players, PlayerPerspectives=@playerperspectives, Themes=@themes, MinimumRating=@minimumrating, MaximumRating=@maximumrating, MaximumRomsPerPlatform=@maximumromsperplatform, MaximumBytesPerPlatform=@maximumbytesperplatform, MaximumCollectionSizeInBytes=@maximumcollectionsizeinbytes, FolderStructure=@folderstructure, IncludeBIOSFiles=@includebiosfiles, ArchiveType=@archivetype, AlwaysInclude=@alwaysinclude, BuiltStatus=@builtstatus WHERE Id=@id AND OwnedBy=@ownedby";
            Dictionary<string, object> dbDict = new Dictionary<string, object>
            {
                { "id", Id },
                { "name", item.Name },
                { "description", item.Description },
                { "platforms", Newtonsoft.Json.JsonConvert.SerializeObject(Common.ReturnValueIfNull(item.Platforms, new List<long>())) },
                { "genres", Newtonsoft.Json.JsonConvert.SerializeObject(Common.ReturnValueIfNull(item.Genres, new List<long>())) },
                { "players", Newtonsoft.Json.JsonConvert.SerializeObject(Common.ReturnValueIfNull(item.Players, new List<long>())) },
                { "playerperspectives", Newtonsoft.Json.JsonConvert.SerializeObject(Common.ReturnValueIfNull(item.PlayerPerspectives, new List<long>())) },
                { "themes", Newtonsoft.Json.JsonConvert.SerializeObject(Common.ReturnValueIfNull(item.Themes, new List<long>())) },
                { "minimumrating", Common.ReturnValueIfNull(item.MinimumRating, -1) },
                { "maximumrating", Common.ReturnValueIfNull(item.MaximumRating, -1) },
                { "maximumromsperplatform", Common.ReturnValueIfNull(item.MaximumRomsPerPlatform, -1) },
                { "maximumbytesperplatform", Common.ReturnValueIfNull(item.MaximumBytesPerPlatform, -1) },
                { "maximumcollectionsizeinbytes", Common.ReturnValueIfNull(item.MaximumCollectionSizeInBytes, -1) },
                { "folderstructure", Common.ReturnValueIfNull(item.FolderStructure, CollectionItem.FolderStructures.Gaseous) },
                { "includebiosfiles", Common.ReturnValueIfNull(item.IncludeBIOSFiles, 0) },
                { "alwaysinclude", Newtonsoft.Json.JsonConvert.SerializeObject(Common.ReturnValueIfNull(item.AlwaysInclude, new List<CollectionItem.AlwaysIncludeItem>())) },
                { "archivetype", Common.ReturnValueIfNull(item.ArchiveType, CollectionItem.ArchiveTypes.Zip) },
                { "ownedby", userid }
            };

            string CollectionZipFile = Path.Combine(Config.LibraryConfiguration.LibraryCollectionsDirectory, Id + item.ArchiveExtension);
            if (ForceRebuild == true)
            {
                dbDict.Add("builtstatus", CollectionItem.CollectionBuildStatus.WaitingForBuild);
                if (File.Exists(CollectionZipFile))
                {
                    Logging.Log(Logging.LogType.Warning, "Collections", "Deleting existing build of collection: " + item.Name);
                    File.Delete(CollectionZipFile);
                }
            }
            else
            {
                if (File.Exists(CollectionZipFile))
                {
                    dbDict.Add("builtstatus", CollectionItem.CollectionBuildStatus.Completed);
                }
                else
                {
                    dbDict.Add("builtstatus", CollectionItem.CollectionBuildStatus.NoStatus);
                }
            }
            db.ExecuteCMD(sql, dbDict);

            CollectionItem collectionItem = GetCollection(Id, userid);

            if (collectionItem.BuildStatus == CollectionItem.CollectionBuildStatus.WaitingForBuild)
            {
                StartCollectionItemBuild(Id, userid);
            }

            return collectionItem;
        }

        public static void DeleteCollection(long Id, string userid)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "DELETE FROM RomCollections WHERE Id=@id AND OwnedBy=@ownedby";
            Dictionary<string, object> dbDict = new Dictionary<string, object>
            {
                { "id", Id },
                { "ownedby", userid }
            };
            db.ExecuteCMD(sql, dbDict);

            string CollectionZipFile = Path.Combine(Config.LibraryConfiguration.LibraryCollectionsDirectory, Id + ".zip");
            if (File.Exists(CollectionZipFile))
            {
                File.Delete(CollectionZipFile);
            }
        }

        public static void StartCollectionItemBuild(long Id, string userid)
        {
            // send blank user id to getcollection as this is not a user initiated process
            CollectionItem collectionItem = GetCollection(Id, userid);

            if (collectionItem.BuildStatus != CollectionItem.CollectionBuildStatus.Building)
            {
                // set collection item to waitingforbuild
                Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
                string sql = "UPDATE RomCollections SET BuiltStatus=@bs WHERE Id=@id";
                Dictionary<string, object> dbDict = new Dictionary<string, object>();
                dbDict.Add("id", Id);
                dbDict.Add("bs", CollectionItem.CollectionBuildStatus.WaitingForBuild);
                db.ExecuteCMD(sql, dbDict);

                // start background task
                ProcessQueue.QueueItem queueItem = new ProcessQueue.QueueItem(ProcessQueue.QueueItemType.CollectionCompiler, 1, false, true);
                queueItem.Options = new Dictionary<string, object>{
                    { "Id", Id },
                    { "UserId", userid }
                };
                queueItem.ForceExecute();
                ProcessQueue.QueueItems.Add(queueItem);
            }
        }

        public static CollectionContents GetCollectionContent(CollectionItem collectionItem, string userid)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);

            // get age ratings for specified user
            List<AgeGroups.AgeRestrictionGroupings> UserAgeGroupings = new List<AgeGroups.AgeRestrictionGroupings>();
            bool UserAgeGroupIncludeUnrated = true;
            if (userid != "")
            {
                Authentication.UserTable<Authentication.ApplicationUser> userTable = new UserTable<ApplicationUser>(db);
                var user = userTable.GetUserById(userid);

                if (user.SecurityProfile.AgeRestrictionPolicy.IncludeUnrated == false)
                {
                    UserAgeGroupIncludeUnrated = false;
                }

                foreach (AgeGroups.AgeRestrictionGroupings ageGrouping in Enum.GetValues(typeof(AgeGroups.AgeRestrictionGroupings)))
                {
                    if (ageGrouping <= user.SecurityProfile.AgeRestrictionPolicy.MaximumAgeRestriction && ageGrouping != AgeGroups.AgeRestrictionGroupings.Unclassified)
                    {
                        UserAgeGroupings.Add(ageGrouping);
                    }
                }
            }

            List<CollectionContents.CollectionPlatformItem> collectionPlatformItems = new List<CollectionContents.CollectionPlatformItem>();

            // get platforms
            List<long> platformids = new List<long>();
            platformids.AddRange(collectionItem.Platforms);

            List<long>? DynamicPlatforms = new List<long>();
            DynamicPlatforms.AddRange(collectionItem.Platforms);

            List<Platform> platforms = new List<Platform>();

            // add platforms with an inclusion status
            foreach (CollectionItem.AlwaysIncludeItem alwaysIncludeItem in collectionItem.AlwaysInclude)
            {
                if (
                        alwaysIncludeItem.InclusionState == CollectionItem.AlwaysIncludeStatus.AlwaysInclude ||
                        alwaysIncludeItem.InclusionState == CollectionItem.AlwaysIncludeStatus.AlwaysExclude
                    )
                {
                    if (!platformids.Contains(alwaysIncludeItem.PlatformId))
                    {
                        platformids.Add(alwaysIncludeItem.PlatformId);
                    }
                }
            }

            // add dynamic platforms
            if (DynamicPlatforms.Count > 0)
            {
                foreach (long PlatformId in platformids)
                {
                    platforms.Add(Platforms.GetPlatform(PlatformId));
                }
            }
            else
            {
                // get all platforms to pull from
                Dictionary<string, List<Filters.FilterItem>> FilterDict = Filters.Filter(AgeGroups.AgeRestrictionGroupings.Adult, true);
                List<Classes.Filters.FilterItem> filteredPlatforms = (List<Classes.Filters.FilterItem>)FilterDict["platforms"];
                foreach (Filters.FilterItem filterItem in filteredPlatforms)
                {
                    platforms.Add(Platforms.GetPlatform(filterItem.Id));
                }
            }

            // age ratings
            AgeGroups.AgeRestrictionGroupings AgeGrouping = AgeGroups.AgeRestrictionGroupings.Unclassified;
            bool ContainsUnclassifiedAgeGroup = false;

            // build collection
            List<CollectionContents.CollectionPlatformItem> platformItems = new List<CollectionContents.CollectionPlatformItem>();

            foreach (Platform platform in platforms)
            {
                long TotalRomSize = 0;
                long TotalGameCount = 0;

                bool isDynamic = false;
                if (DynamicPlatforms.Contains((long)platform.Id))
                {
                    isDynamic = true;
                }
                else if (DynamicPlatforms.Count == 0)
                {
                    isDynamic = true;
                }

                Controllers.v1_1.GamesController.GameReturnPackage games = new Controllers.v1_1.GamesController.GameReturnPackage();
                if (isDynamic == true)
                {
                    Controllers.v1_1.GamesController.GameSearchModel searchModel = new Controllers.v1_1.GamesController.GameSearchModel
                    {
                        Name = "",
                        Platform = new List<string>{
                            platform.Id.ToString()
                        },
                        Genre = collectionItem.Genres.ConvertAll(s => s.ToString()),
                        GameMode = collectionItem.Players.ConvertAll(s => s.ToString()),
                        PlayerPerspective = collectionItem.PlayerPerspectives.ConvertAll(s => s.ToString()),
                        Theme = collectionItem.Themes.ConvertAll(s => s.ToString()),
                        GameRating = new Controllers.v1_1.GamesController.GameSearchModel.GameRatingItem
                        {
                            MinimumRating = collectionItem.MinimumRating,
                            MaximumRating = collectionItem.MaximumRating
                        },
                        GameAgeRating = new Controllers.v1_1.GamesController.GameSearchModel.GameAgeRatingItem
                        {
                            AgeGroupings = UserAgeGroupings,
                            IncludeUnrated = UserAgeGroupIncludeUnrated
                        }
                    };
                    games = Controllers.v1_1.GamesController.GetGames(searchModel, userid);

                }

                CollectionContents.CollectionPlatformItem collectionPlatformItem = new CollectionContents.CollectionPlatformItem(platform);
                collectionPlatformItem.Games = new List<CollectionContents.CollectionPlatformItem.CollectionGameItem>();

                // add titles with an inclusion status
                foreach (CollectionItem.AlwaysIncludeItem alwaysIncludeItem in collectionItem.AlwaysInclude)
                {
                    if (
                        (
                            alwaysIncludeItem.InclusionState == CollectionItem.AlwaysIncludeStatus.AlwaysInclude ||
                            alwaysIncludeItem.InclusionState == CollectionItem.AlwaysIncludeStatus.AlwaysExclude
                        ) && alwaysIncludeItem.PlatformId == platform.Id
                        )
                    {
                        MinimalGameItem AlwaysIncludeGame = new MinimalGameItem(Games.GetGame(HasheousClient.Models.MetadataSources.IGDB, alwaysIncludeItem.GameId));
                        CollectionContents.CollectionPlatformItem.CollectionGameItem gameItem = new CollectionContents.CollectionPlatformItem.CollectionGameItem(AlwaysIncludeGame);
                        gameItem.InclusionStatus = new CollectionItem.AlwaysIncludeItem();
                        gameItem.InclusionStatus.PlatformId = alwaysIncludeItem.PlatformId;
                        gameItem.InclusionStatus.GameId = alwaysIncludeItem.GameId;
                        gameItem.InclusionStatus.InclusionState = alwaysIncludeItem.InclusionState;
                        gameItem.Roms = Roms.GetRoms((long)gameItem.Id, (long)platform.Id).GameRomItems;

                        collectionPlatformItem.Games.Add(gameItem);
                    }
                }

                foreach (MinimalGameItem game in games.Games)
                {
                    bool gameAlreadyInList = false;
                    foreach (CollectionContents.CollectionPlatformItem.CollectionGameItem existingGame in collectionPlatformItem.Games)
                    {
                        if (existingGame.Id == game.Id)
                        {
                            gameAlreadyInList = true;
                        }
                    }

                    if (gameAlreadyInList == false)
                    {
                        CollectionContents.CollectionPlatformItem.CollectionGameItem collectionGameItem = new CollectionContents.CollectionPlatformItem.CollectionGameItem(game);

                        List<Roms.GameRomItem> gameRoms = Roms.GetRoms((long)game.Id, (long)platform.Id).GameRomItems;

                        bool AddGame = false;

                        // calculate total rom size for the game
                        long GameRomSize = 0;
                        foreach (Roms.GameRomItem gameRom in gameRoms)
                        {
                            GameRomSize += (long)gameRom.Size;
                        }
                        if (collectionItem.MaximumBytesPerPlatform > 0)
                        {
                            if ((TotalRomSize + GameRomSize) < collectionItem.MaximumBytesPerPlatform)
                            {
                                AddGame = true;
                            }
                        }
                        else
                        {
                            AddGame = true;
                        }

                        if (AddGame == true)
                        {
                            TotalRomSize += GameRomSize;

                            bool AddRoms = false;

                            if (collectionItem.MaximumRomsPerPlatform > 0)
                            {
                                if (TotalGameCount < collectionItem.MaximumRomsPerPlatform)
                                {
                                    AddRoms = true;
                                }
                            }
                            else
                            {
                                AddRoms = true;
                            }

                            if (AddRoms == true)
                            {
                                TotalGameCount += 1;
                                collectionGameItem.Roms = gameRoms;
                                collectionPlatformItem.Games.Add(collectionGameItem);
                            }
                        }
                    }

                    // handle age grouping
                    List<AgeRating> gameAgeRatings = game.AgeRatings.Select(s => (AgeRating)s).ToList();
                    AgeGroups.AgeRestrictionGroupings CurrentAgeGroup = AgeGroups.GetAgeGroupFromAgeRatings(gameAgeRatings);
                    if (CurrentAgeGroup > AgeGrouping)
                    {
                        AgeGrouping = CurrentAgeGroup;
                    }
                    if (CurrentAgeGroup == AgeGroups.AgeRestrictionGroupings.Unclassified)
                    {
                        ContainsUnclassifiedAgeGroup = true;
                    }
                }

                collectionPlatformItem.Games.Sort((x, y) => x.Name.CompareTo(y.Name));

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

            collectionPlatformItems.Sort((x, y) => x.Name.CompareTo(y.Name));

            CollectionContents collectionContents = new CollectionContents
            {
                Collection = collectionPlatformItems,
                AgeGroup = AgeGrouping,
                ContainsUnclassifiedAgeGroup = ContainsUnclassifiedAgeGroup
            };

            return collectionContents;
        }

        public static void CompileCollections(long CollectionId, string userid)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);

            CollectionItem collectionItem = GetCollection(CollectionId, userid);
            if (collectionItem.BuildStatus == CollectionItem.CollectionBuildStatus.WaitingForBuild)
            {
                Logging.Log(Logging.LogType.Information, "Collections", "Beginning build of collection: " + collectionItem.Name);

                CollectionContents collectionContents = GetCollectionContent(collectionItem, userid);

                // set starting
                string sql = "UPDATE RomCollections SET BuiltStatus=@bs, AgeGroup=@ag, AgeGroupUnclassified=@agu WHERE Id=@id";
                Dictionary<string, object> dbDict = new Dictionary<string, object>
                {
                    { "id", collectionItem.Id },
                    { "bs", CollectionItem.CollectionBuildStatus.Building },
                    { "ag", collectionContents.AgeGroup },
                    { "agu", collectionContents.ContainsUnclassifiedAgeGroup }
                };
                db.ExecuteCMD(sql, dbDict);

                List<CollectionContents.CollectionPlatformItem> collectionPlatformItems = collectionContents.Collection;
                string ZipFilePath = Path.Combine(Config.LibraryConfiguration.LibraryCollectionsDirectory, collectionItem.Id + collectionItem.ArchiveExtension);
                string ZipFileTempPath = Path.Combine(Config.LibraryConfiguration.LibraryTempDirectory, collectionItem.Id.ToString());

                try
                {

                    // clean up if needed
                    if (File.Exists(ZipFilePath))
                    {
                        Logging.Log(Logging.LogType.Warning, "Collections", "Deleting existing build of collection: " + collectionItem.Name);
                        File.Delete(ZipFilePath);
                    }

                    if (Directory.Exists(ZipFileTempPath))
                    {
                        Directory.Delete(ZipFileTempPath, true);
                    }

                    // gather collection files
                    Directory.CreateDirectory(ZipFileTempPath);
                    string ZipBiosPath = Path.Combine(ZipFileTempPath, "BIOS");

                    // get the games
                    foreach (CollectionContents.CollectionPlatformItem collectionPlatformItem in collectionPlatformItems)
                    {
                        // get platform bios files if present
                        if (collectionItem.IncludeBIOSFiles == true)
                        {
                            List<Bios.BiosItem> bios = Bios.GetBios(collectionPlatformItem.Id, true);
                            if (!Directory.Exists(ZipBiosPath))
                            {
                                Directory.CreateDirectory(ZipBiosPath);
                            }

                            foreach (Bios.BiosItem biosItem in bios)
                            {
                                if (File.Exists(biosItem.biosPath))
                                {
                                    Logging.Log(Logging.LogType.Information, "Collections", "Copying BIOS file: " + biosItem.filename);
                                    File.Copy(biosItem.biosPath, Path.Combine(ZipBiosPath, biosItem.filename), true);
                                }
                            }
                        }

                        // create platform directory
                        string ZipPlatformPath = "";
                        switch (collectionItem.FolderStructure)
                        {
                            case CollectionItem.FolderStructures.Gaseous:
                                ZipPlatformPath = Path.Combine(ZipFileTempPath, collectionPlatformItem.Slug);
                                break;

                            case CollectionItem.FolderStructures.RetroPie:
                                try
                                {
                                    PlatformMapping.PlatformMapItem platformMapItem = PlatformMapping.GetPlatformMap(collectionPlatformItem.Id);
                                    ZipPlatformPath = Path.Combine(ZipFileTempPath, "roms", platformMapItem.RetroPieDirectoryName);
                                }
                                catch
                                {
                                    ZipPlatformPath = Path.Combine(ZipFileTempPath, collectionPlatformItem.Slug);
                                }

                                break;

                        }
                        if (!Directory.Exists(ZipPlatformPath))
                        {
                            Directory.CreateDirectory(ZipPlatformPath);
                        }

                        foreach (CollectionContents.CollectionPlatformItem.CollectionGameItem collectionGameItem in collectionPlatformItem.Games)
                        {
                            bool includeGame = false;
                            if (collectionGameItem.InclusionStatus == null)
                            {
                                includeGame = true;
                            }
                            else
                            {
                                if (collectionGameItem.InclusionStatus.InclusionState == CollectionItem.AlwaysIncludeStatus.AlwaysInclude)
                                {
                                    includeGame = true;
                                }
                            }

                            if (includeGame == true)
                            {
                                string ZipGamePath = "";
                                switch (collectionItem.FolderStructure)
                                {
                                    case CollectionItem.FolderStructures.Gaseous:
                                        // create game directory
                                        ZipGamePath = Path.Combine(ZipPlatformPath, collectionGameItem.Slug);
                                        if (!Directory.Exists(ZipGamePath))
                                        {
                                            Directory.CreateDirectory(ZipGamePath);
                                        }
                                        break;

                                    case CollectionItem.FolderStructures.RetroPie:
                                        ZipGamePath = ZipPlatformPath;
                                        break;
                                }

                                // copy in roms
                                foreach (Roms.GameRomItem gameRomItem in collectionGameItem.Roms)
                                {
                                    if (File.Exists(gameRomItem.Path))
                                    {
                                        Logging.Log(Logging.LogType.Information, "Collections", "Copying ROM: " + gameRomItem.Name);
                                        File.Copy(gameRomItem.Path, Path.Combine(ZipGamePath, gameRomItem.Name), true);
                                    }
                                }
                            }
                        }
                    }

                    // compress to zip
                    Logging.Log(Logging.LogType.Information, "Collections", "Compressing collection");
                    switch (collectionItem.ArchiveType)
                    {
                        case CollectionItem.ArchiveTypes.Zip:
                            ZipFile.CreateFromDirectory(ZipFileTempPath, ZipFilePath, CompressionLevel.SmallestSize, false);
                            break;

                        case CollectionItem.ArchiveTypes.RAR:

                            break;

                        case CollectionItem.ArchiveTypes.SevenZip:

                            break;
                    }


                    // clean up
                    if (Directory.Exists(ZipFileTempPath))
                    {
                        Logging.Log(Logging.LogType.Information, "Collections", "Cleaning up");
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

        private static CollectionItem BuildCollectionItem(DataRow row)
        {
            string strPlatforms = (string)Common.ReturnValueIfNull(row["Platforms"], "[ ]");
            string strGenres = (string)Common.ReturnValueIfNull(row["Genres"], "[ ]");
            string strPlayers = (string)Common.ReturnValueIfNull(row["Players"], "[ ]");
            string strPlayerPerspectives = (string)Common.ReturnValueIfNull(row["PlayerPerspectives"], "[ ]");
            string strThemes = (string)Common.ReturnValueIfNull(row["Themes"], "[ ]");
            string strAlwaysInclude = (string)Common.ReturnValueIfNull(row["AlwaysInclude"], "[ ]");

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
            item.FolderStructure = (CollectionItem.FolderStructures)(int)Common.ReturnValueIfNull(row["FolderStructure"], 0);
            item.IncludeBIOSFiles = (bool)row["IncludeBIOSFiles"];
            item.ArchiveType = (CollectionItem.ArchiveTypes)(int)Common.ReturnValueIfNull(row["ArchiveType"], 0);
            item.AlwaysInclude = Newtonsoft.Json.JsonConvert.DeserializeObject<List<CollectionItem.AlwaysIncludeItem>>(strAlwaysInclude);
            item.BuildStatus = (CollectionItem.CollectionBuildStatus)(int)Common.ReturnValueIfNull(row["BuiltStatus"], 0);

            return item;
        }

        public class CollectionItem
        {
            public CollectionItem()
            {

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
            public FolderStructures FolderStructure { get; set; } = FolderStructures.Gaseous;
            public bool IncludeBIOSFiles { get; set; } = true;
            public ArchiveTypes ArchiveType { get; set; } = CollectionItem.ArchiveTypes.Zip;
            public string ArchiveExtension
            {
                get
                {
                    if (ArchiveType != null)
                    {
                        switch (ArchiveType)
                        {
                            case ArchiveTypes.Zip:
                            default:
                                return ".zip";

                            case ArchiveTypes.RAR:
                                return ".rar";

                            case ArchiveTypes.SevenZip:
                                return ".7z";
                        }
                    }
                    else
                    {
                        return ".zip";
                    }
                }
            }
            public List<AlwaysIncludeItem> AlwaysInclude { get; set; }

            [JsonIgnore]
            public CollectionBuildStatus BuildStatus
            {
                get
                {
                    if (_BuildStatus == CollectionBuildStatus.Completed)
                    {
                        if (File.Exists(Path.Combine(Config.LibraryConfiguration.LibraryCollectionsDirectory, Id + ArchiveExtension)))
                        {
                            return CollectionBuildStatus.Completed;
                        }
                        else
                        {
                            return CollectionBuildStatus.NoStatus;
                        }
                    }
                    else
                    {
                        return _BuildStatus;
                    }
                }
                set
                {
                    _BuildStatus = value;
                }
            }
            private CollectionBuildStatus _BuildStatus { get; set; }

            [JsonIgnore]
            public long CollectionBuiltSizeBytes
            {
                get
                {
                    if (BuildStatus == CollectionBuildStatus.Completed)
                    {
                        string ZipFilePath = Path.Combine(Config.LibraryConfiguration.LibraryCollectionsDirectory, Id + ArchiveExtension);
                        if (File.Exists(ZipFilePath))
                        {
                            FileInfo fi = new FileInfo(ZipFilePath);
                            return fi.Length;
                        }
                        else
                        {
                            return 0;
                        }
                    }
                    else
                    {
                        return 0;
                    }
                }
            }

            public enum CollectionBuildStatus
            {
                NoStatus = 0,
                WaitingForBuild = 1,
                Building = 2,
                Completed = 3,
                Failed = 4
            }

            public enum FolderStructures
            {
                Gaseous = 0,
                RetroPie = 1
            }

            public enum ArchiveTypes
            {
                Zip = 0,
                RAR = 1,
                SevenZip = 2
            }

            public class AlwaysIncludeItem
            {
                public long PlatformId { get; set; }
                public long GameId { get; set; }
                public AlwaysIncludeStatus InclusionState { get; set; }
            }

            public enum AlwaysIncludeStatus
            {
                None = 0,
                AlwaysInclude = 1,
                AlwaysExclude = 2
            }
        }

        public class CollectionContents
        {
            [JsonIgnore]
            public List<CollectionPlatformItem> Collection { get; set; }

            [JsonIgnore]
            public long CollectionProjectedSizeBytes
            {
                get
                {
                    long CollectionSize = 0;

                    List<CollectionPlatformItem> collectionPlatformItems = new List<CollectionPlatformItem>();

                    if (Collection != null)
                    {
                        collectionPlatformItems = Collection;
                    }

                    foreach (CollectionPlatformItem platformItem in collectionPlatformItems)
                    {
                        CollectionSize += platformItem.RomSize;
                    }

                    return CollectionSize;
                }
            }

            public AgeGroups.AgeRestrictionGroupings AgeGroup { get; set; }
            public bool ContainsUnclassifiedAgeGroup { get; set; }

            public class CollectionPlatformItem
            {
                public CollectionPlatformItem(Platform platform)
                {
                    string[] PropertyWhitelist = new string[] { "Id", "Name", "Slug" };

                    PropertyInfo[] srcProperties = typeof(Platform).GetProperties();
                    PropertyInfo[] dstProperties = typeof(CollectionPlatformItem).GetProperties();
                    foreach (PropertyInfo srcProperty in srcProperties)
                    {
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

                public int RomCount
                {
                    get
                    {
                        int Counter = 0;
                        foreach (CollectionGameItem Game in Games)
                        {
                            Counter += 1;
                        }

                        return Counter;
                    }
                }

                public long RomSize
                {
                    get
                    {
                        long Size = 0;
                        foreach (CollectionGameItem Game in Games)
                        {
                            foreach (Roms.GameRomItem Rom in Game.Roms)
                            {
                                Size += (long)Rom.Size;
                            }
                        }

                        return Size;
                    }
                }

                public class CollectionGameItem : MinimalGameItem
                {
                    public CollectionGameItem(MinimalGameItem gameObject)
                    {
                        this.Id = gameObject.Id;
                        this.Name = gameObject.Name;
                        this.Slug = gameObject.Slug;
                        this.TotalRating = gameObject.TotalRating;
                        this.TotalRatingCount = gameObject.TotalRatingCount;
                        this.Cover = gameObject.Cover;
                        this.Artworks = gameObject.Artworks;
                        this.FirstReleaseDate = gameObject.FirstReleaseDate;
                        this.AgeRatings = gameObject.AgeRatings;
                    }

                    public AgeGroups.AgeRestrictionGroupings AgeGrouping
                    {
                        get
                        {
                            List<AgeRating> gameAgeRatings = this.AgeRatings.Select(s => (AgeRating)s).ToList();
                            return AgeGroups.GetAgeGroupFromAgeRatings(gameAgeRatings);
                        }
                    }

                    public CollectionItem.AlwaysIncludeItem InclusionStatus { get; set; }

                    public List<Roms.GameRomItem> Roms { get; set; }

                    public long RomSize
                    {
                        get
                        {
                            long Size = 0;
                            foreach (Roms.GameRomItem Rom in Roms)
                            {
                                Size += (long)Rom.Size;
                            }

                            return Size;
                        }
                    }
                }
            }
        }
    }
}