using System;
using IGDB;
using IGDB.Models;


namespace gaseous_server.Classes.Metadata
{
	public class ReleaseDates
    {
        const string fieldList = "fields category,checksum,created_at,date,game,human,m,platform,region,status,updated_at,y;";

        public ReleaseDates()
        {
        }

        public static ReleaseDate? GetReleaseDates(long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Task<ReleaseDate> RetVal = _GetReleaseDates(SearchUsing.id, Id);
                return RetVal.Result;
            }
        }

        public static ReleaseDate GetReleaseDates(string Slug)
        {
            Task<ReleaseDate> RetVal = _GetReleaseDates(SearchUsing.slug, Slug);
            return RetVal.Result;
        }

        private static async Task<ReleaseDate> _GetReleaseDates(SearchUsing searchUsing, object searchValue)
        {
            // check database first
            Storage.CacheStatus? cacheStatus = new Storage.CacheStatus();
            if (searchUsing == SearchUsing.id)
            {
                cacheStatus = Storage.GetCacheStatus("ReleaseDate", (long)searchValue);
            }
            else
            {
                cacheStatus = Storage.GetCacheStatus("ReleaseDate", (string)searchValue);
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

            ReleaseDate returnValue = new ReleaseDate();
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
                        returnValue = Storage.GetCacheValue<ReleaseDate>(returnValue, "id", (long)searchValue);
                    }
                    break;
                case Storage.CacheStatus.Current:
                    returnValue = Storage.GetCacheValue<ReleaseDate>(returnValue, "id", (long)searchValue);
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

        private static async Task<ReleaseDate> GetObjectFromServer(string WhereClause)
        {
            // get ReleaseDates metadata
            Communications comms = new Communications();
            var results = await comms.APIComm<ReleaseDate>(IGDBClient.Endpoints.ReleaseDates, fieldList, WhereClause);
            var result = results.First();

            return result;
        }
	}
}

