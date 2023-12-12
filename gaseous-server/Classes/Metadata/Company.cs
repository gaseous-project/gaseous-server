using System;
using IGDB;
using IGDB.Models;

namespace gaseous_server.Classes.Metadata
{
	public class Companies
	{
        const string fieldList = "fields change_date,change_date_category,changed_company_id,checksum,country,created_at,description,developed,logo,name,parent,published,slug,start_date,start_date_category,updated_at,url,websites;";

        public Companies()
        {
        }

        public static Company? GetCompanies(long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Task<Company> RetVal = _GetCompanies(SearchUsing.id, Id);
                return RetVal.Result;
            }
        }

        public static Company GetCompanies(string Slug)
        {
            Task<Company> RetVal = _GetCompanies(SearchUsing.slug, Slug);
            return RetVal.Result;
        }

        private static async Task<Company> _GetCompanies(SearchUsing searchUsing, object searchValue)
        {
            // check database first
            Storage.CacheStatus? cacheStatus = new Storage.CacheStatus();
            if (searchUsing == SearchUsing.id)
            {
                cacheStatus = Storage.GetCacheStatus("Company", (long)searchValue);
            }
            else
            {
                cacheStatus = Storage.GetCacheStatus("Company", (string)searchValue);
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

            Company returnValue = new Company();
            switch (cacheStatus)
            {
                case Storage.CacheStatus.NotPresent:
                    returnValue = await GetObjectFromServer(WhereClause);
                    if (returnValue != null) { Storage.NewCacheValue(returnValue); }
                    UpdateSubClasses(returnValue);
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
                        returnValue = Storage.GetCacheValue<Company>(returnValue, "id", (long)searchValue);
                    }
                    break;
                case Storage.CacheStatus.Current:
                    returnValue = Storage.GetCacheValue<Company>(returnValue, "id", (long)searchValue);
                    break;
                default:
                    throw new Exception("How did you get here?");
            }

            return returnValue;
        }

        private static void UpdateSubClasses(Company company)
        {
            if (company.Logo != null)
            {
                CompanyLogo companyLogo = CompanyLogos.GetCompanyLogo(company.Logo.Id, Config.LibraryConfiguration.LibraryMetadataDirectory_Company(company));
            }
        }

        private enum SearchUsing
        {
            id,
            slug
        }

        private static async Task<Company> GetObjectFromServer(string WhereClause)
        {
            // get Companies metadata
            Communications comms = new Communications();
            var results = await comms.APIComm<Company>(IGDBClient.Endpoints.Companies, fieldList, WhereClause);
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

