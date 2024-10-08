﻿using System;
using System.Data;
using gaseous_server.Models;
using IGDB;
using IGDB.Models;

namespace gaseous_server.Classes.Metadata
{
    public class Games
    {
        const string fieldList = "fields age_ratings,aggregated_rating,aggregated_rating_count,alternative_names,artworks,bundles,category,checksum,collections,cover,created_at,dlcs,expanded_games,expansions,external_games,first_release_date,follows,forks,franchise,franchises,game_engines,game_localizations,game_modes,genres,hypes,involved_companies,keywords,language_supports,multiplayer_modes,name,parent_game,platforms,player_perspectives,ports,rating,rating_count,release_dates,remakes,remasters,screenshots,similar_games,slug,standalone_expansions,status,storyline,summary,tags,themes,total_rating,total_rating_count,updated_at,url,version_parent,version_title,videos,websites;";

        public Games()
        {

        }

        public class InvalidGameId : Exception
        {
            public InvalidGameId(long Id) : base("Unable to find Game by id " + Id)
            { }
        }

        public static Game? GetGame(long Id, bool getAllMetadata, bool followSubGames, bool forceRefresh)
        {
            if (Id == 0)
            {
                Game returnValue = new Game();
                if (Storage.GetCacheStatus("Game", 0) == Storage.CacheStatus.NotPresent)
                {
                    returnValue = new Game
                    {
                        Id = 0,
                        Name = "Unknown Title",
                        Slug = "Unknown"
                    };
                    Storage.NewCacheValue(returnValue);

                    return returnValue;
                }
                else
                {
                    return Storage.GetCacheValue<Game>(returnValue, "id", 0);
                }
            }
            else
            {
                Task<Game> RetVal = _GetGame(SearchUsing.id, Id, getAllMetadata, followSubGames, forceRefresh);
                return RetVal.Result;
            }
        }

        public static Game GetGame(string Slug, bool getAllMetadata, bool followSubGames, bool forceRefresh)
        {
            Task<Game> RetVal = _GetGame(SearchUsing.slug, Slug, getAllMetadata, followSubGames, forceRefresh);
            return RetVal.Result;
        }

        public static Game GetGame(DataRow dataRow)
        {
            return Storage.BuildCacheObject<Game>(new Game(), dataRow);
        }

        private static async Task<Game> _GetGame(SearchUsing searchUsing, object searchValue, bool getAllMetadata = true, bool followSubGames = false, bool forceRefresh = false)
        {
            // check database first
            Storage.CacheStatus? cacheStatus = new Storage.CacheStatus();
            if (searchUsing == SearchUsing.id)
            {
                cacheStatus = Storage.GetCacheStatus("Game", (long)searchValue);
            }
            else
            {
                cacheStatus = Storage.GetCacheStatus("Game", (string)searchValue);
            }

            if (forceRefresh == true)
            {
                if (cacheStatus == Storage.CacheStatus.Current) { cacheStatus = Storage.CacheStatus.Expired; }
            }

            string WhereClause = "";
            string searchField = "";
            switch (searchUsing)
            {
                case SearchUsing.id:
                    WhereClause = "where id = " + searchValue;
                    searchField = "id";
                    break;
                case SearchUsing.slug:
                    WhereClause = "where slug = \"" + searchValue + "\"";
                    searchField = "slug";
                    break;
                default:
                    throw new Exception("Invalid search type");
            }

            Game returnValue = new Game();
            switch (cacheStatus)
            {
                case Storage.CacheStatus.NotPresent:
                    returnValue = await GetObjectFromServer(WhereClause);
                    Storage.NewCacheValue(returnValue);
                    UpdateSubClasses(returnValue, getAllMetadata, followSubGames, forceRefresh);
                    return returnValue;
                case Storage.CacheStatus.Expired:
                    try
                    {
                        returnValue = await GetObjectFromServer(WhereClause);
                        Storage.NewCacheValue(returnValue, true);
                        UpdateSubClasses(returnValue, getAllMetadata, followSubGames, forceRefresh);
                    }
                    catch (Exception ex)
                    {
                        Logging.Log(Logging.LogType.Warning, "Metadata: " + returnValue.GetType().Name, "An error occurred while connecting to IGDB. WhereClause: " + WhereClause, ex);
                        returnValue = Storage.GetCacheValue<Game>(returnValue, searchField, searchValue);
                    }
                    return returnValue;
                case Storage.CacheStatus.Current:
                    returnValue = Storage.GetCacheValue<Game>(returnValue, searchField, searchValue);
                    UpdateSubClasses(returnValue, false, false, false);
                    return returnValue;
                default:
                    throw new Exception("How did you get here?");
            }
        }

