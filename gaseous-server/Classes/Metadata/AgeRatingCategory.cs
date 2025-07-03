using System;
using IGDB;
using IGDB.Models;


namespace gaseous_server.Classes.Metadata
{
    public class AgeRatingCategories
    {
        const string fieldList = "fields *;";

        public AgeRatingCategories()
        {
        }

        public static AgeRatingCategory? GetAgeRatingCategory(long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Task<AgeRatingCategory> RetVal = _GetAgeRatingCategory(SearchUsing.id, Id);
                return RetVal.Result;
            }
        }

        private static async Task<AgeRatingCategory> _GetAgeRatingCategory(SearchUsing searchUsing, object searchValue)
        {
            // check database first
            Storage.CacheStatus? cacheStatus = new Storage.CacheStatus();
            if (searchUsing == SearchUsing.id)
            {
                cacheStatus = Storage.GetCacheStatus("AgeRatingCategory", (long)searchValue);
            }
            else
            {
                cacheStatus = Storage.GetCacheStatus("AgeRatingCategory", (string)searchValue);
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

            AgeRatingCategory returnValue = new AgeRatingCategory();
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
                        returnValue = Storage.GetCacheValue<AgeRatingCategory>(returnValue, "id", (long)searchValue);
                    }
                    break;
                case Storage.CacheStatus.Current:
                    returnValue = Storage.GetCacheValue<AgeRatingCategory>(returnValue, "id", (long)searchValue);
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

        private static async Task<AgeRatingCategory> GetObjectFromServer(string WhereClause)
        {
            // get Game_Videos metadata
            Communications comms = new Communications();
            var results = await comms.APIComm<AgeRatingCategory>(IGDBClient.Endpoints.AgeRatingCategories, fieldList, WhereClause);
            var result = results.First();

            return result;
        }
    }
}