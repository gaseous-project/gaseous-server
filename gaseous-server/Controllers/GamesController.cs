using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using gaseous_tools;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace gaseous_server.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class GamesController : ControllerBase
    {
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public List<IGDB.Models.Game> Game(string name = "", string platform = "")
        {
            string whereClause = "";
            string havingClause = "";
            Dictionary<string, object> whereParams = new Dictionary<string, object>();

            List<string> whereClauses = new List<string>();
            List<string> havingClauses = new List<string>();

            string tempVal = "";

            if (name.Length > 0)
            {
                tempVal = "`name` LIKE @name";
                whereParams.Add("@name", "%" + name + "%");
                havingClauses.Add(tempVal);
            }

            if (platform.Length > 0)
            {
                tempVal = "games_roms.platformid IN (";
                string[] platformClauseItems = platform.Split(",");
                for (int i = 0; i < platformClauseItems.Length; i++)
                {
                    if (i > 0)
                    {
                        tempVal += ", ";
                    }
                    string platformLabel = "@platform" + i;
                    tempVal += platformLabel;
                    whereParams.Add(platformLabel, platformClauseItems[i]);
                }
                tempVal += ")";
                whereClauses.Add(tempVal);
            }

            // build where clause
            if (whereClauses.Count > 0)
            {
                whereClause = "WHERE ";
                for (int i = 0; i < whereClauses.Count; i++)
                {
                    if (i > 0)
                    {
                        whereClause += ", ";
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
                        havingClause += ", ";
                    }
                    havingClause += havingClauses[i];
                }
            }

            Database db = new gaseous_tools.Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "SELECT DISTINCT games_roms.gameid AS ROMGameId, game.ageratings, game.aggregatedrating, game.aggregatedratingcount, game.alternativenames, game.artworks, game.bundles, game.category, game.collection, game.cover, game.dlcs, game.expansions, game.externalgames, game.firstreleasedate, game.`follows`, game.franchise, game.franchises, game.gameengines, game.gamemodes, game.genres, game.hypes, game.involvedcompanies, game.keywords, game.multiplayermodes, (CASE WHEN games_roms.gameid = 0 THEN games_roms.`name` ELSE game.`name` END) AS `name`, game.parentgame, game.platforms, game.playerperspectives, game.rating, game.ratingcount, game.releasedates, game.screenshots, game.similargames, game.slug, game.standaloneexpansions, game.`status`, game.storyline, game.summary, game.tags, game.themes, game.totalrating, game.totalratingcount, game.versionparent, game.versiontitle, game.videos, game.websites FROM gaseous.games_roms LEFT JOIN game ON game.id = games_roms.gameid " + whereClause + " " + havingClause + " ORDER BY `name`";

            List<IGDB.Models.Game> RetVal = new List<IGDB.Models.Game>();

            DataTable dbResponse = db.ExecuteCMD(sql, whereParams);
            foreach (DataRow dr in dbResponse.Rows)
            {
                if ((long)dr["ROMGameId"] == 0)
                {
                    // unknown game
                }
                else
                {
                    // known game
                    RetVal.Add(Classes.Metadata.Games.GetGame((long)dr["ROMGameId"], false, false));
                }
            }

            return RetVal;
        }
    }
}
