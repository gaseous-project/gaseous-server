using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using gaseous_server.Classes;
using gaseous_server.Classes.Metadata;
using gaseous_server.Models;
using gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NuGet.Common;
using static gaseous_server.Classes.Metadata.Games;
using Asp.Versioning;

namespace gaseous_server.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0", Deprecated = true)]
    [ApiVersion("1.1")]
    [Authorize]
    public class SearchController : Controller
    {
        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("Platform")]
        [ProducesResponseType(typeof(List<Platform>), StatusCodes.Status200OK)]
        public async Task<ActionResult> SearchPlatform(string SearchString)
        {
            List<Platform> RetVal = await _SearchForPlatform(SearchString);
            return Ok(RetVal);
        }

        private static async Task<List<Platform>> _SearchForPlatform(string SearchString)
        {
            // search the database for the requested platforms
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string query = "SELECT `Id` FROM Metadata_Platform WHERE `Name` LIKE @SearchString;";
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("SearchString", "%" + SearchString + "%");
            DataTable data = await db.ExecuteCMDAsync(query, parameters);

            List<Platform> platforms = new List<Platform>();
            foreach (DataRow row in data.Rows)
            {
                Platform platform = await Platforms.GetPlatform((long)row["Id"]);

                platforms.Add(platform);
            }

            return platforms;
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("Game")]
        [ProducesResponseType(typeof(List<Game>), StatusCodes.Status200OK)]
        public async Task<ActionResult> SearchGame(long PlatformId, string SearchString)
        {
            List<Game> RetVal = await _SearchForGame(PlatformId, SearchString);
            return Ok(RetVal);
        }

        private static async Task<List<Game>> _SearchForGame(long PlatformId, string SearchString)
        {
            switch (Config.MetadataConfiguration.DefaultMetadataSource)
            {
                case FileSignature.MetadataSources.IGDB:
                    string searchBody = "";
                    string searchFields = "fields *; ";
                    searchBody += "search \"" + SearchString + "\";";
                    searchBody += "where platforms = (" + PlatformId + ");";
                    searchBody += "limit 100;";

                    var results = await Metadata.SearchGamesAsync(SearchType.search, PlatformId, new List<string> { SearchString });

                    return results.ToList();

                default:
                    return new List<Game>();
            }
        }
    }
}

