using System;
using System.Data;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using gaseous_server.Models;

namespace gaseous_server.Classes.Metadata
{
    public class Games
    {
        /// <summary>
        /// Sources are in order: ScreenScaper, TheGamesDB
        /// </summary>
        public static List<HasheousClient.Models.MetadataSources> clearLogoSources = new List<HasheousClient.Models.MetadataSources>{
                HasheousClient.Models.MetadataSources.TheGamesDb
            };

        public Games()
        {

        }

        public class InvalidGameId : Exception
        {
            public InvalidGameId(long Id) : base("Unable to find Game by id " + Id)
            { }
        }

        public static async Task<Game?> GetGame(HasheousClient.Models.MetadataSources SourceType, long? Id, bool Massage = true)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Game? RetVal = await Metadata.GetMetadataAsync<Game>(SourceType, (long)Id, false);
                RetVal.MetadataSource = SourceType;
                long? metadataMap = MetadataManagement.GetMetadataMapIdFromSourceId(SourceType, (long)Id);
                if (metadataMap == null)
                {
                    metadataMap = MetadataManagement.GetMetadataMapIdFromSourceId(SourceType, (long)Id, false);
                }

                if (metadataMap != null)
                {
                    RetVal.MetadataMapId = (long)metadataMap;
                }

                if (Massage == true)
                {
                    RetVal = await MassageResult(RetVal);
                }

