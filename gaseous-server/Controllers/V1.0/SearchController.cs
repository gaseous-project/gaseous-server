using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using gaseous_server.Classes;
using gaseous_server.Classes.Metadata;
using gaseous_server.Models;
using IGDB;
using IGDB.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NuGet.Common;
using static gaseous_server.Classes.Metadata.Games;
using Asp.Versioning;

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
            // search the database for the requested platforms
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string query = "SELECT `Id` FROM Platform WHERE `Name` LIKE '%" + SearchString + "%';";
            DataTable data = db.ExecuteCMD(query);

            List<Platform> platforms = new List<Platform>();
            foreach (DataRow row in data.Rows)
            {
                Platform platform = Platforms.GetPlatform((long)row["Id"], false, false);

                platforms.Add(platform);
            }

            return platforms;
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("Game")]
        [ProducesResponseType(typeof(List<GaseousGame>), StatusCodes.Status200OK)]
        public async Task<ActionResult> SearchGame(long PlatformId, string SearchString)
        {
            List<GaseousGame> RetVal = await _SearchForGame(PlatformId, SearchString);
            return Ok(RetVal);
        }

        private static async Task<List<GaseousGame>> _SearchForGame(long PlatformId, string SearchString)
        {
            switch (Communications.MetadataSource)
            {
                case HasheousClient.Models.MetadataModel.MetadataSources.IGDB:
                    string searchBody = "";
                    string searchFields = "fields *; ";
                    searchBody += "search \"" + SearchString + "\";";
                    searchBody += "where platforms = (" + PlatformId + ");";
                    searchBody += "limit 100;";

                    List<GaseousGame>? searchCache = Communications.GetSearchCache<List<GaseousGame>>(searchFields, searchBody);

                    if (searchCache == null)
                    {
                        // cache miss
                        // get Game metadata from data source
                        Communications comms = new Communications();
                        var results = await comms.APIComm<Game>(IGDBClient.Endpoints.Games, searchFields, searchBody);

                        List<GaseousGame> games = new List<GaseousGame>();
                        foreach (Game game in results.ToList())
                        {
                            Storage.CacheStatus cacheStatus = Storage.GetCacheStatus("Game", (long)game.Id);
                            switch (cacheStatus)
                            {
                                case Storage.CacheStatus.NotPresent:
                                    Storage.NewCacheValue(game, false);
                                    break;

                                case Storage.CacheStatus.Expired:
                                    Storage.NewCacheValue(game, true);
                                    break;

                            }

                            games.Add(new GaseousGame(game));
                        }

                        Communications.SetSearchCache<List<GaseousGame>>(searchFields, searchBody, games);

                        return games;
                    }
                    else
                    {
                        // get full version of results from database
                        // this is a hacky workaround due to the readonly nature of IGDB.Model.Game IdentityOrValue fields
                        List<GaseousGame> gamesToReturn = new List<GaseousGame>();
                        foreach (GaseousGame game in searchCache)
                        {
                            Game tempGame = Games.GetGame((long)game.Id, false, false, false);
                            gamesToReturn.Add(new GaseousGame(tempGame));
                        }

                        return gamesToReturn;
                    }

                case HasheousClient.Models.MetadataModel.MetadataSources.Hasheous:
                    HasheousClient.Hasheous hasheous = new HasheousClient.Hasheous();
                    Communications.ConfigureHasheousClient(ref hasheous);
                    List<HasheousClient.Models.Metadata.IGDB.Game> hSearch = hasheous.GetMetadataProxy_SearchGame<HasheousClient.Models.Metadata.IGDB.Game>(HasheousClient.Hasheous.MetadataProvider.IGDB, PlatformId.ToString(), SearchString).ToList<HasheousClient.Models.Metadata.IGDB.Game>();

                    List<GaseousGame> hGamesToReturn = new List<GaseousGame>();
                    foreach (HasheousClient.Models.Metadata.IGDB.Game game in hSearch)
                    {
                        IGDB.Models.Game tempGame = Communications.ConvertToIGDBModel<IGDB.Models.Game>(game);
                        hGamesToReturn.Add(new GaseousGame(tempGame));
                    }

                    return hGamesToReturn;

                default:
                    return new List<GaseousGame>();
            }
        }
    }
}

