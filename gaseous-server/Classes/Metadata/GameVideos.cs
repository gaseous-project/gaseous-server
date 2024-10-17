using System;
using IGDB;
using IGDB.Models;


namespace gaseous_server.Classes.Metadata
{
    public class GamesVideos
    {
        public const string fieldList = "fields checksum,game,name,video_id;";

        public GamesVideos()
        {
        }

        public static GameVideo? GetGame_Videos(long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Task<GameVideo> RetVal = _GetGame_Videos((long)Id);
                return RetVal.Result;
            }
        }

        private static async Task<GameVideo> _GetGame_Videos(long searchValue)
        {
            // check database first
            Storage.CacheStatus? cacheStatus = Storage.GetCacheStatus("GameVideo", searchValue);

            GameVideo returnValue = new GameVideo();
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
                        returnValue = Storage.GetCacheValue<GameVideo>(returnValue, "id", (long)searchValue);
                    }
                    break;
                case Storage.CacheStatus.Current:
                    returnValue = Storage.GetCacheValue<GameVideo>(returnValue, "id", (long)searchValue);
                    break;
                default:
                    throw new Exception("How did you get here?");
            }

            return returnValue;
        }

        private static async Task<GameVideo> GetObjectFromServer(long searchValue)
        {
            // get Game_Videos metadata
            Communications comms = new Communications();
            var results = await comms.APIComm<GameVideo>(Communications.MetadataEndpoint.GameVideo, searchValue);
            var result = results.First();

            return result;
        }
    }
}