                return RetVal;
            }
        }

        public static async Task<Game?> GetGame(HasheousClient.Models.MetadataSources SourceType, string? Slug)
        {
            Game? RetVal = await Metadata.GetMetadataAsync<Game>(SourceType, Slug, false);
            RetVal.MetadataSource = SourceType;
            RetVal = await MassageResult(RetVal);
            return RetVal;
        }

        public static Game GetGame(DataRow dataRow)
        {
            return Storage.BuildCacheObject<Game>(new Game(), dataRow);
        }

        private static async Task<Game> MassageResult(Game result)
        {
            Game? parentGame = null;

            if (result.ParentGame != null)
            {
                parentGame = await GetGame(result.MetadataSource, (long)result.ParentGame);
            }

            // get cover art from parent if this has no cover
            if (result.Cover == null)
            {
                Logging.Log(Logging.LogType.Information, "Game Metadata", "Game has no cover art, fetching cover art from parent game");
                result.Cover = parentGame.Cover;
            }

            // get missing metadata from parent if this is a port
            if (result.GameType == 11) // 11 = Port
            {
                if (result.Summary == null)
                {
                    if (result.ParentGame != null)
                    {
                        Logging.Log(Logging.LogType.Information, "Game Metadata", "Game has no summary, fetching summary from parent game");
                        result.Summary = parentGame.Summary;
                    }
                }
            }

            // get associated metadataMapIds
            List<long> metadataMapIds = await MetadataManagement.GetAssociatedMetadataMapIds(result.MetadataMapId);
            List<MetadataMap> metadataMaps = new List<MetadataMap>();
            foreach (long metadataMapId in metadataMapIds)
            {
                MetadataMap? metadataMap = await MetadataManagement.GetMetadataMap(metadataMapId);
                if (metadataMap != null)
                {
                    metadataMaps.Add(metadataMap);
                }
            }

            // search for a clear logo
            result.ClearLogo = new Dictionary<HasheousClient.Models.MetadataSources, List<long>>();
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            foreach (HasheousClient.Models.MetadataSources source in clearLogoSources)
            {
                foreach (MetadataMap metadataMap in metadataMaps)
                {
                    // select the source from the metadataMapItems
                    if (metadataMap.MetadataMapItems == null)
                    {
                        continue;
                    }
                    if (metadataMap.MetadataMapItems.Count == 0)
                    {
                        continue;
                    }

                    MetadataMap.MetadataMapItem? metadataMapItem = metadataMap.MetadataMapItems.FirstOrDefault(x => x.SourceType == source);

                    if (metadataMapItem != null)
                    {
                        // force refresh of game data
                        await GetGame(source, metadataMapItem.SourceId, false);

                        // found a valid source - check if there is a clear logo
                        string sql = "SELECT * FROM ClearLogo WHERE SourceId = @sourceid AND Game = @gameid;";
                        Dictionary<string, object> dbDict = new Dictionary<string, object>
                        {
                            { "sourceid", (int)source },
                            { "gameid", metadataMapItem.SourceId }
                        };
                        DataTable data = await db.ExecuteCMDAsync(sql, dbDict);
                        foreach (DataRow row in data.Rows)
                        {
                            if (result.ClearLogo.ContainsKey(source) == false)
                            {
                                result.ClearLogo.Add(source, new List<long>());
                            }
                            result.ClearLogo[source].Add((long)row["Id"]);
                        }

                        if (result.ClearLogo.Count > 0)
                        {
                            // found a valid source, break out of the loop
                            break;
                        }
                    }
                }
            }

            // populate age group data
            if (result.MetadataSource == HasheousClient.Models.MetadataSources.IGDB)
            {
                await AgeGroups.GetAgeGroup(result);
            }

            return result;
        }

        private static bool AllowNoPlatformSearch = false;

        public static async Task<Game[]> SearchForGame(string SearchString, long PlatformId, SearchType searchType)
        {
            // search local first
            Logging.Log(Logging.LogType.Information, "Game Search", "Attempting local search of type '" + searchType.ToString() + "' for " + SearchString);
            Task<Game[]> games = _SearchForGameDatabase(SearchString, PlatformId, searchType);
            if (games.Result.Length == 0)
            {
                // fall back to online search
                Logging.Log(Logging.LogType.Information, "Game Search", "Falling back to remote search of type '" + searchType.ToString() + "' for " + SearchString);
                games = _SearchForGameRemote(SearchString, PlatformId, searchType);
            }
            return games.Result;
        }

        private static async Task<Game[]> _SearchForGameDatabase(string SearchString, long PlatformId, SearchType searchType)
        {
            string whereClause = "";
            Dictionary<string, object> dbDict = new Dictionary<string, object>();

            bool allowSearch = true;
            switch (searchType)
            {
                case SearchType.searchNoPlatform:
                    whereClause = "MATCH(`Name`) AGAINST (@gamename)";
                    dbDict.Add("platformid", PlatformId);
                    dbDict.Add("gamename", SearchString);

                    allowSearch = AllowNoPlatformSearch;
                    break;
                case SearchType.search:
                    whereClause = "PlatformsId = @platformid AND MATCH(`Name`) AGAINST (@gamename)";
                    dbDict.Add("platformid", PlatformId);
                    dbDict.Add("gamename", SearchString);
                    break;
                case SearchType.wherefuzzy:
                    whereClause = "PlatformsId = @platformid AND `Name` LIKE @gamename";
                    dbDict.Add("platformid", PlatformId);
                    dbDict.Add("gamename", "%" + SearchString + "%");
                    break;
                case SearchType.where:
                    whereClause = "PlatformsId = @platformid AND `Name` = @gamename";
                    dbDict.Add("platformid", PlatformId);
                    dbDict.Add("gamename", SearchString);
                    break;
            }

            string sql = "SELECT Game.Id, Game.`Name`, Game.Slug, Relation_Game_Platforms.PlatformsId AS PlatformsId, Game.Summary FROM gaseous.Game JOIN Relation_Game_Platforms ON Game.Id = Relation_Game_Platforms.GameId AND Game.SourceId = Relation_Game_Platforms.GameSourceId WHERE " + whereClause + ";";


            // get Game metadata
            Game[]? results = new Game[0];
            if (allowSearch == true)
            {
                List<Game> searchResults = new List<Game>();
                Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
                DataTable data = await db.ExecuteCMDAsync(sql, dbDict);
                foreach (DataRow row in data.Rows)
                {
                    Game game = new Game
                    {
                        Id = (long)row["Id"],
                        Name = (string)Common.ReturnValueIfNull(row["Name"], ""),
                        Slug = (string)Common.ReturnValueIfNull(row["Slug"], ""),
                        Summary = (string)Common.ReturnValueIfNull(row["Summary"], "")
                    };
                    searchResults.Add(game);
                }

                results = searchResults.ToArray();
            }

            return results;
        }

        private static async Task<Game[]> _SearchForGameRemote(string SearchString, long PlatformId, SearchType searchType)
        {
            switch (Config.MetadataConfiguration.DefaultMetadataSource)
            {
                case HasheousClient.Models.MetadataSources.None:
                    return new Game[0];
                case HasheousClient.Models.MetadataSources.IGDB:
                    if (Config.IGDB.UseHasheousProxy == false)
                    {
                        string searchBody = "";
                        string searchFields = "fields id,name,slug,platforms,summary; ";
                        bool allowSearch = true;
                        switch (searchType)
                        {
                            case SearchType.searchNoPlatform:
                                searchBody = "search \"" + SearchString + "\"; ";

                                allowSearch = AllowNoPlatformSearch;
                                break;
                            case SearchType.search:
                                searchBody = "search \"" + SearchString + "\"; where platforms = (" + PlatformId + ");";
                                break;
                            case SearchType.wherefuzzy:
                                searchBody = "where platforms = (" + PlatformId + ") & name ~ *\"" + SearchString + "\"*;";
                                break;
                            case SearchType.where:
                                searchBody = "where platforms = (" + PlatformId + ") & name ~ \"" + SearchString + "\";";
                                break;
                        }

                        // check search cache
                        Game[]? games = Communications.GetSearchCache<Game[]?>(searchFields, searchBody);

                        if (games == null)
                        {
                            // cache miss
                            // get Game metadata
                            Communications comms = new Communications();
                            Game[]? results = new Game[0];
                            if (allowSearch == true)
                            {
                                results = await comms.APIComm<Game>(IGDB.IGDBClient.Endpoints.Games, searchFields, searchBody);

                                Communications.SetSearchCache<Game[]?>(searchFields, searchBody, results);
                            }

                            return results;
                        }
                        else
                        {
                            return games.ToArray();
                        }
                    }
                    else
                    {
                        HasheousClient.Hasheous hasheous = new HasheousClient.Hasheous();
                        Communications.ConfigureHasheousClient(ref hasheous);
                        HasheousClient.Models.Metadata.IGDB.Game[] hResults = hasheous.GetMetadataProxy_SearchGame<HasheousClient.Models.Metadata.IGDB.Game>(HasheousClient.Hasheous.MetadataProvider.IGDB, PlatformId.ToString(), SearchString);

                        List<Game> hGames = new List<Game>();
                        foreach (HasheousClient.Models.Metadata.IGDB.Game hResult in hResults)
                        {
                            hGames.Add(Communications.ConvertToIGDBModel<Game>(hResult));
                        }

                        return hGames.ToArray();
                    }
                default:
                    return new Game[0];
            }
        }

        public static async Task<List<AvailablePlatformItem>> GetAvailablePlatformsAsync(string UserId, HasheousClient.Models.MetadataSources SourceType, long GameId)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = @"
