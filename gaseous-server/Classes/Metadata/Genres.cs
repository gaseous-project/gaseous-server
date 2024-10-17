using System;
using IGDB;
using IGDB.Models;


namespace gaseous_server.Classes.Metadata
{
	public class Genres
    {
        public const string fieldList = "fields checksum,created_at,name,slug,updated_at,url;";

        public Genres()
        {
        }

        public static Genre? GetGenres(long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Task<Genre> RetVal = _GetGenres((long)Id);
                return RetVal.Result;
            }
        }

        private static async Task<Genre> _GetGenres(long searchValue)
        {
            // check database first
            Storage.CacheStatus? cacheStatus  = Storage.GetCacheStatus("Genre", searchValue);

            Genre returnValue = new Genre();
            switch (cacheStatus)
            {
                case Storage.CacheStatus.NotPresent:
                    returnValue = await GetObjectFromServer(searchValue);
                    Storage.NewCacheValue(returnValue);
                    break;  
                case Storage.CacheStatus.Expired:
                    try
                    {
                        returnValue = await GetObjectFromServer(searchValue);
                        Storage.NewCacheValue(returnValue, true);
                    }
                    catch (Exception ex)
                    {
                        Logging.Log(Logging.LogType.Warning, "Metadata: " + returnValue.GetType().Name, "An error occurred while connecting to IGDB. Id: " + searchValue, ex);
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

        private static async Task<Genre> GetObjectFromServer(long searchValue)
        {
            // get Genres metadata
            Communications comms = new Communications();
            var results = await comms.APIComm<Genre>(Communications.MetadataEndpoint.Genre, searchValue);
            var result = results.First();

            return result;
        }
	}
}

