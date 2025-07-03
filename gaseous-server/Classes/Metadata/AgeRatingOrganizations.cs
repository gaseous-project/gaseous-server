using System;
using IGDB;
using IGDB.Models;


namespace gaseous_server.Classes.Metadata
{
    public class AgeRatingOrganizations
    {
        const string fieldList = "fields *;";

        public AgeRatingOrganizations()
        {
        }

        public static AgeRatingOrganization? GetAgeRatingOrganizations(long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Task<AgeRatingOrganization> RetVal = _GetAgeRatingOrganizations(SearchUsing.id, Id);
                return RetVal.Result;
            }
        }

        public static AgeRatingOrganization GetAgeRatingOrganizations(string Slug)
        {
            Task<AgeRatingOrganization> RetVal = _GetAgeRatingOrganizations(SearchUsing.slug, Slug);
            return RetVal.Result;
        }

        private static async Task<AgeRatingOrganization> _GetAgeRatingOrganizations(SearchUsing searchUsing, object searchValue)
        {
            // check database first
            Storage.CacheStatus? cacheStatus = new Storage.CacheStatus();
            if (searchUsing == SearchUsing.id)
            {
                cacheStatus = Storage.GetCacheStatus("AgeRatingOrganization", (long)searchValue);
            }
            else
            {
                cacheStatus = Storage.GetCacheStatus("AgeRatingOrganization", (string)searchValue);
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

            AgeRatingOrganization returnValue = new AgeRatingOrganization();
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
                        returnValue = Storage.GetCacheValue<AgeRatingOrganization>(returnValue, "id", (long)searchValue);
                    }
                    break;
                case Storage.CacheStatus.Current:
                    returnValue = Storage.GetCacheValue<AgeRatingOrganization>(returnValue, "id", (long)searchValue);
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

        private static async Task<AgeRatingOrganization> GetObjectFromServer(string WhereClause)
        {
            // get AgeRatingContentDescriptionContentDescriptions metadata
            Communications comms = new Communications();
            var results = await comms.APIComm<AgeRatingOrganization>(IGDBClient.Endpoints.AgeRatingOrganizations, fieldList, WhereClause);
            var result = results.First();

            return result;
        }
    }
}