using System;
using IGDB;
using IGDB.Models;


namespace gaseous_server.Classes.Metadata
{
    public class GameModes
    {
        public const string fieldList = "fields checksum,created_at,name,slug,updated_at,url;";

        public GameModes()
        {
        }

        public static GameMode? GetGame_Modes(long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Task<GameMode> RetVal = _GetGame_Modes((long)Id);
                return RetVal.Result;
            }
        }

        private static async Task<GameMode> _GetGame_Modes(long searchValue)
        {
            // check database first
            Storage.CacheStatus? cacheStatus = Storage.GetCacheStatus("GameMode", searchValue);

            GameMode returnValue = new GameMode();
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
                        returnValue = Storage.GetCacheValue<GameMode>(returnValue, "id", (long)searchValue);
                    }
                    break;
                case Storage.CacheStatus.Current:
                    returnValue = Storage.GetCacheValue<GameMode>(returnValue, "id", (long)searchValue);
                    break;
                default:
                    throw new Exception("How did you get here?");
            }

            return returnValue;
        }

        private static async Task<GameMode> GetObjectFromServer(long searchValue)
        {
            // get Game_Modes metadata
            Communications comms = new Communications();
            var results = await comms.APIComm<GameMode>(Communications.MetadataEndpoint.GameMode, searchValue);
            var result = results.First();

            return result;
        }
    }
}