SELECT DISTINCT
	view_Games_Roms.MetadataMapId,
    view_Games_Roms.MetadataGameName,
    view_Games_Roms.GameId,
    view_Games_Roms.PlatformId,
    view_Games_Roms.UserManualLink,
    Platform.`Name`,
    User_RecentPlayedRoms.UserId AS MostRecentUserId,
    User_RecentPlayedRoms.RomId AS MostRecentRomId,
    CASE User_RecentPlayedRoms.IsMediaGroup
        WHEN 0 THEN GMR.`Name`
        WHEN 1 THEN view_Games_Roms.`MetadataGameName`
        ELSE NULL
    END AS `MostRecentRomName`,
    User_RecentPlayedRoms.IsMediaGroup AS MostRecentRomIsMediaGroup,
    User_GameFavouriteRoms.UserId AS FavouriteUserId,
    User_GameFavouriteRoms.RomId AS FavouriteRomId,
    CASE User_GameFavouriteRoms.IsMediaGroup
        WHEN 0 THEN GFV.`Name`
        WHEN 1 THEN view_Games_Roms.`MetadataGameName`
        ELSE NULL
    END AS `FavouriteRomName`,
    User_GameFavouriteRoms.IsMediaGroup AS FavouriteRomIsMediaGroup,
    (SELECT 
            MAX(SessionTime)
        FROM
            UserTimeTracking
        WHERE
            UserId = @userid
                AND PlatformId = view_Games_Roms.PlatformId
                AND GameId = view_Games_Roms.MetadataMapId) AS SessionTime
