using System;
using IGDB;
using IGDB.Models;

namespace gaseous_server.Classes.Metadata
{
    public class InvolvedCompanies
    {
        public const string fieldList = "fields *;";

        public InvolvedCompanies()
        {
        }

        public static InvolvedCompany? GetInvolvedCompanies(long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Task<InvolvedCompany> RetVal = _GetInvolvedCompanies((long)Id);
                return RetVal.Result;
            }
        }

        private static async Task<InvolvedCompany> _GetInvolvedCompanies(long searchValue)
        {
            // check database first
            Storage.CacheStatus? cacheStatus = Storage.GetCacheStatus("InvolvedCompany", searchValue);

            InvolvedCompany returnValue = new InvolvedCompany();
            switch (cacheStatus)
            {
                case Storage.CacheStatus.NotPresent:
                    returnValue = await GetObjectFromServer(searchValue);
                    Storage.NewCacheValue(returnValue);
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
                        returnValue = Storage.GetCacheValue<InvolvedCompany>(returnValue, "id", (long)searchValue);
                    }
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

        private static async Task<InvolvedCompany> GetObjectFromServer(long searchValue)
        {
            // get Genres metadata
            Communications comms = new Communications();
            var results = await comms.APIComm<InvolvedCompany>(Communications.MetadataEndpoint.InvolvedCompany, searchValue);
            var result = results.First();

            return result;
        }
    }
}

