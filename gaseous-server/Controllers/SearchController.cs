﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using gaseous_server.Classes;
using IGDB;
using IGDB.Models;
using Microsoft.AspNetCore.Mvc;
using static gaseous_server.Classes.Metadata.Games;


namespace gaseous_server.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    public class SearchController : Controller
    {
        private static IGDBClient igdb = new IGDBClient(
                            // Found in Twitch Developer portal for your app
                            Config.IGDB.ClientId,
                            Config.IGDB.Secret
                        );

        [MapToApiVersion("1.0")]
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
            searchBody += "fields abbreviation,alternative_name,category,checksum,created_at,generation,name,platform_family,platform_logo,slug,summary,updated_at,url,versions,websites; ";
            searchBody += "where name ~ *\"" + SearchString + "\"*;";

            // get Platform metadata
            var results = await igdb.QueryAsync<Platform>(IGDBClient.Endpoints.Platforms, query: searchBody);

            return results.ToList();
        }

        [MapToApiVersion("1.0")]
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
            searchBody += "fields cover.*,first_release_date,name,platforms,slug; ";
            searchBody += "search \"" + SearchString + "\";";
            searchBody += "where platforms = (" + PlatformId + ");";

            // get Platform metadata
            var results = await igdb.QueryAsync<Game>(IGDBClient.Endpoints.Games, query: searchBody);

            return results.ToList();
        }
    }
}

