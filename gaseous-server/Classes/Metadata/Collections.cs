using System;
using IGDB;
using IGDB.Models;


namespace gaseous_server.Classes.Metadata
{
    public class Collections
    {
        public const string fieldList = "fields as_child_relations,as_parent_relations,checksum,created_at,games,name,slug,type,updated_at,url;";

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
                Task<Collection> RetVal = _GetCollections((long)Id);
                return RetVal.Result;
            }
        }

        private static async Task<Collection> _GetCollections(long searchValue)
        {
            // check database first
            Storage.CacheStatus? cacheStatus = Storage.GetCacheStatus("Collection", searchValue);

            Collection returnValue = new Collection();
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

        private static async Task<Collection> GetObjectFromServer(long searchValue)
        {
            // get Collections metadata
            Communications comms = new Communications();
            var results = await comms.APIComm<Collection>(Communications.MetadataEndpoint.Collection, searchValue);
            var result = results.First();

            return result;
        }
    }
}

