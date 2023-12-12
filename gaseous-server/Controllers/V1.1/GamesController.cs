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
using IGDB.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Scripting;
using static gaseous_server.Classes.Metadata.AgeRatings;

namespace gaseous_server.Controllers.v1_1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.1")]
    [ApiController]
    [Authorize]
    public class GamesController: ControllerBase
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
        public async Task<IActionResult> Game_v1_1(GameSearchModel model, int pageNumber = 0, int pageSize = 0)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user != null)
            {
                // apply security profile filtering
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

                return Ok(GetGames(model, pageNumber, pageSize));
            }
            else
            {
                return Unauthorized();
            }
        }

        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("{GameId}/Related")]
        [ProducesResponseType(typeof(GameReturnPackage), StatusCodes.Status200OK)]
        public async Task<IActionResult> GameRelated(long GameId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user != null)
            {
                string IncludeUnrated = "";
                if (user.SecurityProfile.AgeRestrictionPolicy.IncludeUnrated == true) {
                    IncludeUnrated = " OR view_Games.AgeGroupId IS NULL";
                }

                Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
                string sql = "SELECT view_Games.Id, view_Games.AgeGroupId, Relation_Game_SimilarGames.SimilarGamesId FROM view_Games JOIN Relation_Game_SimilarGames ON view_Games.Id = Relation_Game_SimilarGames.GameId AND Relation_Game_SimilarGames.SimilarGamesId IN (SELECT Id FROM view_Games) WHERE view_Games.Id = @id AND (view_Games.AgeGroupId <= @agegroupid" + IncludeUnrated + ")";
                Dictionary<string, object> dbDict = new Dictionary<string, object>();
                dbDict.Add("id", GameId);
                dbDict.Add("agegroupid", (int)user.SecurityProfile.AgeRestrictionPolicy.MaximumAgeRestriction);

                List<IGDB.Models.Game> RetVal = new List<IGDB.Models.Game>();

                DataTable dbResponse = db.ExecuteCMD(sql, dbDict);

                foreach (DataRow dr in dbResponse.Rows)
                {
                    RetVal.Add(Classes.Metadata.Games.GetGame((long)dr["SimilarGamesId"], false, false, false));
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
            public List<string> Platform { get; set; }
            public List<string> Genre { get; set; }
            public List<string> GameMode { get; set; }
            public List<string> PlayerPerspective { get; set; }
            public List<string> Theme { get; set; }
            public GameRatingItem GameRating { get; set; } = new GameRatingItem();
            public GameAgeRatingItem GameAgeRating { get; set; } = new GameAgeRatingItem();
            public GameSortingItem Sorting { get; set; } = new GameSortingItem();
            

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
    
        public static GameReturnPackage GetGames(GameSearchModel model, int pageNumber = 0, int pageSize = 0)
        {
            string whereClause = "";
            string havingClause = "";
            Dictionary<string, object> whereParams = new Dictionary<string, object>();

            List<string> whereClauses = new List<string>();
            List<string> havingClauses = new List<string>();

            string tempVal = "";

            if (model.Name.Length > 0)
            {
                tempVal = "`Name` LIKE @Name";
                whereParams.Add("@Name", "%" + model.Name + "%");
                havingClauses.Add(tempVal);
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

            if (model.Platform.Count > 0)
            {
                tempVal = "Games_Roms.PlatformId IN (";
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

            if (model.Genre.Count > 0)
            {
                tempVal = "Relation_Game_Genres.GenresId IN (";
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

            if (model.GameMode.Count > 0)
            {
                tempVal = "Relation_Game_GameModes.GameModesId IN (";
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

            if (model.PlayerPerspective.Count > 0)
            {
                tempVal = "Relation_Game_PlayerPerspectives.PlayerPerspectivesId IN (";
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

            if (model.Theme.Count > 0)
            {
                tempVal = "Relation_Game_Themes.ThemesId IN (";
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

            if (model.GameAgeRating != null)
            {
                if (model.GameAgeRating.AgeGroupings.Count > 0)
                {
                    tempVal = "(AgeGroupId IN (";
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
                        tempVal += " OR AgeGroupId IS NULL";
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
                switch(model.Sorting.SortBy)
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
            string sql = "SELECT DISTINCT view_Games.* FROM view_Games LEFT JOIN Games_Roms ON view_Games.Id = Games_Roms.GameId LEFT JOIN Relation_Game_Genres ON view_Games.Id = Relation_Game_Genres.GameId LEFT JOIN Relation_Game_GameModes ON view_Games.Id = Relation_Game_GameModes.GameId LEFT JOIN Relation_Game_PlayerPerspectives ON view_Games.Id = Relation_Game_PlayerPerspectives.GameId LEFT JOIN Relation_Game_Themes ON view_Games.Id = Relation_Game_Themes.GameId " + whereClause + " " + havingClause + " " + orderByClause;

            List<IGDB.Models.Game> RetVal = new List<IGDB.Models.Game>();

            DataTable dbResponse = db.ExecuteCMD(sql, whereParams);

            // get count
            int RecordCount = dbResponse.Rows.Count;
            
            // compile data for return
            int pageOffset = pageSize * (pageNumber - 1);
            for (int i = pageOffset; i < dbResponse.Rows.Count; i++)
            {
                if (i >= (pageOffset + pageSize))
                {
                    break;
                }

                RetVal.Add(Classes.Metadata.Games.GetGame(dbResponse.Rows[i]));
            }

            GameReturnPackage gameReturn = new GameReturnPackage(RecordCount, RetVal);

            return gameReturn;
        }

        public class GameReturnPackage
        {
            public GameReturnPackage()
            {

            }

            public GameReturnPackage(int Count, List<Game> Games)
            {
                this.Count = Count;

                List<Games.MinimalGameItem> minimalGames = new List<Games.MinimalGameItem>();
                foreach (Game game in Games)
                {
                    minimalGames.Add(new Classes.Metadata.Games.MinimalGameItem(game));
                }

                this.Games = minimalGames;
            }

            public int Count { get; set; }
            public List<Games.MinimalGameItem> Games { get; set; } = new List<Games.MinimalGameItem>();
        }
    }
}