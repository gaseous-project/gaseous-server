using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Authentication;
using gaseous_server.Classes;
using gaseous_server.Classes.Metadata;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Scripting;
using static gaseous_server.Classes.Metadata.AgeRatings;
using Asp.Versioning;
using Humanizer;
using HasheousClient.Models.Metadata.IGDB;
using gaseous_server.Models;

namespace gaseous_server.Controllers.v1_1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.1")]
    [ApiController]
    [Authorize]
    public class GamesController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public GamesController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [MapToApiVersion("1.1")]
        [HttpPost]
        [ProducesResponseType(typeof(GameReturnPackage), StatusCodes.Status200OK)]
        public async Task<IActionResult> Game_v1_1(GameSearchModel model, int pageNumber = 0, int pageSize = 0, bool returnSummary = true, bool returnGames = true)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user != null)
            {
                // apply security profile filtering
                if (model.GameAgeRating == null)
                {
                    model.GameAgeRating = new GameSearchModel.GameAgeRatingItem();
                }
                if (model.GameAgeRating.AgeGroupings == null)
                {
                    model.GameAgeRating.AgeGroupings = new List<AgeGroups.AgeRestrictionGroupings>();
                }
                if (model.GameAgeRating.AgeGroupings.Count == 0)
                {
                    model.GameAgeRating.AgeGroupings.Add(AgeGroups.AgeRestrictionGroupings.Adult);
                    model.GameAgeRating.AgeGroupings.Add(AgeGroups.AgeRestrictionGroupings.Mature);
                    model.GameAgeRating.AgeGroupings.Add(AgeGroups.AgeRestrictionGroupings.Teen);
                    model.GameAgeRating.AgeGroupings.Add(AgeGroups.AgeRestrictionGroupings.Child);
                    model.GameAgeRating.IncludeUnrated = true;
                }
                List<AgeGroups.AgeRestrictionGroupings> RemoveAgeGroups = new List<AgeGroups.AgeRestrictionGroupings>();
                switch (user.SecurityProfile.AgeRestrictionPolicy.MaximumAgeRestriction)
                {
                    case AgeGroups.AgeRestrictionGroupings.Adult:
                        break;
                    case AgeGroups.AgeRestrictionGroupings.Mature:
                        RemoveAgeGroups.Add(AgeGroups.AgeRestrictionGroupings.Adult);
                        break;
                    case AgeGroups.AgeRestrictionGroupings.Teen:
                        RemoveAgeGroups.Add(AgeGroups.AgeRestrictionGroupings.Adult);
                        RemoveAgeGroups.Add(AgeGroups.AgeRestrictionGroupings.Mature);
                        break;
                    case AgeGroups.AgeRestrictionGroupings.Child:
                        RemoveAgeGroups.Add(AgeGroups.AgeRestrictionGroupings.Adult);
                        RemoveAgeGroups.Add(AgeGroups.AgeRestrictionGroupings.Mature);
                        RemoveAgeGroups.Add(AgeGroups.AgeRestrictionGroupings.Teen);
                        break;
                }
                foreach (AgeGroups.AgeRestrictionGroupings RemoveAgeGroup in RemoveAgeGroups)
                {
                    if (model.GameAgeRating.AgeGroupings.Contains(RemoveAgeGroup))
                    {
                        model.GameAgeRating.AgeGroupings.Remove(RemoveAgeGroup);
                    }
                }
                if (user.SecurityProfile.AgeRestrictionPolicy.IncludeUnrated == false)
                {
                    model.GameAgeRating.IncludeUnrated = false;
                }

                return Ok(GetGames(model, user.Id, pageNumber, pageSize, returnSummary, returnGames));
            }
            else
            {
                return Unauthorized();
            }
        }

        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("{MetadataMapId}/Related")]
        [ProducesResponseType(typeof(GameReturnPackage), StatusCodes.Status200OK)]
        public async Task<IActionResult> GameRelated(long MetadataMapId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user != null)
            {
                string IncludeUnrated = "";
                if (user.SecurityProfile.AgeRestrictionPolicy.IncludeUnrated == true)
                {
                    IncludeUnrated = " OR view_Games.AgeGroupId IS NULL";
                }

                Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
                string sql = "SELECT view_Games.Id, view_Games.AgeGroupId, Relation_Game_SimilarGames.SimilarGamesId FROM view_Games JOIN Relation_Game_SimilarGames ON view_Games.Id = Relation_Game_SimilarGames.GameId AND view_Games.GameIdType = Relation_Game_SimilarGames.GameSourceId AND Relation_Game_SimilarGames.SimilarGamesId IN (SELECT Id FROM view_Games) WHERE view_Games.Id = @id AND (view_Games.AgeGroupId <= @agegroupid" + IncludeUnrated + ")";
                Dictionary<string, object> dbDict = new Dictionary<string, object>();
                dbDict.Add("id", MetadataMapId);
                dbDict.Add("agegroupid", (int)user.SecurityProfile.AgeRestrictionPolicy.MaximumAgeRestriction);

                List<Models.Game> RetVal = new List<Models.Game>();

                DataTable dbResponse = db.ExecuteCMD(sql, dbDict);

                foreach (DataRow dr in dbResponse.Rows)
                {
                    MetadataMap.MetadataMapItem metadataMap = Classes.MetadataManagement.GetMetadataMap(MetadataMapId).PreferredMetadataMapItem;
                    RetVal.Add(Classes.Metadata.Games.GetGame(metadataMap.SourceType, (long)dr["SimilarGamesId"]));
                }

                GameReturnPackage gameReturn = new GameReturnPackage(RetVal.Count, RetVal);

                return Ok(gameReturn);
            }
            else
            {
                return Unauthorized();
            }
        }

        public class GameSearchModel
        {
            public string Name { get; set; }
            public List<string>? Platform { get; set; }
            public List<string>? Genre { get; set; }
            public List<string>? GameMode { get; set; }
            public List<string>? PlayerPerspective { get; set; }
            public List<string>? Theme { get; set; }
            public int MinimumReleaseYear { get; set; } = -1;
            public int MaximumReleaseYear { get; set; } = -1;
            public GameRatingItem? GameRating { get; set; }
            public GameAgeRatingItem? GameAgeRating { get; set; }
            public GameSortingItem Sorting { get; set; }
            public bool HasSavedGame { get; set; }
            public bool IsFavourite { get; set; }


            public class GameRatingItem
            {
                public int MinimumRating { get; set; } = -1;
                public int MinimumRatingCount { get; set; } = -1;
                public int MaximumRating { get; set; } = -1;
                public int MaximumRatingCount { get; set; } = -1;
                public bool IncludeUnrated { get; set; } = false;
            }

            public class GameAgeRatingItem
            {
                public List<AgeGroups.AgeRestrictionGroupings> AgeGroupings { get; set; } = new List<AgeGroups.AgeRestrictionGroupings>{
                    AgeGroups.AgeRestrictionGroupings.Child,
                    AgeGroups.AgeRestrictionGroupings.Teen,
                    AgeGroups.AgeRestrictionGroupings.Mature,
                    AgeGroups.AgeRestrictionGroupings.Adult
                };
                public bool IncludeUnrated { get; set; } = true;
            }

            public class GameSortingItem
            {
                public SortField SortBy { get; set; } = SortField.NameThe;
                public bool SortAscending { get; set; } = true;

                public enum SortField
                {
                    Name,
                    NameThe,
                    Rating,
                    RatingCount
                }
            }
        }

        public static GameReturnPackage GetGames(GameSearchModel model, string userid, int pageNumber = 0, int pageSize = 0, bool returnSummary = true, bool returnGames = true)
        {
            string whereClause = "";
            string havingClause = "";
            Dictionary<string, object> whereParams = new Dictionary<string, object>();
            whereParams.Add("userid", userid);

            List<string> whereClauses = new List<string>();
            List<string> havingClauses = new List<string>();

            string tempVal = "";

            string nameWhereClause = "";
            if (model.Name.Length > 0)
            {
                // tempVal = "`Name` LIKE @Name";
                // whereParams.Add("@Name", "%" + model.Name + "%");
                // havingClauses.Add(tempVal);
                nameWhereClause = "(MATCH(Game.`Name`) AGAINST (@GameName IN BOOLEAN MODE) OR MATCH(AlternativeName.`Name`) AGAINST (@GameName IN BOOLEAN MODE)) AND ";
                whereParams.Add("@GameName", "(*" + model.Name + "*) (" + model.Name + ") ");
            }

            if (model.HasSavedGame == true)
            {
                string hasSavesTemp = "(RomSavedStates.RomSaveCount IS NOT NULL OR RomGroupSavedStates.MediaGroupSaveCount IS NOT NULL)";
                whereClauses.Add(hasSavesTemp);
            }

            if (model.IsFavourite == true)
            {
                string isFavTemp = "Favourite = 1";
                havingClauses.Add(isFavTemp);
            }

            if (model.MinimumReleaseYear != -1)
            {
                string releaseTempMinVal = "FirstReleaseDate >= @minreleasedate";
                whereParams.Add("minreleasedate", new DateTime(model.MinimumReleaseYear, 1, 1));
                havingClauses.Add(releaseTempMinVal);
            }

            if (model.MaximumReleaseYear != -1)
            {
                string releaseTempMaxVal = "FirstReleaseDate <= @maxreleasedate";
                whereParams.Add("maxreleasedate", new DateTime(model.MaximumReleaseYear, 12, 31, 23, 59, 59));
                havingClauses.Add(releaseTempMaxVal);
            }

            if (model.GameRating != null)
            {
                List<string> ratingClauses = new List<string>();
                if (model.GameRating.MinimumRating != -1)
                {
                    string ratingTempMinVal = "totalRating >= @totalMinRating";
                    whereParams.Add("@totalMinRating", model.GameRating.MinimumRating);
                    ratingClauses.Add(ratingTempMinVal);
                }

                if (model.GameRating.MaximumRating != -1)
                {
                    string ratingTempMaxVal = "totalRating <= @totalMaxRating";
                    whereParams.Add("@totalMaxRating", model.GameRating.MaximumRating);
                    ratingClauses.Add(ratingTempMaxVal);
                }

                if (model.GameRating.MinimumRatingCount != -1)
                {
                    string ratingTempMinCountVal = "totalRatingCount >= @totalMinRatingCount";
                    whereParams.Add("@totalMinRatingCount", model.GameRating.MinimumRatingCount);
                    ratingClauses.Add(ratingTempMinCountVal);
                }

                if (model.GameRating.MaximumRatingCount != -1)
                {
                    string ratingTempMaxCountVal = "totalRatingCount <= @totalMaxRatingCount";
                    whereParams.Add("@totalMaxRatingCount", model.GameRating.MaximumRatingCount);
                    ratingClauses.Add(ratingTempMaxCountVal);
                }

                // generate rating sub clause
                string ratingClauseValue = "";
                if (ratingClauses.Count > 0)
                {
                    foreach (string ratingClause in ratingClauses)
                    {
                        if (ratingClauseValue.Length > 0)
                        {
                            ratingClauseValue += " AND ";
                        }
                        ratingClauseValue += ratingClause;
                    }
                }

                string unratedClause = "";
                if (model.GameRating.IncludeUnrated == true)
                {
                    unratedClause = "totalRating IS NULL";
                }

                if (ratingClauseValue.Length > 0)
                {
                    if (unratedClause.Length > 0)
                    {
                        havingClauses.Add("((" + ratingClauseValue + ") OR " + unratedClause + ")");
                    }
                    else
                    {
                        havingClauses.Add("(" + ratingClauseValue + ")");
                    }
                }
            }

            string platformWhereClause = "";
            if (model.Platform != null)
            {
                if (model.Platform.Count > 0)
                {
                    tempVal = " AND view_Games_Roms.PlatformId IN (";
                    for (int i = 0; i < model.Platform.Count; i++)
                    {
                        if (i > 0)
                        {
                            tempVal += ", ";
                        }
                        string platformLabel = "@Platform" + i;
                        tempVal += platformLabel;
                        whereParams.Add(platformLabel, model.Platform[i]);
                    }
                    tempVal += ")";
                    //whereClauses.Add(tempVal);
                    platformWhereClause = tempVal;
                }
            }

            if (model.Genre != null)
            {
                if (model.Genre.Count > 0)
                {
                    tempVal = "Genre.`Name` IN (";
                    for (int i = 0; i < model.Genre.Count; i++)
                    {
                        if (i > 0)
                        {
                            tempVal += " AND ";
                        }
                        string genreLabel = "@Genre" + i;
                        tempVal += genreLabel;
                        whereParams.Add(genreLabel, model.Genre[i]);
                    }
                    tempVal += ")";
                    whereClauses.Add(tempVal);
                }
            }

            if (model.GameMode != null)
            {
                if (model.GameMode.Count > 0)
                {
                    tempVal = "GameMode.`Name` IN (";
                    for (int i = 0; i < model.GameMode.Count; i++)
                    {
                        if (i > 0)
                        {
                            tempVal += " AND ";
                        }
                        string gameModeLabel = "@GameMode" + i;
                        tempVal += gameModeLabel;
                        whereParams.Add(gameModeLabel, model.GameMode[i]);
                    }
                    tempVal += ")";
                    whereClauses.Add(tempVal);
                }
            }

            if (model.PlayerPerspective != null)
            {
                if (model.PlayerPerspective.Count > 0)
                {
                    tempVal = "PlayerPerspective.`Name` IN (";
                    for (int i = 0; i < model.PlayerPerspective.Count; i++)
                    {
                        if (i > 0)
                        {
                            tempVal += " AND ";
                        }
                        string playerPerspectiveLabel = "@PlayerPerspective" + i;
                        tempVal += playerPerspectiveLabel;
                        whereParams.Add(playerPerspectiveLabel, model.PlayerPerspective[i]);
                    }
                    tempVal += ")";
                    whereClauses.Add(tempVal);
                }
            }

            if (model.Theme != null)
            {
                if (model.Theme.Count > 0)
                {
                    tempVal = "Theme.`Name` IN (";
                    for (int i = 0; i < model.Theme.Count; i++)
                    {
                        if (i > 0)
                        {
                            tempVal += " AND ";
                        }
                        string themeLabel = "@Theme" + i;
                        tempVal += themeLabel;
                        whereParams.Add(themeLabel, model.Theme[i]);
                    }
                    tempVal += ")";
                    whereClauses.Add(tempVal);
                }
            }

            if (model.GameAgeRating != null)
            {
                if (model.GameAgeRating.AgeGroupings.Count > 0)
                {
                    tempVal = "(Game.AgeGroupId IN (";
                    for (int i = 0; i < model.GameAgeRating.AgeGroupings.Count; i++)
                    {
                        if (i > 0)
                        {
                            tempVal += ", ";
                        }
                        string themeLabel = "@Rating" + i;
                        tempVal += themeLabel;
                        whereParams.Add(themeLabel, model.GameAgeRating.AgeGroupings[i]);
                    }
                    tempVal += ")";

                    if (model.GameAgeRating.IncludeUnrated == true)
                    {
                        tempVal += " OR Game.AgeGroupId IS NULL";
                    }
                    tempVal += ")";

                    whereClauses.Add(tempVal);
                }
            }

            // build where clause
            if (whereClauses.Count > 0)
            {
                whereClause = "WHERE ";
                for (int i = 0; i < whereClauses.Count; i++)
                {
                    if (i > 0)
                    {
                        whereClause += " AND ";
                    }
                    whereClause += whereClauses[i];
                }
            }

            // build having clause
            if (havingClauses.Count > 0)
            {
                havingClause = "HAVING ";
                for (int i = 0; i < havingClauses.Count; i++)
                {
                    if (i > 0)
                    {
                        havingClause += " AND ";
                    }
                    havingClause += havingClauses[i];
                }
            }

            // order by clause
            string orderByField = "NameThe";
            string orderByOrder = "ASC";
            if (model.Sorting != null)
            {
                switch (model.Sorting.SortBy)
                {
                    case GameSearchModel.GameSortingItem.SortField.NameThe:
                        orderByField = "NameThe";
                        break;
                    case GameSearchModel.GameSortingItem.SortField.Name:
                        orderByField = "Name";
                        break;
                    case GameSearchModel.GameSortingItem.SortField.Rating:
                        orderByField = "TotalRating";
                        break;
                    case GameSearchModel.GameSortingItem.SortField.RatingCount:
                        orderByField = "TotalRatingCount";
                        break;
                    default:
                        orderByField = "NameThe";
                        break;
                }

                if (model.Sorting.SortAscending == true)
                {
                    orderByOrder = "ASC";
                }
                else
                {
                    orderByOrder = "DESC";
                }
            }
            string orderByClause = "ORDER BY `" + orderByField + "` " + orderByOrder;

            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);

            string sql = @"