        private static void UpdateSubClasses(Game Game, bool getAllMetadata, bool followSubGames, bool forceRefresh)
        {
            // required metadata
            // if (Game.Cover != null)
            // {
            //     try
            //     {
            //         Cover GameCover = Covers.GetCover(Game.Cover.Id, Config.LibraryConfiguration.LibraryMetadataDirectory_Game(Game), forceRefresh);
            //     }
            //     catch (Exception ex)
            //     {
            //         Logging.Log(Logging.LogType.Critical, "Game Metadata", "Unable to fetch cover artwork.", ex);
            //     }
            // }

            if (Game.Genres != null)
            {
                foreach (long GenreId in Game.Genres.Ids)
                {
                    Genre GameGenre = Genres.GetGenres(GenreId);
                }
            }

            if (Game.GameModes != null)
            {
                foreach (long gameModeId in Game.GameModes.Ids)
                {
                    GameMode gameMode = GameModes.GetGame_Modes(gameModeId);
                }
            }

            if (Game.MultiplayerModes != null)
            {
                foreach (long multiplayerModeId in Game.MultiplayerModes.Ids)
                {
                    MultiplayerMode multiplayerMode = MultiplayerModes.GetGame_MultiplayerModes(multiplayerModeId);
                }
            }

            if (Game.PlayerPerspectives != null)
            {
                foreach (long PerspectiveId in Game.PlayerPerspectives.Ids)
                {
                    PlayerPerspective GamePlayPerspective = PlayerPerspectives.GetGame_PlayerPerspectives(PerspectiveId);
                }
            }

            if (Game.Themes != null)
            {
                foreach (long ThemeId in Game.Themes.Ids)
                {
                    Theme GameTheme = Themes.GetGame_Themes(ThemeId);
                }
            }

            if (Game.AgeRatings != null)
            {
                foreach (long AgeRatingId in Game.AgeRatings.Ids)
                {
                    AgeRating GameAgeRating = AgeRatings.GetAgeRatings(AgeRatingId);
                }
            }
            AgeGroups.GetAgeGroup(Game);

            if (Game.ReleaseDates != null)
            {
                foreach (long ReleaseDateId in Game.ReleaseDates.Ids)
                {
                    ReleaseDate GameReleaseDate = ReleaseDates.GetReleaseDates(ReleaseDateId);
                }
            }

            // optional metadata - usually downloaded as needed
            if (getAllMetadata == true)
            {
                if (Game.AlternativeNames != null)
                {
                    foreach (long AlternativeNameId in Game.AlternativeNames.Ids)
                    {
                        AlternativeName GameAlternativeName = AlternativeNames.GetAlternativeNames(AlternativeNameId);
                    }
                }

                if (Game.Artworks != null)
                {
                    foreach (long ArtworkId in Game.Artworks.Ids)
                    {
                        try
                        {
                            Artwork GameArtwork = Artworks.GetArtwork(ArtworkId, Config.LibraryConfiguration.LibraryMetadataDirectory_Game(Game), forceRefresh);
                        }
                        catch (Exception ex)
                        {
                            Logging.Log(Logging.LogType.Critical, "Game Metadata", "Unable to fetch artwork id: " + ArtworkId, ex);
                        }
                    }
                }

                if (followSubGames)
                {
                    List<long> gamesToFetch = new List<long>();
                    if (Game.Bundles != null) { gamesToFetch.AddRange(Game.Bundles.Ids); }
                    if (Game.Dlcs != null) { gamesToFetch.AddRange(Game.Dlcs.Ids); }
                    if (Game.Expansions != null) { gamesToFetch.AddRange(Game.Expansions.Ids); }
                    if (Game.ParentGame != null) { gamesToFetch.Add((long)Game.ParentGame.Id); }
                    //if (Game.SimilarGames != null) { gamesToFetch.AddRange(Game.SimilarGames.Ids); }
                    if (Game.StandaloneExpansions != null) { gamesToFetch.AddRange(Game.StandaloneExpansions.Ids); }
                    if (Game.VersionParent != null) { gamesToFetch.Add((long)Game.VersionParent.Id); }

                    foreach (long gameId in gamesToFetch)
                    {
                        Game relatedGame = GetGame(gameId, false, true, false);
                    }
                }

                if (Game.Collection != null)
                {
                    Collection GameCollection = Collections.GetCollections(Game.Collection.Id);
                }

                if (Game.ExternalGames != null)
                {
                    foreach (long ExternalGameId in Game.ExternalGames.Ids)
                    {
                        ExternalGame GameExternalGame = ExternalGames.GetExternalGames(ExternalGameId);
                    }
                }

                if (Game.Franchise != null)
                {
                    Franchise GameFranchise = Franchises.GetFranchises(Game.Franchise.Id);
                }

                if (Game.Franchises != null)
                {
                    foreach (long FranchiseId in Game.Franchises.Ids)
                    {
                        Franchise GameFranchise = Franchises.GetFranchises(FranchiseId);
                    }
                }

                if (Game.InvolvedCompanies != null)
                {
                    foreach (long involvedCompanyId in Game.InvolvedCompanies.Ids)
                    {
                        InvolvedCompany involvedCompany = InvolvedCompanies.GetInvolvedCompanies(involvedCompanyId);
                    }
                }

                if (Game.Platforms != null)
                {
                    foreach (long PlatformId in Game.Platforms.Ids)
                    {
                        Platform GamePlatform = Platforms.GetPlatform(PlatformId);
                    }
                }

                if (Game.Screenshots != null)
                {
                    foreach (long ScreenshotId in Game.Screenshots.Ids)
                    {
                        try
                        {
                            Screenshot GameScreenshot = Screenshots.GetScreenshot(ScreenshotId, Config.LibraryConfiguration.LibraryMetadataDirectory_Game(Game), forceRefresh);
                        }
                        catch (Exception ex)
                        {
                            Logging.Log(Logging.LogType.Critical, "Game Metadata", "Unable to fetch screenshot id: " + ScreenshotId, ex);
                        }
                    }
                }

                if (Game.Videos != null)
                {
                    foreach (long GameVideoId in Game.Videos.Ids)
                    {
                        GameVideo gameVideo = GamesVideos.GetGame_Videos(GameVideoId);
                    }
                }
            }
        }

