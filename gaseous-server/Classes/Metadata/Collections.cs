using System;
using IGDB;
using IGDB.Models;


namespace gaseous_server.Classes.Metadata
{
	public class Collections
    {
        const string fieldList = "fields checksum,created_at,games,name,slug,updated_at,url;";

        public Collections()
        {
        }

        public static Collection? GetCollections(long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Task<Collection> RetVal = _GetCollections(SearchUsing.id, Id);
                return RetVal.Result;
            }
        }

        public static Collection GetCollections(string Slug)
        {
            Task<Collection> RetVal = _GetCollections(SearchUsing.slug, Slug);
            return RetVal.Result;
        }

        private static async Task<Collection> _GetCollections(SearchUsing searchUsing, object searchValue)
        {
            // check database first
            Storage.CacheStatus? cacheStatus = new Storage.CacheStatus();
            if (searchUsing == SearchUsing.id)
            {
                cacheStatus = Storage.GetCacheStatus("Collection", (long)searchValue);
            }
            else
            {
                cacheStatus = Storage.GetCacheStatus("Collection", (string)searchValue);
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

            Collection returnValue = new Collection();
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
                        returnValue = Storage.GetCacheValue<Collection>(returnValue, "id", (long)searchValue);
                    }
                    break;
                case Storage.CacheStatus.Current:
                    returnValue = Storage.GetCacheValue<Collection>(returnValue, "id", (long)searchValue);
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

        private static async Task<Collection> GetObjectFromServer(string WhereClause)
        {
            // get Collections metadata
            Communications comms = new Communications();
            var results = await comms.APIComm<Collection>(IGDBClient.Endpoints.Collections, fieldList, WhereClause);
            var result = results.First();

            return result;
        }
	}
}