SET SESSION sql_mode=(SELECT REPLACE(@@sql_mode,'ONLY_FULL_GROUP_BY',''));
SELECT
    Game.Id,
    Game.MetadataMapId,
    Game.GameIdType,
    Game.`Name`,
    Game.NameThe,
    Game.Slug,
    Game.Summary,
    Game.PlatformId,
    Game.TotalRating,
    Game.TotalRatingCount,
    Game.Cover,
    Game.Artworks,
    Game.FirstReleaseDate,
    Game.Category,
    Game.ParentGame,
    Game.AgeRatings,
    Game.AgeGroupId,
    Game.Genres,
    Game.GameModes,
    Game.PlayerPerspectives,
    Game.Themes,
    CONCAT('[', GROUP_CONCAT(DISTINCT MetadataMap.PlatformId ORDER BY MetadataMap.PlatformId SEPARATOR ','), ']') AS Platforms,
    Game.RomCount,
    RomSavedStates.RomSaveCount,
    RomGroupSavedStates.MediaGroupSaveCount,
    CASE
        WHEN Favourites.UserId IS NULL THEN 0
        ELSE 1
    END AS Favourite
FROM
    (SELECT DISTINCT
        Game.*,
            view_Games_Roms.PlatformId,
            AgeGroup.AgeGroupId
    FROM
        view_GamesWithRoms AS Game
    LEFT JOIN AgeGroup ON Game.Id = AgeGroup.GameId
    JOIN view_Games_Roms ON Game.Id = view_Games_Roms.GameId" + platformWhereClause + @"
    LEFT JOIN AlternativeName ON Game.Id = AlternativeName.Game WHERE " + nameWhereClause + @"
    Game.RomCount > 0) Game
        LEFT JOIN
    (SELECT 
        view_Games_Roms.GameId, COUNT(GameState.Id) AS RomSaveCount
    FROM
        GameState
    JOIN view_Games_Roms ON GameState.RomId = view_Games_Roms.Id
    WHERE
        GameState.IsMediaGroup = 0
            AND GameState.UserId = @userid
    GROUP BY view_Games_Roms.GameId) RomSavedStates ON Game.Id = RomSavedStates.GameId
        LEFT JOIN
    (SELECT 
        RomMediaGroup.GameId,
            COUNT(RomMediaGroup.GameId) AS MediaGroupSaveCount
    FROM
        RomMediaGroup
    JOIN GameState ON RomMediaGroup.Id = GameState.RomId
        AND GameState.IsMediaGroup = 1
        AND GameState.UserId = @userid
    GROUP BY RomMediaGroup.GameId) RomGroupSavedStates ON Game.MetadataMapId = RomGroupSavedStates.GameId
        JOIN
	MetadataMapBridge ON Game.Id = MetadataMapBridge.MetadataSourceId AND MetadataMapBridge.Preferred = 1
		JOIN
	MetadataMap ON MetadataMapBridge.ParentMapId = MetadataMap.Id
