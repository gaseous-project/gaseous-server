using System;
using IGDB;
using IGDB.Models;


namespace gaseous_server.Classes.Metadata
{
    public class Themes
    {
        public const string fieldList = "fields checksum,created_at,name,slug,updated_at,url;";

        public Themes()
        {
        }

        public static Theme? GetGame_Themes(long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Task<Theme> RetVal = _GetGame_Themes((long)Id);
                return RetVal.Result;
            }
        }

        private static async Task<Theme> _GetGame_Themes(long searchValue)
        {
            // check database first
            Storage.CacheStatus? cacheStatus = Storage.GetCacheStatus("Theme", searchValue);

            Theme returnValue = new Theme();
            bool forceImageDownload = false;
            switch (cacheStatus)
            {
                case Storage.CacheStatus.NotPresent:
                    returnValue = await GetObjectFromServer(searchValue);
                    Storage.NewCacheValue(returnValue);
                    forceImageDownload = true;
                    break;
                case Storage.CacheStatus.Expired:
                    try
                    {
                        returnValue = await GetObjectFromServer(searchValue);
                        Storage.NewCacheValue(returnValue, true);
                        return returnValue;
                    }
                    catch (Exception ex)
                    {
                        Logging.Log(Logging.LogType.Warning, "Metadata: " + returnValue.GetType().Name, "An error occurred while connecting to IGDB. Id: " + searchValue, ex);
                        return Storage.GetCacheValue<Theme>(returnValue, "id", (long)searchValue);
                    }
                case Storage.CacheStatus.Current:
                    returnValue = Storage.GetCacheValue<Theme>(returnValue, "id", (long)searchValue);
                    break;
                default:
                    throw new Exception("How did you get here?");
            }

            return returnValue;
        }

        private static async Task<Theme> GetObjectFromServer(long searchValue)
        {
            // get Game_Themes metadata
            Communications comms = new Communications();
            var results = await comms.APIComm<Theme>(Communications.MetadataEndpoint.Theme, searchValue);
            var result = results.First();

            return result;
        }
    }
}

