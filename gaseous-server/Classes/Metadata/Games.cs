using System;
using System.Data;
using System.Security.Cryptography.X509Certificates;
using gaseous_server.Models;

namespace gaseous_server.Classes.Metadata
{
    public class Games
    {
        public const string fieldList = "fields age_ratings,aggregated_rating,aggregated_rating_count,alternative_names,artworks,bundles,category,checksum,collections,cover,created_at,dlcs,expanded_games,expansions,external_games,first_release_date,follows,forks,franchise,franchises,game_engines,game_localizations,game_modes,genres,hypes,involved_companies,keywords,language_supports,multiplayer_modes,name,parent_game,platforms,player_perspectives,ports,rating,rating_count,release_dates,remakes,remasters,screenshots,similar_games,slug,standalone_expansions,status,storyline,summary,tags,themes,total_rating,total_rating_count,updated_at,url,version_parent,version_title,videos,websites;";

        public Games()
        {

        }

        public class InvalidGameId : Exception
        {
            public InvalidGameId(long Id) : base("Unable to find Game by id " + Id)
            { }
        }

        public static Game? GetGame(HasheousClient.Models.MetadataSources SourceType, long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Game? RetVal = Metadata.GetMetadata<Game>(SourceType, (long)Id, false);
                RetVal.MetadataSource = SourceType;
                RetVal = MassageResult(RetVal);
                return RetVal;
            }
        }

        public static Game? GetGame(HasheousClient.Models.MetadataSources SourceType, string? Slug)
        {
            Game? RetVal = Metadata.GetMetadata<Game>(SourceType, Slug, false);
            RetVal.MetadataSource = SourceType;
            RetVal = MassageResult(RetVal);
            return RetVal;
        }

        public static Game GetGame(DataRow dataRow)
        {
            return Storage.BuildCacheObject<Game>(new Game(), dataRow);
        }

        private static Game MassageResult(Game result)
        {
            Game? parentGame = null;

            // get cover art from parent if this has no cover
            if (result.Cover == null)
            {
                if (result.ParentGame != null)
                {
                    Logging.Log(Logging.LogType.Information, "Game Metadata", "Game has no cover art, fetching cover art from parent game");
                    parentGame = GetGame(result.MetadataSource, (long)result.ParentGame);
                    result.Cover = parentGame.Cover;
                }
            }

            // get missing metadata from parent if this is a port
            if (result.Category == HasheousClient.Models.Metadata.IGDB.Category.Port)
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

            return result;
        }

        private static bool AllowNoPlatformSearch = false;

        public static Game[] SearchForGame(string SearchString, long PlatformId, SearchType searchType)
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

            string sql = "SELECT Game.Id, Game.`Name`, Game.Slug, Relation_Game_Platforms.PlatformsId AS PlatformsId, Game.Summary FROM gaseous.Game JOIN Relation_Game_Platforms ON Game.Id = Relation_Game_Platforms.GameId WHERE " + whereClause + ";";


