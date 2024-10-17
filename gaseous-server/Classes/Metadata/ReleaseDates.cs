using System;
using IGDB;
using IGDB.Models;


namespace gaseous_server.Classes.Metadata
{
    public class ReleaseDates
    {
        public const string fieldList = "fields category,checksum,created_at,date,game,human,m,platform,region,status,updated_at,y;";

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
                Task<ReleaseDate> RetVal = _GetReleaseDates((long)Id);
                return RetVal.Result;
            }
        }

        private static async Task<ReleaseDate> _GetReleaseDates(long searchValue)
        {
            // check database first
            Storage.CacheStatus? cacheStatus = Storage.GetCacheStatus("ReleaseDate", searchValue);

            ReleaseDate returnValue = new ReleaseDate();
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

        private static async Task<ReleaseDate> GetObjectFromServer(long searchValue)
        {
            // get ReleaseDates metadata
            Communications comms = new Communications();
            var results = await comms.APIComm<ReleaseDate>(Communications.MetadataEndpoint.ReleaseDate, searchValue);
            var result = results.First();

            return result;
        }
    }
}