FROM
    view_Games_Roms
        LEFT JOIN
    Platform ON view_Games_Roms.PlatformId = Platform.Id AND Platform.SourceId = view_Games_Roms.GameIdType
        LEFT JOIN
    User_RecentPlayedRoms ON User_RecentPlayedRoms.UserId = @userid
        AND User_RecentPlayedRoms.GameId = view_Games_Roms.MetadataMapId
        AND User_RecentPlayedRoms.PlatformId = view_Games_Roms.PlatformId
        LEFT JOIN
    User_GameFavouriteRoms ON User_GameFavouriteRoms.UserId = @userid
        AND User_GameFavouriteRoms.GameId = view_Games_Roms.MetadataMapId
        AND User_GameFavouriteRoms.PlatformId = view_Games_Roms.PlatformId
        LEFT JOIN
    view_Games_Roms AS GMR ON GMR.Id = User_RecentPlayedRoms.RomId
        LEFT JOIN
    view_Games_Roms AS GFV ON GFV.Id = User_GameFavouriteRoms.RomId
WHERE
    view_Games_Roms.GameIdType = @sourcetype AND view_Games_Roms.GameId = @gameid
ORDER BY Platform.`Name`, view_Games_Roms.MetadataGameName;";
            Dictionary<string, object> dbDict = new Dictionary<string, object>
            {
                { "sourcetype", (int)SourceType },
                { "gameid", GameId },
                { "userid", UserId }
            };
            DataTable data = await db.ExecuteCMDAsync(sql, dbDict);

            PlatformMapping platformMapping = new PlatformMapping();
            List<AvailablePlatformItem> platforms = new List<AvailablePlatformItem>();
            foreach (DataRow row in data.Rows)
            {
                HasheousClient.Models.Metadata.IGDB.Platform platform = await Platforms.GetPlatform((long)row["PlatformId"]);

                // get the user emulator configuration
                PlatformMapping.UserEmulatorConfiguration? emulatorConfiguration = await platformMapping.GetUserEmulator(UserId, GameId, (long)platform.Id);

                // if no user configuration, get the platform emulator configuration
                if (emulatorConfiguration == null)
                {
                    if (platform.Id != 0)
                    {
                        Models.PlatformMapping.PlatformMapItem platformMap = await PlatformMapping.GetPlatformMap((long)platform.Id);
                        if (platformMap != null)
                        {
                            emulatorConfiguration = new PlatformMapping.UserEmulatorConfiguration
                            {
                                EmulatorType = platformMap.WebEmulator.Type,
                                Core = platformMap.WebEmulator.Core,
                                EnableBIOSFiles = platformMap.EnabledBIOSHashes
                            };
                        }
                    }
                }

                // if still no emulator configuration, create a blank one
                if (emulatorConfiguration == null)
                {
                    emulatorConfiguration = new PlatformMapping.UserEmulatorConfiguration
                    {
                        EmulatorType = "",
                        Core = "",
                        EnableBIOSFiles = new List<string>()
                    };
                }

                long? LastPlayedRomId = null;
                bool? LastPlayedIsMediagroup = false;
                string? LastPlayedRomName = null;
                if (row["MostRecentRomId"] != DBNull.Value)
                {
                    LastPlayedRomId = (long?)row["MostRecentRomId"];
                    LastPlayedIsMediagroup = (bool)row["MostRecentRomIsMediaGroup"];
                    if (row["MostRecentRomName"] != System.DBNull.Value)
                    {
                        LastPlayedRomName = string.IsNullOrEmpty((string?)row["MostRecentRomName"]) ? "" : (string)row["MostRecentRomName"];
                    }
                }

                long? FavouriteRomId = null;
                bool? FavouriteRomIsMediagroup = false;
                string? FavouriteRomName = null;
                if (row["FavouriteRomId"] != DBNull.Value)
                {
                    FavouriteRomId = (long?)row["FavouriteRomId"];
                    FavouriteRomIsMediagroup = (bool)row["FavouriteRomIsMediaGroup"];
                    if (row["FavouriteRomName"] != System.DBNull.Value)
                    {
                        FavouriteRomName = string.IsNullOrEmpty((string?)row["FavouriteRomName"]) ? "" : (string)row["FavouriteRomName"];
                    }
                }

                string? UserManualLink = null;
                if (row["UserManualLink"] != DBNull.Value)
                {
                    UserManualLink = string.IsNullOrEmpty((string?)row["UserManualLink"]) ? "" : (string)row["UserManualLink"];
                }

                DateTime? LastPlayed = null;
                if (row["SessionTime"] != DBNull.Value)
                {
                    LastPlayed = row["SessionTime"] as DateTime?;
                }

                AvailablePlatformItem valuePair = new AvailablePlatformItem
                {
                    Id = platform.Id,
                    Name = platform.Name,
                    MetadataMapId = (long)row["MetadataMapId"],
                    MetadataGameName = (string)row["MetadataGameName"],
                    emulatorConfiguration = emulatorConfiguration,
                    LastPlayedRomId = LastPlayedRomId,
                    LastPlayedRomIsMediagroup = LastPlayedIsMediagroup,
                    LastPlayedRomName = LastPlayedRomName,
                    FavouriteRomId = FavouriteRomId,
                    FavouriteRomIsMediagroup = FavouriteRomIsMediagroup,
                    FavouriteRomName = FavouriteRomName,
                    UserManualLink = UserManualLink,
                    LastPlayed = LastPlayed
                };
                platforms.Add(valuePair);
            }

            // sort platforms by the Name attribute
            platforms.Sort((x, y) => x.Name.CompareTo(y.Name));

            return platforms;
        }

        public static async Task GameSetFavouriteRom(string UserId, long GameId, long PlatformId, long RomId, bool IsMediaGroup)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "DELETE FROM User_GameFavouriteRoms WHERE UserId = @userid AND GameId = @gameid AND PlatformId = @platformid; INSERT INTO User_GameFavouriteRoms (UserId, GameId, PlatformId, RomId, IsMediaGroup) VALUES (@userid, @gameid, @platformid, @romid, @ismediagroup);";
            Dictionary<string, object> dbDict = new Dictionary<string, object>
            {
                { "userid", UserId },
                { "gameid", GameId },
                { "platformid", PlatformId },
                { "romid", RomId },
                { "ismediagroup", IsMediaGroup }
            };
            await db.ExecuteCMDAsync(sql, dbDict);
        }

        public static async Task GameClearFavouriteRom(string UserId, long GameId, long PlatformId)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "DELETE FROM User_GameFavouriteRoms WHERE UserId = @userid AND GameId = @gameid AND PlatformId = @platformid;";
            Dictionary<string, object> dbDict = new Dictionary<string, object>
            {
                { "userid", UserId },
                { "gameid", GameId },
                { "platformid", PlatformId }
            };
            await db.ExecuteCMDAsync(sql, dbDict);
        }

        public class AvailablePlatformItem : HasheousClient.Models.Metadata.IGDB.Platform
        {
            public long MetadataMapId { get; set; }
            public string? MetadataGameName { get; set; }
            public PlatformMapping.UserEmulatorConfiguration emulatorConfiguration { get; set; }
            public long? LastPlayedRomId { get; set; }
            public bool? LastPlayedRomIsMediagroup { get; set; }
            public string? LastPlayedRomName { get; set; }
            public long? FavouriteRomId { get; set; }
            public bool? FavouriteRomIsMediagroup { get; set; }
            public string? FavouriteRomName { get; set; }
            public string? UserManualLink { get; set; }
            public DateTime? LastPlayed { get; set; }
        }

        public enum SearchType
        {
            where = 0,
            wherefuzzy = 1,
            search = 2,
            searchNoPlatform = 3
        }

        public class MinimalGameItem
        {
            public MinimalGameItem()
            {

            }

            public MinimalGameItem(Game gameObject)
            {
                this.PlatformIds = gameObject.Platforms;
                this.Id = gameObject.Id;
                this.MetadataMapId = gameObject.MetadataMapId;
                this.MetadataSource = gameObject.MetadataSource;
                this.Name = gameObject.Name;
                this.Slug = gameObject.Slug;
                this.Summary = gameObject.Summary;
                this.TotalRating = gameObject.TotalRating;
                this.TotalRatingCount = gameObject.TotalRatingCount;
                this.Cover = gameObject.Cover;
                this.Artworks = gameObject.Artworks;
                this.FirstReleaseDate = gameObject.FirstReleaseDate;

                // compile genres
                this.Genres = new List<string>();
                if (gameObject.Genres != null)
                {
                    foreach (long genreId in gameObject.Genres)
                    {
                        HasheousClient.Models.Metadata.IGDB.Genre? genre = Classes.Metadata.Genres.GetGenres(gameObject.MetadataSource, genreId).Result;
                        if (genre != null)
                        {
                            if (!this.Genres.Contains(genre.Name))
                            {
                                this.Genres.Add(genre.Name);
                            }
                        }
                    }
                }

                // compile themes
                this.Themes = new List<string>();
                if (gameObject.Themes != null)
                {
                    foreach (long themeId in gameObject.Themes)
                    {
                        HasheousClient.Models.Metadata.IGDB.Theme? theme = Classes.Metadata.Themes.GetGame_ThemesAsync(gameObject.MetadataSource, themeId).Result;
                        if (theme != null)
                        {
                            if (!this.Themes.Contains(theme.Name))
                            {
                                this.Themes.Add(theme.Name);
                            }
                        }
                    }
                }

                // compile players
                this.Players = new List<string>();
                if (gameObject.GameModes != null)
                {
                    foreach (long playerId in gameObject.GameModes)
                    {
                        HasheousClient.Models.Metadata.IGDB.GameMode? player = Classes.Metadata.GameModes.GetGame_Modes(gameObject.MetadataSource, playerId).Result;
                        if (player != null)
                        {
                            if (!this.Players.Contains(player.Name))
                            {
                                this.Players.Add(player.Name);
                            }
                        }
                    }
                }

                // compile perspectives
                this.Perspectives = new List<string>();
                if (gameObject.PlayerPerspectives != null)
                {
                    foreach (long perspectiveId in gameObject.PlayerPerspectives)
                    {
                        HasheousClient.Models.Metadata.IGDB.PlayerPerspective? perspective = Classes.Metadata.PlayerPerspectives.GetGame_PlayerPerspectives(gameObject.MetadataSource, perspectiveId).Result;
                        if (perspective != null)
                        {
                            if (!this.Perspectives.Contains(perspective.Name))
                            {
                                this.Perspectives.Add(perspective.Name);
                            }
                        }
                    }
                }

                // compile age ratings
                this.AgeRatings = new List<HasheousClient.Models.Metadata.IGDB.AgeRating>();
                if (gameObject.AgeRatings != null)
                {
                    foreach (long ageRatingId in gameObject.AgeRatings)
                    {
                        HasheousClient.Models.Metadata.IGDB.AgeRating? rating = Classes.Metadata.AgeRatings.GetAgeRating(gameObject.MetadataSource, ageRatingId).Result;
                        if (rating != null)
                        {
                            this.AgeRatings.Add(rating);
                        }
                    }
                }
            }

            public List<long>? PlatformIds { get; set; }
            public long? Id { get; set; }
            public long? MetadataMapId { get; set; }
            public HasheousClient.Models.MetadataSources MetadataSource { get; set; }
            public long Index { get; set; }
            public string Alpha
            {
                get
                {
                    if (string.IsNullOrEmpty(Name) == false)
                    {
                        string firstLetter = Name.Substring(0, 1).ToUpper();
                        if (char.IsLetter(firstLetter[0]))
                        {
                            return firstLetter;
                        }
                        else
                        {
                            return "#";
                        }
                    }
                    else
                    {
                        return "#";
                    }
                }
            }
            public string Name { get; set; }
            public string NameThe
            {
                get
                {
                    // if Name starts with "The ", move it to the end
                    if (Name.StartsWith("The ", StringComparison.OrdinalIgnoreCase))
                    {
                        return Name.Substring(4) + ", The";
                    }
                    else
                    {
                        return Name;
                    }
                }
            }
            public List<string> AlternateNames { get; set; }
            public string Slug { get; set; }
            public string Summary { get; set; }
            public double? TotalRating { get; set; }
            public int? TotalRatingCount { get; set; }
            public bool HasSavedGame { get; set; } = false;
            public bool IsFavourite { get; set; } = false;
            public List<string> Genres { get; set; }
            public List<string> Themes { get; set; }
            public List<string> Players { get; set; }
            public List<string> Perspectives { get; set; }
            public DateTimeOffset? FirstReleaseDate { get; set; }
            public object Cover { get; set; }
            public List<long> Artworks { get; set; }
            public List<HasheousClient.Models.Metadata.IGDB.AgeRating> AgeRatings { get; set; }
            public AgeGroups.AgeRestrictionGroupings AgeGroup
            {
                get
                {
                    return AgeGroups.GetAgeGroupFromAgeRatings(AgeRatings);
                }
            }
        }
    }
}