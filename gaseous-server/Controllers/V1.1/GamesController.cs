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
        [ProducesResponseType(typeof(List<Game>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Game_v1_1(GameSearchModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user != null)
            {
                // apply security profile filtering
                List<string> RemoveAgeGroups = new List<string>();
                switch (user.SecurityProfile.AgeRestrictionPolicy.MaximumAgeRestriction.ToLower())
                {
                    case "adult":
                        break;
                    case "mature":
                        RemoveAgeGroups.Add("Adult");
                        break;
                    case "teen":
                        RemoveAgeGroups.Add("Adult");
                        RemoveAgeGroups.Add("Mature");
                        break;
                    case "child":
                        RemoveAgeGroups.Add("Adult");
                        RemoveAgeGroups.Add("Mature");
                        RemoveAgeGroups.Add("Teen");
                        break;
                }
                foreach (string RemoveAgeGroup in RemoveAgeGroups)
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

                return Ok(GetGames(model));
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
                public List<string> AgeGroupings { get; set; } = new List<string>{ "Child", "Teen", "Mature", "Adult" };
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
    
        public static List<Game> GetGames(GameSearchModel model)
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

                string unratedClause = "totalRating IS NOT NULL";
                if (model.GameRating.IncludeUnrated == true)
                {
                    unratedClause = "totalRating IS NULL";
                }

                if (ratingClauseValue.Length > 0)
                {
                    havingClauses.Add("((" + ratingClauseValue + ") OR " + unratedClause + ")");
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
                    List<long> AgeClassificationsList = new List<long>();
                    foreach (string ratingGroup in model.GameAgeRating.AgeGroupings)
                    {
                        if (AgeGroups.AgeGroupings.ContainsKey(ratingGroup))
                        {
                            List<AgeGroups.AgeGroupItem> ageGroups = AgeGroups.AgeGroupings[ratingGroup];
                            foreach (AgeGroups.AgeGroupItem ageGroup in ageGroups)
                            {
                                AgeClassificationsList.AddRange(ageGroup.AgeGroupItemValues);
                            }
                        }
                    }

                    if (AgeClassificationsList.Count > 0)
                    {
                        AgeClassificationsList = new HashSet<long>(AgeClassificationsList).ToList();
                        tempVal = "(view_AgeRatings.Rating IN (";
                        for (int i = 0; i < AgeClassificationsList.Count; i++)
                        {
                            if (i > 0)
                            {
                                tempVal += ", ";
                            }
                            string themeLabel = "@Rating" + i;
                            tempVal += themeLabel;
                            whereParams.Add(themeLabel, AgeClassificationsList[i]);
                        }
                        tempVal += ")";

                        if (model.GameAgeRating.IncludeUnrated == true)
                        {
                            tempVal += " OR view_AgeRatings.Rating IS NULL";
                        }
                        tempVal += ")";

                        whereClauses.Add(tempVal);
                    }
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
            string sql = "SELECT DISTINCT Games_Roms.GameId AS ROMGameId, Game.*, case when Game.`Name` like 'The %' then CONCAT(trim(substr(Game.`Name` from 4)), ', The') else Game.`Name` end as NameThe FROM Games_Roms LEFT JOIN Game ON Game.Id = Games_Roms.GameId LEFT JOIN Relation_Game_Genres ON Game.Id = Relation_Game_Genres.GameId LEFT JOIN Relation_Game_GameModes ON Game.Id = Relation_Game_GameModes.GameId LEFT JOIN Relation_Game_PlayerPerspectives ON Game.Id = Relation_Game_PlayerPerspectives.GameId LEFT JOIN Relation_Game_Themes ON Game.Id = Relation_Game_Themes.GameId LEFT JOIN (SELECT Relation_Game_AgeRatings.GameId, AgeRating.* FROM Relation_Game_AgeRatings JOIN AgeRating ON Relation_Game_AgeRatings.AgeRatingsId = AgeRating.Id) view_AgeRatings ON Game.Id = view_AgeRatings.GameId " + whereClause + " " + havingClause + " " + orderByClause;

            List<IGDB.Models.Game> RetVal = new List<IGDB.Models.Game>();

            DataTable dbResponse = db.ExecuteCMD(sql, whereParams);
            foreach (DataRow dr in dbResponse.Rows)
            {
                //RetVal.Add(Classes.Metadata.Games.GetGame((long)dr["ROMGameId"], false, false));
                RetVal.Add(Classes.Metadata.Games.GetGame(dr));
            }

            return RetVal;
        }
    }
}