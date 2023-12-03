using System;
using System.Data;
using IGDB;
using IGDB.Models;

namespace gaseous_server.Classes.Metadata
{
	public class Games
	{
        const string fieldList = "fields age_ratings,aggregated_rating,aggregated_rating_count,alternative_names,artworks,bundles,category,checksum,collection,cover,created_at,dlcs,expanded_games,expansions,external_games,first_release_date,follows,forks,franchise,franchises,game_engines,game_localizations,game_modes,genres,hypes,involved_companies,keywords,language_supports,multiplayer_modes,name,parent_game,platforms,player_perspectives,ports,rating,rating_count,release_dates,remakes,remasters,screenshots,similar_games,slug,standalone_expansions,status,storyline,summary,tags,themes,total_rating,total_rating_count,updated_at,url,version_parent,version_title,videos,websites;";

        public Games()
        {

        }

        public class InvalidGameId : Exception
        { 
            public InvalidGameId(long Id) : base("Unable to find Game by id " + Id)
            {}
        }

        private static IGDBClient igdb = new IGDBClient(
                    // Found in Twitch Developer portal for your app
                    Config.IGDB.ClientId,
                    Config.IGDB.Secret
                );

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

            // set up where clause
            string WhereClause = "";
            switch (searchUsing)
            {
                case SearchUsing.id:
                    WhereClause = "where id = " + searchValue;
                    break;
                case SearchUsing.slug:
                    WhereClause = "where slug = " + searchValue;
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
                    UpdateSubClasses(returnValue, getAllMetadata, followSubGames);
                    return returnValue;
                case Storage.CacheStatus.Expired:
                    try
                    {
                        returnValue = await GetObjectFromServer(WhereClause);
                        Storage.NewCacheValue(returnValue, true);
                        UpdateSubClasses(returnValue, getAllMetadata, followSubGames);
                    }
                    catch (Exception ex)
                    {
                        Logging.Log(Logging.LogType.Warning, "Metadata: " + returnValue.GetType().Name, "An error occurred while connecting to IGDB. WhereClause: " + WhereClause, ex);
                        returnValue = Storage.GetCacheValue<Game>(returnValue, "id", (long)searchValue);
                    }
                    return returnValue;
                case Storage.CacheStatus.Current:
                    return Storage.GetCacheValue<Game>(returnValue, "id", (long)searchValue);
                default:
                    throw new Exception("How did you get here?");
            }
        }

        private static void UpdateSubClasses(Game Game, bool getAllMetadata, bool followSubGames)
        {
            // required metadata
            if (Game.Cover != null)
            {
                Cover GameCover = Covers.GetCover(Game.Cover.Id, Config.LibraryConfiguration.LibraryMetadataDirectory_Game(Game));
            }

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
                        Artwork GameArtwork = Artworks.GetArtwork(ArtworkId, Config.LibraryConfiguration.LibraryMetadataDirectory_Game(Game));
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
                        Screenshot GameScreenshot = Screenshots.GetScreenshot(ScreenshotId, Config.LibraryConfiguration.LibraryMetadataDirectory_Game(Game));
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
            var results = await igdb.QueryAsync<Game>(IGDBClient.Endpoints.Games, query: fieldList + " " + WhereClause + ";");
            var result = results.First();

            return result;
        }

        public static Game[] SearchForGame(string SearchString, long PlatformId, SearchType searchType)
        {
            Task<Game[]> games = _SearchForGame(SearchString, PlatformId, searchType);
            return games.Result;
        }

        private static async Task<Game[]> _SearchForGame(string SearchString, long PlatformId, SearchType searchType)
        {
            string searchBody = "";
            searchBody += "fields id,name,slug,platforms,summary; ";
            switch (searchType)
            {
                case SearchType.searchNoPlatform:
                    searchBody += "search \"" + SearchString + "\"; ";
                    break;
                case SearchType.search:
                    searchBody += "search \"" + SearchString + "\"; ";
                    searchBody += "where platforms = (" + PlatformId + ");";
                    break;
                case SearchType.wherefuzzy:
                    searchBody += "where platforms = (" + PlatformId + ") & name ~ *\"" + SearchString + "\"*;";
                    break;
                case SearchType.where:
                    searchBody += "where platforms = (" + PlatformId + ") & name ~ \"" + SearchString + "\";";
                    break;
            }
            

            // get Game metadata
            var results = await igdb.QueryAsync<Game>(IGDBClient.Endpoints.Games, query: searchBody);

            return results;
        }

        public enum SearchType
        {
            where = 0,
            wherefuzzy = 1,
            search = 2,
            searchNoPlatform = 3
        }
    }
}