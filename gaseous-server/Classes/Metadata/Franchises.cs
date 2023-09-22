using System;
using gaseous_tools;
using IGDB;
using IGDB.Models;
using MySqlX.XDevAPI.Common;
using static gaseous_tools.Config.ConfigFile;

namespace gaseous_server.Classes.Metadata
{
	public class Franchises
    {
        const string fieldList = "fields checksum,created_at,games,name,slug,updated_at,url;";

        public Franchises()
        {
        }

        private static IGDBClient igdb = new IGDBClient(
                    // Found in Twitch Developer portal for your app
                    Config.IGDB.ClientId,
                    Config.IGDB.Secret
                );

        public static Franchise? GetFranchises(long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Task<Franchise> RetVal = _GetFranchises(SearchUsing.id, Id);
                return RetVal.Result;
            }
        }

        public static Franchise GetFranchises(string Slug)
        {
            Task<Franchise> RetVal = _GetFranchises(SearchUsing.slug, Slug);
            return RetVal.Result;
        }

        private static async Task<Franchise> _GetFranchises(SearchUsing searchUsing, object searchValue)
        {
            // check database first
            Storage.CacheStatus? cacheStatus = new Storage.CacheStatus();
            if (searchUsing == SearchUsing.id)
            {
                cacheStatus = Storage.GetCacheStatus("Franchise", (long)searchValue);
            }
            else
            {
                cacheStatus = Storage.GetCacheStatus("Franchise", (string)searchValue);
            }

            // set up where clause
            string WhereClause = "";
            switch (searchUsing)
            {
                case SearchUsing.id:
                    WhereClause = "where id = " + searchValue;
                    break;
                case SearchUsing.slug:
                    WhereClause = "where slug = " + searchValue;
                    break;
                default:
                    throw new Exception("Invalid search type");
            }

            Franchise returnValue = new Franchise();
            switch (cacheStatus)
            {
                case Storage.CacheStatus.NotPresent:
                    returnValue = await GetObjectFromServer(WhereClause);
                    Storage.NewCacheValue(returnValue);
                    break;  
                case Storage.CacheStatus.Expired:
                    try
                    {
                        returnValue = await GetObjectFromServer(WhereClause);
                        Storage.NewCacheValue(returnValue, true);
                    }
                    catch (Exception ex)
                    {
                        gaseous_tools.Logging.Log(gaseous_tools.Logging.LogType.Warning, "Metadata: " + returnValue.GetType().Name, "An error occurred while connecting to IGDB. WhereClause: " + WhereClause, ex);
                        returnValue = Storage.GetCacheValue<Franchise>(returnValue, "id", (long)searchValue);
                    }
                    break;
                case Storage.CacheStatus.Current:
                    returnValue = Storage.GetCacheValue<Franchise>(returnValue, "id", (long)searchValue);
                    break;
                default:
                    throw new Exception("How did you get here?");
            }

            return returnValue;
        }

        private enum SearchUsing
        {
            id,
            slug
        }

        private static async Task<Franchise> GetObjectFromServer(string WhereClause)
        {
            // get FranchiseContentDescriptions metadata
            var results = await igdb.QueryAsync<Franchise>(IGDBClient.Endpoints.Franchies, query: fieldList + " " + WhereClause + ";");
            var result = results.First();

            return result;
        }
	}
}

