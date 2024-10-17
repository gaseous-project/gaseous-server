using System;
using IGDB;
using IGDB.Models;


namespace gaseous_server.Classes.Metadata
{
    public class AgeRatingContentDescriptions
    {
        public const string fieldList = "fields category,checksum,description;";

        public AgeRatingContentDescriptions()
        {
        }

        public static AgeRatingContentDescription? GetAgeRatingContentDescriptions(long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Task<AgeRatingContentDescription> RetVal = _GetAgeRatingContentDescriptions((long)Id);
                return RetVal.Result;
            }
        }

        private static async Task<AgeRatingContentDescription> _GetAgeRatingContentDescriptions(long searchValue)
        {
            // check database first
            Storage.CacheStatus? cacheStatus = Storage.GetCacheStatus("AgeRatingContentDescription", searchValue);

            AgeRatingContentDescription returnValue = new AgeRatingContentDescription();
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
                        returnValue = Storage.GetCacheValue<AgeRatingContentDescription>(returnValue, "id", (long)searchValue);
                    }
                    break;
                case Storage.CacheStatus.Current:
                    returnValue = Storage.GetCacheValue<AgeRatingContentDescription>(returnValue, "id", (long)searchValue);
                    break;
                default:
                    throw new Exception("How did you get here?");
            }

            return returnValue;
        }

        private static async Task<AgeRatingContentDescription> GetObjectFromServer(long searchValue)
        {
            // get AgeRatingContentDescriptionContentDescriptions metadata
            Communications comms = new Communications();
            var results = await comms.APIComm<AgeRatingContentDescription>(Communications.MetadataEndpoint.AgeRatingContentDescription, searchValue);
            var result = results.First();

            return result;
        }
    }
}

