using System;
using IGDB;
using IGDB.Models;


namespace gaseous_server.Classes.Metadata
{
    public class Franchises
    {
        public const string fieldList = "fields checksum,created_at,games,name,slug,updated_at,url;";

        public Franchises()
        {
        }

        public static Franchise? GetFranchises(long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Task<Franchise> RetVal = _GetFranchises((long)Id);
                return RetVal.Result;
            }
        }

        private static async Task<Franchise> _GetFranchises(long searchValue)
        {
            // check database first
            Storage.CacheStatus? cacheStatus = Storage.GetCacheStatus("Franchise", searchValue);

            Franchise returnValue = new Franchise();
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

        private static async Task<Franchise> GetObjectFromServer(long searchValue)
        {
            // get FranchiseContentDescriptions metadata
            Communications comms = new Communications();
            var results = await comms.APIComm<Franchise>(Communications.MetadataEndpoint.Franchise, searchValue);
            var result = results.First();

            return result;
        }
    }
}

