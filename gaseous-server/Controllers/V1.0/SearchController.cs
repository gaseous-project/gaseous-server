using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using gaseous_server.Classes;
using gaseous_server.Classes.Metadata;
using gaseous_server.Models;
using HasheousClient.Models.Metadata.IGDB;
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
            string query = "SELECT `Id` FROM Platform WHERE `Name` LIKE @SearchString;";
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
        [ProducesResponseType(typeof(List<gaseous_server.Models.Game>), StatusCodes.Status200OK)]
        public async Task<ActionResult> SearchGame(long PlatformId, string SearchString)
        {
            List<gaseous_server.Models.Game> RetVal = await _SearchForGame(PlatformId, SearchString);
            return Ok(RetVal);
        }

        private static async Task<List<gaseous_server.Models.Game>> _SearchForGame(long PlatformId, string SearchString)
        {
            switch (Config.MetadataConfiguration.DefaultMetadataSource)
            {
                case FileSignature.MetadataSources.IGDB:
                    if (Config.IGDB.UseHasheousProxy == false)
                    {
                        string searchBody = "";
                        string searchFields = "fields *; ";
                        searchBody += "search \"" + SearchString + "\";";
                        searchBody += "where platforms = (" + PlatformId + ");";
                        searchBody += "limit 100;";

                        List<gaseous_server.Models.Game>? searchCache = Communications.GetSearchCache<List<gaseous_server.Models.Game>>(searchFields, searchBody);

                        if (searchCache == null)
                        {
                            // cache miss
                            // get Game metadata from data source
                            Communications comms = new Communications();
                            var results = await comms.APIComm<gaseous_server.Models.Game>("Game", searchFields, searchBody);

                            List<gaseous_server.Models.Game> games = new List<gaseous_server.Models.Game>();
                            foreach (gaseous_server.Models.Game game in results.ToList())
                            {
                                Storage.CacheStatus cacheStatus = await Storage.GetCacheStatusAsync(FileSignature.MetadataSources.IGDB, "Game", (long)game.Id);
                                switch (cacheStatus)
                                {
                                    case Storage.CacheStatus.NotPresent:
                                        await Storage.NewCacheValue(FileSignature.MetadataSources.IGDB, game, false);
                                        break;

                                    case Storage.CacheStatus.Expired:
                                        await Storage.NewCacheValue(FileSignature.MetadataSources.IGDB, game, true);
                                        break;

                                }

                                games.Add(game);
                            }

                            Communications.SetSearchCache<List<gaseous_server.Models.Game>>(searchFields, searchBody, games);

                            return games;
                        }
                        else
                        {
                            // get full version of results from database
                            // this is a hacky workaround due to the readonly nature of IGDB.Model.Game IdentityOrValue fields
                            List<gaseous_server.Models.Game> gamesToReturn = new List<gaseous_server.Models.Game>();
                            foreach (gaseous_server.Models.Game game in searchCache)
                            {
                                gaseous_server.Models.Game? tempGame = await Games.GetGame(Communications.MetadataSource, (long)game.Id);
                                if (tempGame != null)
                                {
                                    gamesToReturn.Add(tempGame);
                                }
                            }

                            return gamesToReturn;
                        }
                    }
                    else
                    {
                        HasheousClient.Hasheous hasheous = new HasheousClient.Hasheous();
                        Communications.ConfigureHasheousClient(ref hasheous);
                        List<gaseous_server.Models.Game> hSearch = hasheous.GetMetadataProxy_SearchGame<gaseous_server.Models.Game>(HasheousClient.Hasheous.MetadataProvider.IGDB, PlatformId.ToString(), SearchString).ToList<gaseous_server.Models.Game>();

                        List<gaseous_server.Models.Game> hGamesToReturn = new List<gaseous_server.Models.Game>();
                        foreach (gaseous_server.Models.Game game in hSearch)
                        {
                            hGamesToReturn.Add(game);
                        }

                        return hGamesToReturn;
                    }

                default:
                    return new List<gaseous_server.Models.Game>();
            }
        }
    }
}