            // get Game metadata
            Game[]? results = new Game[0];
            if (allowSearch == true)
            {
                List<Game> searchResults = new List<Game>();
                Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
                DataTable data = db.ExecuteCMD(sql, dbDict);
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
                    if (Config.MetadataConfiguration.MetadataUseHasheousProxy == false)
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

        public static List<AvailablePlatformItem> GetAvailablePlatforms(string UserId, long GameId)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = @"
SELECT DISTINCT
    view_Games_Roms.GameId,
    view_Games_Roms.PlatformId,
    Platform.`Name`,
    User_RecentPlayedRoms.UserId AS MostRecentUserId,
    User_RecentPlayedRoms.RomId AS MostRecentRomId,
    CASE User_RecentPlayedRoms.IsMediaGroup
        WHEN 0 THEN GMR.`Name`
        WHEN 1 THEN 'Media Group'
        ELSE NULL
    END AS `MostRecentRomName`,
    User_RecentPlayedRoms.IsMediaGroup AS MostRecentRomIsMediaGroup,
    User_GameFavouriteRoms.UserId AS FavouriteUserId,
    User_GameFavouriteRoms.RomId AS FavouriteRomId,
    CASE User_GameFavouriteRoms.IsMediaGroup
        WHEN 0 THEN GFV.`Name`
        WHEN 1 THEN 'Media Group'
        ELSE NULL
    END AS `FavouriteRomName`,
    User_GameFavouriteRoms.IsMediaGroup AS FavouriteRomIsMediaGroup
FROM
    view_Games_Roms
        LEFT JOIN
    Platform ON view_Games_Roms.PlatformId = Platform.Id
        LEFT JOIN
    User_RecentPlayedRoms ON User_RecentPlayedRoms.UserId = @userid
        AND User_RecentPlayedRoms.GameId = view_Games_Roms.GameId
        AND User_RecentPlayedRoms.PlatformId = view_Games_Roms.PlatformId
        LEFT JOIN
    User_GameFavouriteRoms ON User_GameFavouriteRoms.UserId = @userid
        AND User_GameFavouriteRoms.GameId = view_Games_Roms.GameId
        AND User_GameFavouriteRoms.PlatformId = view_Games_Roms.PlatformId
        LEFT JOIN
    view_Games_Roms AS GMR ON GMR.Id = User_RecentPlayedRoms.RomId
        LEFT JOIN
    view_Games_Roms AS GFV ON GFV.Id = User_GameFavouriteRoms.RomId
WHERE
    view_Games_Roms.GameId = @gameid
ORDER BY Platform.`Name`;";
            Dictionary<string, object> dbDict = new Dictionary<string, object>
            {
                { "gameid", GameId },
                { "userid", UserId }
            };
            DataTable data = db.ExecuteCMD(sql, dbDict);

            PlatformMapping platformMapping = new PlatformMapping();
            List<AvailablePlatformItem> platforms = new List<AvailablePlatformItem>();
            foreach (DataRow row in data.Rows)
            {
                HasheousClient.Models.Metadata.IGDB.Platform platform = Platforms.GetPlatform((long)row["PlatformId"]);
                PlatformMapping.UserEmulatorConfiguration? emulatorConfiguration = platformMapping.GetUserEmulator(UserId, GameId, (long)platform.Id);

                if (emulatorConfiguration == null)
                {
                    if (platform.Id != 0)
                    {
                        Models.PlatformMapping.PlatformMapItem platformMap = PlatformMapping.GetPlatformMap((long)platform.Id);
                        emulatorConfiguration = new PlatformMapping.UserEmulatorConfiguration
                        {
                            EmulatorType = platformMap.WebEmulator.Type,
                            Core = platformMap.WebEmulator.Core,
                            EnableBIOSFiles = platformMap.EnabledBIOSHashes
                        };
                    }
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
                    if (row["MostRecentRomName"] != System.DBNull.Value)
                    {
                        FavouriteRomName = string.IsNullOrEmpty((string?)row["MostRecentRomName"]) ? "" : (string)row["MostRecentRomName"];
                    }
                }

                AvailablePlatformItem valuePair = new AvailablePlatformItem
                {
                    Id = platform.Id,
                    Name = platform.Name,
                    Category = platform.Category,
                    emulatorConfiguration = emulatorConfiguration,
                    LastPlayedRomId = LastPlayedRomId,
                    LastPlayedRomIsMediagroup = LastPlayedIsMediagroup,
                    LastPlayedRomName = LastPlayedRomName,
                    FavouriteRomId = FavouriteRomId,
                    FavouriteRomIsMediagroup = FavouriteRomIsMediagroup,
                    FavouriteRomName = FavouriteRomName
                };
                platforms.Add(valuePair);
            }

            return platforms;
        }

        public static void GameSetFavouriteRom(string UserId, long GameId, long PlatformId, long RomId, bool IsMediaGroup)
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
            db.ExecuteCMD(sql, dbDict);
        }

        public static void GameClearFavouriteRom(string UserId, long GameId, long PlatformId)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "DELETE FROM User_GameFavouriteRoms WHERE UserId = @userid AND GameId = @gameid AND PlatformId = @platformid;";
            Dictionary<string, object> dbDict = new Dictionary<string, object>
            {
                { "userid", UserId },
                { "gameid", GameId },
                { "platformid", PlatformId }
            };
            db.ExecuteCMD(sql, dbDict);
        }

        public class AvailablePlatformItem : HasheousClient.Models.Metadata.IGDB.Platform
        {
            public PlatformMapping.UserEmulatorConfiguration emulatorConfiguration { get; set; }
            public long? LastPlayedRomId { get; set; }
            public bool? LastPlayedRomIsMediagroup { get; set; }
            public string? LastPlayedRomName { get; set; }
            public long? FavouriteRomId { get; set; }
            public bool? FavouriteRomIsMediagroup { get; set; }
            public string? FavouriteRomName { get; set; }
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
                this.Id = gameObject.Id;
                this.MetadataMapId = gameObject.MetadataMapId;
                this.Name = gameObject.Name;
                this.Slug = gameObject.Slug;
                this.Summary = gameObject.Summary;
                this.TotalRating = gameObject.TotalRating;
                this.TotalRatingCount = gameObject.TotalRatingCount;
                this.Cover = gameObject.Cover;
                this.Artworks = gameObject.Artworks;
                this.FirstReleaseDate = gameObject.FirstReleaseDate;

                // compile age ratings
                this.AgeRatings = new List<object>();
                if (gameObject.AgeRatings != null)
                {
                    foreach (long ageRatingId in gameObject.AgeRatings)
                    {
                        HasheousClient.Models.Metadata.IGDB.AgeRating? rating = Classes.Metadata.AgeRatings.GetAgeRating(gameObject.MetadataSource, ageRatingId);
                        if (rating != null)
                        {
                            this.AgeRatings.Add(rating);
                        }
                    }
                }
            }

            public long? Id { get; set; }
            public long? MetadataMapId { get; set; }
            public long Index { get; set; }
            public string Name { get; set; }
            public string Slug { get; set; }
            public string Summary { get; set; }
            public double? TotalRating { get; set; }
            public int? TotalRatingCount { get; set; }
            public bool HasSavedGame { get; set; } = false;
            public bool IsFavourite { get; set; } = false;
            public DateTimeOffset? FirstReleaseDate { get; set; }
            public object Cover { get; set; }
            public List<object> Artworks { get; set; }
            public List<object> AgeRatings { get; set; }
        }
    }
}