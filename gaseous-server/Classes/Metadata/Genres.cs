using System;
using gaseous_tools;
using IGDB;
using IGDB.Models;
using static gaseous_tools.Config.ConfigFile;

namespace gaseous_server.Classes.Metadata
{
	public class Genres
    {
        const string fieldList = "fields checksum,created_at,name,slug,updated_at,url;";

        public Genres()
        {
        }

        private static IGDBClient igdb = new IGDBClient(
                    // Found in Twitch Developer portal for your app
                    Config.IGDB.ClientId,
                    Config.IGDB.Secret
                );

        public static Genre? GetGenres(long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Task<Genre> RetVal = _GetGenres(SearchUsing.id, Id);
                return RetVal.Result;
            }
        }

        public static Genre GetGenres(string Slug)
        {
            Task<Genre> RetVal = _GetGenres(SearchUsing.slug, Slug);
            return RetVal.Result;
        }

        private static async Task<Genre> _GetGenres(SearchUsing searchUsing, object searchValue)
        {
            // check database first
            Storage.CacheStatus? cacheStatus = new Storage.CacheStatus();
            if (searchUsing == SearchUsing.id)
            {
                cacheStatus = Storage.GetCacheStatus("Genre", (long)searchValue);
            }
            else
            {
                cacheStatus = Storage.GetCacheStatus("Genre", (string)searchValue);
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

            Genre returnValue = new Genre();
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
                        returnValue = Storage.GetCacheValue<Genre>(returnValue, "id", (long)searchValue);
                    }
                    break;
                case Storage.CacheStatus.Current:
                    returnValue = Storage.GetCacheValue<Genre>(returnValue, "id", (long)searchValue);
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

        private static async Task<Genre> GetObjectFromServer(string WhereClause)
        {
            // get Genres metadata
            var results = await igdb.QueryAsync<Genre>(IGDBClient.Endpoints.Genres, query: fieldList + " " + WhereClause + ";");
            var result = results.First();

            return result;
        }
	}
}

