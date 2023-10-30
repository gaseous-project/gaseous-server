using System;
using IGDB;
using IGDB.Models;


namespace gaseous_server.Classes.Metadata
{
	public class AlternativeNames
    {
        const string fieldList = "fields checksum,comment,game,name;";

        public AlternativeNames()
        {
        }

        private static IGDBClient igdb = new IGDBClient(
                    // Found in Twitch Developer portal for your app
                    Config.IGDB.ClientId,
                    Config.IGDB.Secret
                );

        public static AlternativeName? GetAlternativeNames(long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Task<AlternativeName> RetVal = _GetAlternativeNames(SearchUsing.id, Id);
                return RetVal.Result;
            }
        }

        public static AlternativeName GetAlternativeNames(string Slug)
        {
            Task<AlternativeName> RetVal = _GetAlternativeNames(SearchUsing.slug, Slug);
            return RetVal.Result;
        }

        private static async Task<AlternativeName> _GetAlternativeNames(SearchUsing searchUsing, object searchValue)
        {
            // check database first
            Storage.CacheStatus? cacheStatus = new Storage.CacheStatus();
            if (searchUsing == SearchUsing.id)
            {
                cacheStatus = Storage.GetCacheStatus("AlternativeName", (long)searchValue);
            }
            else
            {
                cacheStatus = Storage.GetCacheStatus("AlternativeName", (string)searchValue);
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

            AlternativeName returnValue = new AlternativeName();
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
                        Logging.Log(Logging.LogType.Warning, "Metadata: " + returnValue.GetType().Name, "An error occurred while connecting to IGDB. WhereClause: " + WhereClause, ex);
                        returnValue = Storage.GetCacheValue<AlternativeName>(returnValue, "id", (long)searchValue);
                    }
                    break;
                case Storage.CacheStatus.Current:
                    returnValue = Storage.GetCacheValue<AlternativeName>(returnValue, "id", (long)searchValue);
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

        private static async Task<AlternativeName> GetObjectFromServer(string WhereClause)
        {
            // get AlternativeNames metadata
            var results = await igdb.QueryAsync<AlternativeName>(IGDBClient.Endpoints.AlternativeNames, query: fieldList + " " + WhereClause + ";");
            var result = results.First();

            return result;
        }
	}
}

