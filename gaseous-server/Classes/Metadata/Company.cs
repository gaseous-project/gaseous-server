using System;
using IGDB;
using IGDB.Models;

namespace gaseous_server.Classes.Metadata
{
    public class Companies
    {
        public const string fieldList = "fields change_date,change_date_category,changed_company_id,checksum,country,created_at,description,developed,logo,name,parent,published,slug,start_date,start_date_category,updated_at,url,websites;";

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
                Task<Company> RetVal = _GetCompanies((long)Id);
                return RetVal.Result;
            }
        }

        private static async Task<Company> _GetCompanies(long searchValue)
        {
            // check database first
            Storage.CacheStatus? cacheStatus = Storage.GetCacheStatus("Company", searchValue);

            Company returnValue = new Company();
            switch (cacheStatus)
            {
                case Storage.CacheStatus.NotPresent:
                    returnValue = await GetObjectFromServer(searchValue);
                    if (returnValue != null) { Storage.NewCacheValue(returnValue); }
                    UpdateSubClasses(returnValue);
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

        private static async Task<Company> GetObjectFromServer(long searchValue)
        {
            // get Companies metadata
            Communications comms = new Communications();
            var results = await comms.APIComm<Company>(Communications.MetadataEndpoint.Company, searchValue);
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

