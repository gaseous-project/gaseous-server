using System;
using IGDB;
using IGDB.Models;


namespace gaseous_server.Classes.Metadata
{
    public class AlternativeNames
    {
        public const string fieldList = "fields checksum,comment,game,name;";

        public AlternativeNames()
        {
        }

        public static AlternativeName? GetAlternativeNames(long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Task<AlternativeName> RetVal = _GetAlternativeNames((long)Id);
                return RetVal.Result;
            }
        }

        private static async Task<AlternativeName> _GetAlternativeNames(long searchValue)
        {
            // check database first
            Storage.CacheStatus? cacheStatus = Storage.GetCacheStatus("AlternativeName", searchValue);

            AlternativeName returnValue = new AlternativeName();
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

        private static async Task<AlternativeName> GetObjectFromServer(long searchValue)
        {
            // get AlternativeNames metadata
            Communications comms = new Communications();
            var results = await comms.APIComm<AlternativeName>(Communications.MetadataEndpoint.AlternativeName, searchValue);
            var result = results.First();

            return result;
        }
    }
}

