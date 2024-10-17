using System;
using IGDB;
using IGDB.Models;


namespace gaseous_server.Classes.Metadata
{
    public class ExternalGames
    {
        public const string fieldList = "fields category,checksum,countries,created_at,game,media,name,platform,uid,updated_at,url,year;";

        public ExternalGames()
        {
        }

        public static ExternalGame? GetExternalGames(long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Task<ExternalGame> RetVal = _GetExternalGames((long)Id);
                return RetVal.Result;
            }
        }

        private static async Task<ExternalGame> _GetExternalGames(long searchValue)
        {
            // check database first
            Storage.CacheStatus? cacheStatus = Storage.GetCacheStatus("ExternalGame", searchValue);

            ExternalGame returnValue = new ExternalGame();
            switch (cacheStatus)
            {
                case Storage.CacheStatus.NotPresent:
                    returnValue = await GetObjectFromServer(searchValue);
                    if (returnValue != null)
                    {
                        Storage.NewCacheValue(returnValue);
                    }
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
                        returnValue = Storage.GetCacheValue<ExternalGame>(returnValue, "id", (long)searchValue);
                    }
                    break;
                case Storage.CacheStatus.Current:
                    returnValue = Storage.GetCacheValue<ExternalGame>(returnValue, "id", (long)searchValue);
                    break;
                default:
                    throw new Exception("How did you get here?");
            }

            return returnValue;
        }

        private static async Task<ExternalGame?> GetObjectFromServer(long searchValue)
        {
            // get ExternalGames metadata
            Communications comms = new Communications();
            var results = await comms.APIComm<ExternalGame>(Communications.MetadataEndpoint.ExternalGame, searchValue);
            if (results.Length > 0)
            {
                var result = results.First();

                return result;
            }
            else
            {
                return null;
            }
        }
    }
}

