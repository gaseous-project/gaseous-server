using System;
using gaseous_tools;
using IGDB;
using IGDB.Models;

namespace gaseous_server.Classes.Metadata
{
	public class InvolvedCompanies
	{
        const string fieldList = "fields *;";

        public InvolvedCompanies()
        {
        }

        private static IGDBClient igdb = new IGDBClient(
                    // Found in Twitch Developer portal for your app
                    Config.IGDB.ClientId,
                    Config.IGDB.Secret
                );

        public static InvolvedCompany? GetInvolvedCompanies(long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Task<InvolvedCompany> RetVal = _GetInvolvedCompanies(SearchUsing.id, Id);
                return RetVal.Result;
            }
        }

        public static InvolvedCompany GetInvolvedCompanies(string Slug)
        {
            Task<InvolvedCompany> RetVal = _GetInvolvedCompanies(SearchUsing.slug, Slug);
            return RetVal.Result;
        }

        private static async Task<InvolvedCompany> _GetInvolvedCompanies(SearchUsing searchUsing, object searchValue)
        {
            // check database first
            Storage.CacheStatus? cacheStatus = new Storage.CacheStatus();
            if (searchUsing == SearchUsing.id)
            {
                cacheStatus = Storage.GetCacheStatus("InvolvedCompany", (long)searchValue);
            }
            else
            {
                cacheStatus = Storage.GetCacheStatus("InvolvedCompany", (string)searchValue);
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

            InvolvedCompany returnValue = new InvolvedCompany();
            switch (cacheStatus)
            {
                case Storage.CacheStatus.NotPresent:
                    returnValue = await GetObjectFromServer(WhereClause);
                    Storage.NewCacheValue(returnValue);
                    UpdateSubClasses(returnValue);
                    break;
                case Storage.CacheStatus.Expired:
                    returnValue = await GetObjectFromServer(WhereClause);
                    Storage.NewCacheValue(returnValue, true);
                    UpdateSubClasses(returnValue);
                    break;
                case Storage.CacheStatus.Current:
                    returnValue = Storage.GetCacheValue<InvolvedCompany>(returnValue, "id", (long)searchValue);
                    break;
                default:
                    throw new Exception("How did you get here?");
            }

            return returnValue;
        }

        private static void UpdateSubClasses(InvolvedCompany involvedCompany)
        {
            if (involvedCompany.Company != null)
            {
                Company company = Companies.GetCompanies(involvedCompany.Company.Id);
            }
        }

        private enum SearchUsing
        {
            id,
            slug
        }

        private static async Task<InvolvedCompany> GetObjectFromServer(string WhereClause)
        {
            // get InvolvedCompanies metadata
            try
            {
                var results = await igdb.QueryAsync<InvolvedCompany>(IGDBClient.Endpoints.InvolvedCompanies, query: fieldList + " " + WhereClause + ";");
                var result = results.First();

                return result;
            }
            catch (Exception ex)
            {
                Logging.Log(Logging.LogType.Critical, "Involved Companies", "Failure when requesting involved companies.");
                Logging.Log(Logging.LogType.Critical, "Involved Companies", "Field list: " + fieldList);
                Logging.Log(Logging.LogType.Critical, "Involved Companies", "Where clause: " + WhereClause);
                Logging.Log(Logging.LogType.Critical, "Involved Companies", "Error", ex);
                throw;
            }
        }
    }
}

