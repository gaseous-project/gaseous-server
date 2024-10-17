using System;
using IGDB;
using IGDB.Models;


namespace gaseous_server.Classes.Metadata
{
    public class MultiplayerModes
    {
        public const string fieldList = "fields campaigncoop,checksum,dropin,game,lancoop,offlinecoop,offlinecoopmax,offlinemax,onlinecoop,onlinecoopmax,onlinemax,platform,splitscreen,splitscreenonline;";

        public MultiplayerModes()
        {
        }

        public static MultiplayerMode? GetGame_MultiplayerModes(long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Task<MultiplayerMode> RetVal = _GetGame_MultiplayerModes((long)Id);
                return RetVal.Result;
            }
        }

        private static async Task<MultiplayerMode> _GetGame_MultiplayerModes(long searchValue)
        {
            // check database first
            Storage.CacheStatus? cacheStatus = Storage.GetCacheStatus("MultiplayerMode", searchValue);

            MultiplayerMode returnValue = new MultiplayerMode();
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
                        returnValue = Storage.GetCacheValue<MultiplayerMode>(returnValue, "id", (long)searchValue);
                    }
                    break;
                case Storage.CacheStatus.Current:
                    returnValue = Storage.GetCacheValue<MultiplayerMode>(returnValue, "id", (long)searchValue);
                    break;
                default:
                    throw new Exception("How did you get here?");
            }

            return returnValue;
        }

        private static async Task<MultiplayerMode> GetObjectFromServer(long searchValue)
        {
            // get Game_MultiplayerModes metadata
            Communications comms = new Communications();
            var results = await comms.APIComm<MultiplayerMode>(Communications.MetadataEndpoint.MultiplayerMode, searchValue);
            var result = results.First();

            return result;
        }
    }
}

