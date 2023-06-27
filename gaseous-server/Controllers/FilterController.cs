using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using gaseous_tools;
using IGDB.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace gaseous_server.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class FilterController : ControllerBase
    {
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ResponseCache(CacheProfileName = "5Minute")]
        public Dictionary<string, object> Filter()
        {
            Database db = new gaseous_tools.Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);

            Dictionary<string, object> FilterSet = new Dictionary<string, object>();

            // platforms
            List<Platform> platforms = new List<Platform>();
            string sql = "SELECT platform.id, platform.abbreviation, platform.alternativename, platform.`name`, platform.platformlogo, (SELECT COUNT(games_roms.id) AS RomCount FROM games_roms WHERE games_roms.platformid = platform.id) AS RomCount FROM platform HAVING RomCount > 0 ORDER BY `name`";
            DataTable dbResponse = db.ExecuteCMD(sql);

            foreach (DataRow dr in dbResponse.Rows)
            {
                platforms.Add(Classes.Metadata.Platforms.GetPlatform((long)dr["id"]));
            }
            FilterSet.Add("platforms", platforms);

            // genres
            List<Genre> genres = new List<Genre>();
            sql = "SELECT DISTINCT t1.id, t1.`name` FROM genre AS t1 JOIN (SELECT * FROM game WHERE (SELECT COUNT(id) FROM games_roms WHERE gameid = game.id) > 0) AS t2 ON JSON_CONTAINS(t2.genres, CAST(t1.id AS char), '$') ORDER BY t1.`name`";
            dbResponse = db.ExecuteCMD(sql);

            foreach (DataRow dr in dbResponse.Rows)
            {
                genres.Add(Classes.Metadata.Genres.GetGenres((long)dr["id"]));
            }
            FilterSet.Add("genres", genres);

            return FilterSet;
        }
    }
}