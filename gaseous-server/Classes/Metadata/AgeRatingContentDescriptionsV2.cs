using System;
using IGDB;
using IGDB.Models;


namespace gaseous_server.Classes.Metadata
{
    public class AgeRatingContentDescriptionsV2
    {
        const string fieldList = "fields *;";

        public AgeRatingContentDescriptionsV2()
        {
        }

        public static AgeRatingContentDescriptionV2? GetAgeRatingContentDescriptions(long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Task<AgeRatingContentDescriptionV2> RetVal = _GetAgeRatingContentDescriptions(SearchUsing.id, Id);
                return RetVal.Result;
            }
        }

        public static AgeRatingContentDescriptionV2 GetAgeRatingContentDescriptions(string Slug)
        {
            Task<AgeRatingContentDescriptionV2> RetVal = _GetAgeRatingContentDescriptions(SearchUsing.slug, Slug);
            return RetVal.Result;
        }

        private static async Task<AgeRatingContentDescriptionV2> _GetAgeRatingContentDescriptions(SearchUsing searchUsing, object searchValue)
        {
            // check database first
            Storage.CacheStatus? cacheStatus = new Storage.CacheStatus();
            if (searchUsing == SearchUsing.id)
            {
                cacheStatus = Storage.GetCacheStatus("AgeRatingContentDescriptionV2", (long)searchValue);
            }
            else
            {
                cacheStatus = Storage.GetCacheStatus("AgeRatingContentDescriptionV2", (string)searchValue);
            }

            // set up where clause
            string WhereClause = "";
            switch (searchUsing)
            {
                case SearchUsing.id:
                    WhereClause = "where id = " + searchValue;
                    break;
                case SearchUsing.slug:
                    WhereClause = "where slug = " + searchValue;
                    break;
                default:
                    throw new Exception("Invalid search type");
            }

            AgeRatingContentDescriptionV2 returnValue = new AgeRatingContentDescriptionV2();
            switch (cacheStatus)
            {
                case Storage.CacheStatus.NotPresent:
                    returnValue = await GetObjectFromServer(WhereClause);
                    Storage.NewCacheValue(returnValue);
                    break;
                case Storage.CacheStatus.Expired:
                    try
                    {
                        returnValue = await GetObjectFromServer(WhereClause);
                        Storage.NewCacheValue(returnValue, true);
                    }
                    catch (Exception ex)
                    {
                        Logging.Log(Logging.LogType.Warning, "Metadata: " + returnValue.GetType().Name, "An error occurred while connecting to IGDB. WhereClause: " + WhereClause, ex);
                        returnValue = Storage.GetCacheValue<AgeRatingContentDescriptionV2>(returnValue, "id", (long)searchValue);
                    }
                    break;
                case Storage.CacheStatus.Current:
                    returnValue = Storage.GetCacheValue<AgeRatingContentDescriptionV2>(returnValue, "id", (long)searchValue);
                    break;
                default:
                    throw new Exception("How did you get here?");
            }

            return returnValue;
        }

        private enum SearchUsing
        {
            id,
            slug
        }

        private static async Task<AgeRatingContentDescriptionV2> GetObjectFromServer(string WhereClause)
        {
            // get AgeRatingContentDescriptionContentDescriptions metadata
            Communications comms = new Communications();
            var results = await comms.APIComm<AgeRatingContentDescriptionV2>(IGDBClient.Endpoints.AgeRatingContentDescriptionsV2, fieldList, WhereClause);
            var result = results.First();

            return result;
        }
    }
}