        private enum SearchUsing
        {
            id,
            slug
        }

        private static async Task<Game> GetObjectFromServer(string WhereClause)
        {
            // get Game metadata
            Communications comms = new Communications();
            var results = await comms.APIComm<Game>(IGDBClient.Endpoints.Games, fieldList, WhereClause);
            var result = results.First();

            // add artificial unknown platform mapping
            List<long> platformIds = new List<long>();
            platformIds.Add(0);
            if (result.Platforms != null)
            {
                if (result.Platforms.Ids != null)
                {
                    platformIds.AddRange(result.Platforms.Ids.ToList());
                }
            }
            result.Platforms = new IdentitiesOrValues<Platform>(
                ids: platformIds.ToArray<long>()
            );

            // get cover art from parent if this has no cover
            if (result.Cover == null)
            {
                if (result.ParentGame != null)
                {
                    if (result.ParentGame.Id != null)
                    {
                        Logging.Log(Logging.LogType.Information, "Game Metadata", "Game has no cover art, fetching cover art from parent game");
                        Game parentGame = GetGame((long)result.ParentGame.Id, false, false, false);
                        result.Cover = parentGame.Cover;
                    }
                }
            }

            // get missing metadata from parent if this is a port
            if (result.Category == Category.Port)
            {
                if (result.Summary == null)
                {
                    if (result.ParentGame != null)
                    {
                        if (result.ParentGame.Id != null)
                        {
                            Logging.Log(Logging.LogType.Information, "Game Metadata", "Game has no summary, fetching summary from parent game");
                            Game parentGame = GetGame((long)result.ParentGame.Id, false, false, false);
                            result.Summary = parentGame.Summary;
                        }
                    }
                }
            }

            return result;
        }

        public static void AssignAllGamesToPlatformIdZero()
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "SELECT * FROM Game;";
            DataTable gamesTable = db.ExecuteCMD(sql);
            foreach (DataRow gameRow in gamesTable.Rows)
            {
                sql = "DELETE FROM Relation_Game_Platforms WHERE PlatformsId = 0 AND GameId = @Id; INSERT INTO Relation_Game_Platforms (GameId, PlatformsId) VALUES (@Id, 0);";
                Dictionary<string, object> dbDict = new Dictionary<string, object>();
                dbDict.Add("Id", (long)gameRow["Id"]);
                db.ExecuteCMD(sql, dbDict);
            }
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
                    results = await comms.APIComm<Game>(IGDBClient.Endpoints.Games, searchFields, searchBody);

                    Communications.SetSearchCache<Game[]?>(searchFields, searchBody, results);
                }

                return results;
            }
            else
            {
                return games.ToArray();
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
                IGDB.Models.Platform platform = Platforms.GetPlatform((long)row["PlatformId"]);
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

        public class AvailablePlatformItem : IGDB.Models.Platform
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
                this.Name = gameObject.Name;
                this.Slug = gameObject.Slug;
                this.Summary = gameObject.Summary;
                this.TotalRating = gameObject.TotalRating;
                this.TotalRatingCount = gameObject.TotalRatingCount;
                this.Cover = gameObject.Cover;
                this.Artworks = gameObject.Artworks;
                this.FirstReleaseDate = gameObject.FirstReleaseDate;

                // compile age ratings
                this.AgeRatings = new List<AgeRating>();
                if (gameObject.AgeRatings != null)
                {
                    foreach (long ageRatingId in gameObject.AgeRatings.Ids)
                    {
                        AgeRating? rating = Classes.Metadata.AgeRatings.GetAgeRatings(ageRatingId);
                        if (rating != null)
                        {
                            this.AgeRatings.Add(rating);
                        }
                    }
                }
            }

            public long? Id { get; set; }
            public long Index { get; set; }
            public string Name { get; set; }
            public string Slug { get; set; }
            public string Summary { get; set; }
            public double? TotalRating { get; set; }
            public int? TotalRatingCount { get; set; }
            public bool HasSavedGame { get; set; } = false;
            public bool IsFavourite { get; set; } = false;
            public DateTimeOffset? FirstReleaseDate { get; set; }
            public IGDB.IdentityOrValue<IGDB.Models.Cover> Cover { get; set; }
            public IGDB.IdentitiesOrValues<IGDB.Models.Artwork> Artworks { get; set; }
            public List<IGDB.Models.AgeRating> AgeRatings { get; set; }
        }
    }
}