using System;
using IGDB;
using IGDB.Models;


namespace gaseous_server.Classes.Metadata
{
    public class PlayerPerspectives
    {
        public const string fieldList = "fields checksum,created_at,name,slug,updated_at,url;";

        public PlayerPerspectives()
        {
        }

        public static PlayerPerspective? GetGame_PlayerPerspectives(long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Task<PlayerPerspective> RetVal = _GetGame_PlayerPerspectives((long)Id);
                return RetVal.Result;
            }
        }

        private static async Task<PlayerPerspective> _GetGame_PlayerPerspectives(long searchValue)
        {
            // check database first
            Storage.CacheStatus? cacheStatus = Storage.GetCacheStatus("PlayerPerspective", searchValue);

            PlayerPerspective returnValue = new PlayerPerspective();
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
                    }
                    catch (Exception ex)
                    {
                        Logging.Log(Logging.LogType.Warning, "Metadata: " + returnValue.GetType().Name, "An error occurred while connecting to IGDB. Id: " + searchValue, ex);
                        returnValue = Storage.GetCacheValue<PlayerPerspective>(returnValue, "id", (long)searchValue);
                    }
                    break;
                case Storage.CacheStatus.Current:
                    returnValue = Storage.GetCacheValue<PlayerPerspective>(returnValue, "id", (long)searchValue);
                    break;
                default:
                    throw new Exception("How did you get here?");
            }

            return returnValue;
        }

        private static async Task<PlayerPerspective> GetObjectFromServer(long searchValue)
        {
            // get Game_PlayerPerspectives metadata
            Communications comms = new Communications();
            var results = await comms.APIComm<PlayerPerspective>(Communications.MetadataEndpoint.PlayerPerspective, searchValue);
            var result = results.First();

            return result;
        }
    }
}

