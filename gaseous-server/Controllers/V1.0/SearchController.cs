using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using gaseous_server.Classes;
using gaseous_server.Classes.Metadata;
using IGDB;
using IGDB.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static gaseous_server.Classes.Metadata.Games;


namespace gaseous_server.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
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
            string searchBody = "";
            string searchFields = "fields abbreviation,alternative_name,category,checksum,created_at,generation,name,platform_family,platform_logo,slug,summary,updated_at,url,versions,websites; ";
            searchBody += "where name ~ *\"" + SearchString + "\"*;";

            // get Platform metadata
            var results = await Communications.APIComm<Platform>(IGDBClient.Endpoints.Platforms, searchFields, searchBody);

            return results.ToList();
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
            string searchBody = "";
            string searchFields = "fields cover.*,first_release_date,name,platforms,slug; ";
            searchBody += "search \"" + SearchString + "\";";
            searchBody += "where platforms = (" + PlatformId + ");";

            // get Platform metadata
            var results = await Communications.APIComm<Game>(IGDBClient.Endpoints.Games, searchFields, searchBody);

            return results.ToList();
        }
    }
}

