using System;
using IGDB;
using IGDB.Models;


namespace gaseous_server.Classes.Metadata
{
	public class AgeRatingContentDescriptions
    {
        const string fieldList = "fields category,checksum,description;";

        public AgeRatingContentDescriptions()
        {
        }

        private static IGDBClient igdb = new IGDBClient(
                    // Found in Twitch Developer portal for your app
                    Config.IGDB.ClientId,
                    Config.IGDB.Secret
                );

        public static AgeRatingContentDescription? GetAgeRatingContentDescriptions(long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Task<AgeRatingContentDescription> RetVal = _GetAgeRatingContentDescriptions(SearchUsing.id, Id);
                return RetVal.Result;
            }
        }

        public static AgeRatingContentDescription GetAgeRatingContentDescriptions(string Slug)
        {
            Task<AgeRatingContentDescription> RetVal = _GetAgeRatingContentDescriptions(SearchUsing.slug, Slug);
            return RetVal.Result;
        }

        private static async Task<AgeRatingContentDescription> _GetAgeRatingContentDescriptions(SearchUsing searchUsing, object searchValue)
        {
            // check database first
            Storage.CacheStatus? cacheStatus = new Storage.CacheStatus();
            if (searchUsing == SearchUsing.id)
            {
                cacheStatus = Storage.GetCacheStatus("AgeRatingContentDescription", (long)searchValue);
            }
            else
            {
                cacheStatus = Storage.GetCacheStatus("AgeRatingContentDescription", (string)searchValue);
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

            AgeRatingContentDescription returnValue = new AgeRatingContentDescription();
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

        private enum SearchUsing
        {
            id,
            slug
        }

        private static async Task<AgeRatingContentDescription> GetObjectFromServer(string WhereClause)
        {
            // get AgeRatingContentDescriptionContentDescriptions metadata
            var results = await igdb.QueryAsync<AgeRatingContentDescription>(IGDBClient.Endpoints.AgeRatingContentDescriptions, query: fieldList + " " + WhereClause + ";");
            var result = results.First();

            return result;
        }
	}
}