LEFT JOIN
    Relation_Game_Genres ON Game.Id = Relation_Game_Genres.GameId AND Relation_Game_Genres.GameSourceId = Game.GameIdType
		JOIN
	Genre ON Relation_Game_Genres.GenresId = Genre.Id AND Relation_Game_Genres.GameSourceId = Genre.SourceId
        LEFT JOIN
    Relation_Game_GameModes ON Game.Id = Relation_Game_GameModes.GameId AND Relation_Game_GameModes.GameSourceId = Game.GameIdType
		LEFT JOIN
	GameMode ON Relation_Game_GameModes.GameModesId = GameMode.Id AND Relation_Game_GameModes.GameSourceId = GameMode.SourceId
        LEFT JOIN
    Relation_Game_PlayerPerspectives ON Game.Id = Relation_Game_PlayerPerspectives.GameId AND Relation_Game_PlayerPerspectives.GameSourceId = Game.GameIdType
		LEFT JOIN
	PlayerPerspective ON Relation_Game_PlayerPerspectives.PlayerPerspectivesId = PlayerPerspective.Id AND Relation_Game_PlayerPerspectives.GameSourceId = PlayerPerspective.SourceId
        LEFT JOIN
    Relation_Game_Themes ON Game.Id = Relation_Game_Themes.GameId AND Relation_Game_Themes.GameSourceId = Game.GameIdType
		LEFT JOIN
	Theme ON Relation_Game_Themes.ThemesId = Theme.Id AND Relation_Game_Themes.GameSourceId = Theme.SourceId
        LEFT JOIN
    Favourites ON Game.MetadataMapId = Favourites.GameId AND Favourites.UserId = @userid " + whereClause + " GROUP BY Game.Id " + havingClause + " " + orderByClause;

            string limiter = "";
            if (returnGames == true)
            {
                limiter += " LIMIT @pageOffset, @pageSize";
                whereParams.Add("pageOffset", pageSize * (pageNumber - 1));
                whereParams.Add("pageSize", pageSize);
            }

            DataTable dbResponse = db.ExecuteCMD(sql + limiter, whereParams, new Database.DatabaseMemoryCacheOptions(CacheEnabled: true, ExpirationSeconds: 60));

            // get count
            int? RecordCount = null;
            if (returnSummary == true)
            {
                RecordCount = dbResponse.Rows.Count;
            }

            int indexInPage = 0;
            if (pageNumber > 1)
            {
                indexInPage = pageSize * (pageNumber - 1);
            }

            // compile data for return
            List<Games.MinimalGameItem>? RetVal = null;
            if (returnGames == true)
            {
                RetVal = new List<Games.MinimalGameItem>();
                foreach (int i in Enumerable.Range(0, dbResponse.Rows.Count))
                {
                    Models.Game retGame = Storage.BuildCacheObject<Models.Game>(new Models.Game(), dbResponse.Rows[i]);
                    retGame.MetadataMapId = (long)dbResponse.Rows[i]["MetadataMapId"];
                    retGame.MetadataSource = (HasheousClient.Models.MetadataSources)dbResponse.Rows[i]["GameIdType"];

                    Games.MinimalGameItem retMinGame = new Games.MinimalGameItem(retGame);
                    retMinGame.Index = indexInPage;
                    indexInPage += 1;
                    if (dbResponse.Rows[i]["RomSaveCount"] != DBNull.Value || dbResponse.Rows[i]["MediaGroupSaveCount"] != DBNull.Value)
                    {
                        retMinGame.HasSavedGame = true;
                    }
                    else
                    {
                        retMinGame.HasSavedGame = false;
                    }
                    if ((int)dbResponse.Rows[i]["Favourite"] == 0)
                    {
                        retMinGame.IsFavourite = false;
                    }
                    else
                    {
                        retMinGame.IsFavourite = true;
                    }

                    RetVal.Add(retMinGame);
                }
            }

            Dictionary<string, GameReturnPackage.AlphaListItem>? AlphaList = null;
            if (returnSummary == true)
            {
                dbResponse = db.ExecuteCMD(sql, whereParams, new Database.DatabaseMemoryCacheOptions(CacheEnabled: true, ExpirationSeconds: 60));

                RecordCount = dbResponse.Rows.Count;

                AlphaList = new Dictionary<string, GameReturnPackage.AlphaListItem>();

                // build alpha list
                if (orderByField == "NameThe" || orderByField == "Name")
                {
                    int CurrentPage = 1;
                    int NextPageIndex = pageSize;

                    string alphaSearchField;
                    if (orderByField == "NameThe")
                    {
                        alphaSearchField = "NameThe";
                    }
                    else
                    {
                        alphaSearchField = "Name";
                    }

                    for (int i = 0; i < dbResponse.Rows.Count; i++)
                    {
                        if (NextPageIndex == i + 1)
                        {
                            NextPageIndex += pageSize;
                            CurrentPage += 1;
                        }

                        string firstChar = dbResponse.Rows[i][alphaSearchField].ToString().Substring(0, 1).ToUpperInvariant();
                        if ("ABCDEFGHIJKLMNOPQRSTUVWXYZ".Contains(firstChar))
                        {
                            if (!AlphaList.ContainsKey(firstChar))
                            {
                                AlphaList.Add(firstChar, new GameReturnPackage.AlphaListItem
                                {
                                    Index = i,
                                    Page = CurrentPage
                                });
                            }
                        }
                        else
                        {
                            if (!AlphaList.ContainsKey("#"))
                            {
                                AlphaList.Add("#", new GameReturnPackage.AlphaListItem
                                {
                                    Index = i,
                                    Page = 1
                                });
                            }
                        }
                    }
                }
            }

            GameReturnPackage gameReturn = new GameReturnPackage
            {
                Count = RecordCount,
                Games = RetVal,
                AlphaList = AlphaList
            };

            return gameReturn;
        }

        public class GameReturnPackage
        {
            public GameReturnPackage()
            {

            }

            public GameReturnPackage(int Count, List<Models.Game> Games)
            {
                this.Count = Count;

                List<Games.MinimalGameItem> minimalGames = new List<Games.MinimalGameItem>();
                foreach (Models.Game game in Games)
                {
                    minimalGames.Add(new Classes.Metadata.Games.MinimalGameItem(game));
                }

                this.Games = minimalGames;
            }

            public int? Count { get; set; }
            public List<Games.MinimalGameItem>? Games { get; set; } = new List<Games.MinimalGameItem>();
            public Dictionary<string, AlphaListItem>? AlphaList { get; set; }
            public class AlphaListItem
            {
                public int Index { get; set; }
                public int Page { get; set; }
            }
        }
    }
}