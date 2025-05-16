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
                if (model.GameAgeRating.IncludeUnrated == false)
                {
                    if (model.GameAgeRating.AgeGroupings.Count == 0)
                    {
                        model.GameAgeRating.AgeGroupings.Add(AgeGroups.AgeRestrictionGroupings.Adult);
                        model.GameAgeRating.AgeGroupings.Add(AgeGroups.AgeRestrictionGroupings.Mature);
                        model.GameAgeRating.AgeGroupings.Add(AgeGroups.AgeRestrictionGroupings.Teen);
                        model.GameAgeRating.AgeGroupings.Add(AgeGroups.AgeRestrictionGroupings.Child);
                        model.GameAgeRating.IncludeUnrated = true;
                    }
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

                return Ok(await GetGames(model, user, pageNumber, pageSize, returnSummary, returnGames));
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

                DataTable dbResponse = await db.ExecuteCMDAsync(sql, dbDict);

                foreach (DataRow dr in dbResponse.Rows)
                {
                    MetadataMap.MetadataMapItem metadataMap = (await Classes.MetadataManagement.GetMetadataMap(MetadataMapId)).PreferredMetadataMapItem;
                    RetVal.Add(await Classes.Metadata.Games.GetGame(metadataMap.SourceType, (long)dr["SimilarGamesId"]));
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
            public int MinPlayTime { get; set; } = -1;
            public int MaxPlayTime { get; set; } = -1;


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
                    RatingCount,
                    DateAdded,
                    LastPlayed,
                    TimePlayed
                }
            }
        }

        public static async Task<GameReturnPackage> GetGames(GameSearchModel model, ApplicationUser? user, int pageNumber = 0, int pageSize = 0, bool returnSummary = true, bool returnGames = true)
        {
            string whereClause = "";
            string havingClause = "";
            Dictionary<string, object> whereParams = new Dictionary<string, object>();
            whereParams.Add("userid", user.Id);

            List<string> joinClauses = new List<string>();
            string joinClauseTemplate = "LEFT JOIN `Relation_Game_<Datatype>s` ON `Game`.`Id` = `Relation_Game_<Datatype>s`.`GameId` AND `Relation_Game_<Datatype>s`.`GameSourceId` = `Game`.`SourceId` LEFT JOIN `<Datatype>` ON `Relation_Game_<Datatype>s`.`<Datatype>sId` = `<Datatype>`.`Id`  AND `Relation_Game_<Datatype>s`.`GameSourceId` = `<Datatype>`.`SourceId`";
            List<string> whereClauses = new List<string>();
            List<string> havingClauses = new List<string>();

            string tempVal = "";

            string nameWhereClause = "";
            if (model.Name.Length > 0)
            {
                whereClauses.Add("(MATCH(`Game`.`Name`) AGAINST (@GameName IN BOOLEAN MODE) OR MATCH(`AlternativeName`.`Name`) AGAINST (@GameName IN BOOLEAN MODE))");
                whereParams.Add("@GameName", "(*" + model.Name + "*) (" + model.Name + ") ");
            }

            if (model.HasSavedGame == true)
            {
                string hasSavesTemp = "(RomSavedStates > 0 OR RomGroupSavedStates > 0 OR RomSavedFiles > 0 OR RomGroupSavedFiles > 0)";
                havingClauses.Add(hasSavesTemp);
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

            if (model.MinPlayTime != -1)
            {
                string playTimeTempMinVal = "TimePlayed >= @minplaytime";
                whereParams.Add("minplaytime", model.MinPlayTime);
                havingClauses.Add(playTimeTempMinVal);
            }

            if (model.MaxPlayTime != -1)
            {
                string playTimeTempMaxVal = "TimePlayed <= @maxplaytime";
                whereParams.Add("maxplaytime", model.MaxPlayTime);
                havingClauses.Add(playTimeTempMaxVal);
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

            if (model.Platform != null)
            {
                if (model.Platform.Count > 0)
                {
                    tempVal = "`MetadataMap`.`PlatformId` IN (";
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
                    whereClauses.Add(tempVal);
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

                    joinClauses.Add(joinClauseTemplate.Replace("<Datatype>", "Genre"));
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

                    joinClauses.Add(joinClauseTemplate.Replace("<Datatype>", "GameMode"));
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

                    joinClauses.Add(joinClauseTemplate.Replace("<Datatype>", "PlayerPerspective"));
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

                    joinClauses.Add(joinClauseTemplate.Replace("<Datatype>", "Theme"));
                }
            }

            string gameAgeRatingString = "(";
            if (model.GameAgeRating != null)
            {
                if (model.GameAgeRating.AgeGroupings.Count > 0)
                {
                    tempVal = "AgeGroup.AgeGroupId IN (";
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
                }

                if (model.GameAgeRating.IncludeUnrated == true)
                {
                    if (tempVal.Length > 0)
                    {
                        tempVal += " OR ";
                    }
                    tempVal += "AgeGroup.AgeGroupId IS NULL";
                }
                gameAgeRatingString += tempVal + ")";
                whereClauses.Add(gameAgeRatingString);
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
                    case GameSearchModel.GameSortingItem.SortField.DateAdded:
                        orderByField = "DateAdded";
                        break;
                    case GameSearchModel.GameSortingItem.SortField.LastPlayed:
                        orderByField = "LastPlayed";
                        break;
                    case GameSearchModel.GameSortingItem.SortField.TimePlayed:
                        orderByField = "TimePlayed";
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
                SELECT 
	`MetadataMapBridge`.`MetadataSourceId` AS `Id`,
    `MetadataMap`.`Id` AS `MetadataMapId`,
    `MetadataMapBridge`.`MetadataSourceType` AS `GameIdType`,
    `MetadataMap`.`SignatureGameName`,
    CONCAT('[',
            GROUP_CONCAT(DISTINCT `MetadataMap`.`PlatformId`
                ORDER BY `MetadataMap`.`PlatformId`
                SEPARATOR ','),
            ']') AS `Platforms`,
    COUNT(`Games_Roms`.`Id`) AS `RomCount`,
    CASE
        WHEN `RomMediaGroup`.`Id` IS NULL THEN 0
        ELSE 1
    END AS `MediaGroups`,
    CASE
        WHEN `Favourites`.`UserId` IS NULL THEN 0
        ELSE 1
    END AS `Favourite`,
    COUNT(`RomSavedState`.`Id`) AS `RomSavedStates`,
    COUNT(`RomGroupSavedState`.`Id`) AS `RomGroupSavedStates`,
    COUNT(`RomSavedFile`.`Id`) AS `RomSavedFiles`,
    COUNT(`RomGroupSavedFile`.`Id`) AS `RomGroupSavedFiles`,
    `AgeGroup`.`AgeGroupId`,
    CASE
        WHEN `LocalizedNames`.`LocalizedName` IS NOT NULL THEN `LocalizedNames`.`LocalizedName`
        WHEN `Game`.`Name` IS NULL THEN `MetadataMap`.`SignatureGameName`
        ELSE `Game`.`Name`
    END AS `Name`,
    CASE
		WHEN `LocalizedNames`.`LocalizedNameThe` IS NOT NULL THEN `LocalizedNames`.`LocalizedNameThe`
        WHEN `Game`.`Name` IS NULL THEN
            CASE
                WHEN
                    `MetadataMap`.`SignatureGameName` LIKE 'The %'
                THEN
                    CONCAT(TRIM(SUBSTR(`MetadataMap`.`SignatureGameName`,
                                    4)),
                            ', The')
                ELSE `MetadataMap`.`SignatureGameName`
            END
        WHEN `Game`.`Name` LIKE 'The %' THEN CONCAT(TRIM(SUBSTR(`Game`.`Name`, 4)), ', The')
        ELSE `Game`.`Name`
    END AS `NameThe`,
    `Game`.`Slug`,
    `Game`.`Summary`,
    `Game`.`TotalRating`,
    `Game`.`TotalRatingCount`,
    CASE
        WHEN `LocalizedNames`.`LocalizedCover` IS NULL THEN `Game`.`Cover`
        WHEN `LocalizedNames`.`LocalizedCover` = 0 THEN `Game`.`Cover`
        ELSE `LocalizedNames`.`LocalizedCover`
    END AS `Cover`,
    `Game`.`Artworks`,
    `Game`.`FirstReleaseDate`,
    `Game`.`Category`,
    `Game`.`ParentGame`,
    `Game`.`AgeRatings`,
    `Game`.`Genres`,
    `Game`.`GameModes`,
    `Game`.`PlayerPerspectives`,
    `Game`.`Themes`,
    MIN(`Games_Roms`.`DateCreated`) AS `DateAdded`,
    MAX(`Games_Roms`.`DateUpdated`) AS `DateUpdated`,
    SUM(`UserTimeTracking`.`SessionLength`) AS `TimePlayed`,
    MAX(`UserTimeTracking`.`SessionTime`) AS `LastPlayed`
FROM
    `MetadataMap`
        LEFT JOIN
    `MetadataMapBridge` ON (`MetadataMap`.`Id` = `MetadataMapBridge`.`ParentMapId`
        AND `MetadataMapBridge`.`Preferred` = 1)
        JOIN
    `Games_Roms` ON `MetadataMap`.`Id` = `Games_Roms`.`MetadataMapId`
        LEFT JOIN
    `UserTimeTracking` ON `MetadataMap`.`Id` = `UserTimeTracking`.`GameId`
        AND `MetadataMap`.`PlatformId` = `UserTimeTracking`.`PlatformId`
        AND `UserTimeTracking`.`UserId` = @userid
        LEFT JOIN
    `Favourites` ON `MetadataMapBridge`.`ParentMapId` = `Favourites`.`GameId`
        AND `Favourites`.`UserId` = @userid
        LEFT JOIN
    `GameState` AS `RomSavedState` ON `Games_Roms`.`Id` = `RomSavedState`.`RomId`
        AND `RomSavedState`.`IsMediaGroup` = 0
        AND `RomSavedState`.`UserId` = @userid
        LEFT JOIN
    `GameSaves` AS `RomSavedFile` ON `Games_Roms`.`Id` = `RomSavedFile`.`RomId`
        AND `RomSavedFile`.`IsMediaGroup` = 0
        AND `RomSavedFile`.`UserId` = @userid
        LEFT JOIN
    `RomMediaGroup` ON `MetadataMap`.`Id` = `RomMediaGroup`.`GameId`
        AND `MetadataMap`.`PlatformId` = `RomMediaGroup`.`PlatformId`
        LEFT JOIN
    `GameState` AS `RomGroupSavedState` ON `RomMediaGroup`.`Id` = `RomGroupSavedState`.`RomId`
        AND `RomGroupSavedState`.`IsMediaGroup` = 1
        AND `RomGroupSavedState`.`UserId` = @userid
        LEFT JOIN
    `GameSaves` AS `RomGroupSavedFile` ON `RomMediaGroup`.`Id` = `RomGroupSavedFile`.`RomId`
        AND `RomGroupSavedFile`.`IsMediaGroup` = 1
        AND `RomGroupSavedFile`.`UserId` = @userid
        LEFT JOIN
    `Game` ON `MetadataMapBridge`.`MetadataSourceType` = `Game`.`SourceId`
        AND `MetadataMapBridge`.`MetadataSourceId` = `Game`.`Id`
        LEFT JOIN
	`AgeGroup` ON `Game`.`Id` = `AgeGroup`.`GameId` AND `Game`.`SourceId` = `AgeGroup`.`SourceId`
        LEFT JOIN
    `AlternativeName` ON `Game`.`Id` = `AlternativeName`.`Game` AND `Game`.`SourceId` = `AlternativeName`.`SourceId`
        LEFT JOIN
    (SELECT 
        `GameLocalization`.`Game`,
            `GameLocalization`.`SourceId`,
            `GameLocalization`.`Name` AS `LocalizedName`,
            CASE
                WHEN `GameLocalization`.`Name` LIKE 'The %' THEN CONCAT(TRIM(SUBSTR(`GameLocalization`.`Name`, 4)), ', The')
                ELSE `GameLocalization`.`Name`
            END AS `LocalizedNameThe`,
            `GameLocalization`.`Cover` AS `LocalizedCover`,
            `Region`.`Identifier`
    FROM
        `GameLocalization`
    JOIN `Region` ON `GameLocalization`.`Region` = `Region`.`Id`
        AND `GameLocalization`.`SourceId` = `Region`.`SourceId`
    WHERE
        `Region`.`Identifier` = @lang) `LocalizedNames` ON `Game`.`Id` = `LocalizedNames`.`Game`
        AND `Game`.`SourceId` = `LocalizedNames`.`SourceId`
" + String.Join(" ", joinClauses) + " " + whereClause + " GROUP BY `MetadataMapBridge`.`MetadataSourceId` " + havingClause + " " + orderByClause;

            string limiter = "";
            if (returnGames == true)
            {
                limiter += " LIMIT @pageOffset, @pageSize";
                whereParams.Add("pageOffset", pageSize * (pageNumber - 1));
                whereParams.Add("pageSize", pageSize);
            }

            string? userLocale = user.UserPreferences?.Find(x => x.Setting == "User.Locale")?.Value;
            if (userLocale != null)
            {
                // userLocale is in a serliazed format, so we need to deserialize it - but since it's the only thing, we can simply remove the quotes
                userLocale = userLocale.Replace("\"", "");
                whereParams["lang"] = userLocale;
            }
            else
            {
                whereParams["lang"] = "";
            }

            DataTable dbResponse = await db.ExecuteCMDAsync(sql + limiter, whereParams, new Database.DatabaseMemoryCacheOptions(CacheEnabled: true, ExpirationSeconds: 60));

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
                    if (
                        dbResponse.Rows[i]["RomSavedStates"] != DBNull.Value ||
                        dbResponse.Rows[i]["RomGroupSavedStates"] != DBNull.Value ||
                        dbResponse.Rows[i]["RomSavedFiles"] != DBNull.Value ||
                        dbResponse.Rows[i]["RomGroupSavedFiles"] != DBNull.Value
                        )
                    {
                        if (
                            (long)dbResponse.Rows[i]["RomSavedStates"] >= 1 ||
                            (long)dbResponse.Rows[i]["RomGroupSavedStates"] >= 1 ||
                            (long)dbResponse.Rows[i]["RomSavedFiles"] >= 1 ||
                            (long)dbResponse.Rows[i]["RomGroupSavedFiles"] >= 1
                            )
                        {
                            retMinGame.HasSavedGame = true;
                        }
                        else
                        {
                            retMinGame.HasSavedGame = false;
                        }
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
                dbResponse = await db.ExecuteCMDAsync(sql, whereParams, new Database.DatabaseMemoryCacheOptions(CacheEnabled: true, ExpirationSeconds: 60));

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

                        string gameName = dbResponse.Rows[i][alphaSearchField].ToString();
                        if (gameName.Length == 0)
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
                        else
                        {
                            string firstChar = gameName.Substring(0, 1).ToUpperInvariant();
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