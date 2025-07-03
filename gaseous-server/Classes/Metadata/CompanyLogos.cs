using System;
using IGDB;
using IGDB.Models;


namespace gaseous_server.Classes.Metadata
{
    public class CompanyLogos
    {
        const string fieldList = "fields *;";

        public CompanyLogos()
        {
        }

        public static CompanyLogo? GetCompanyLogo(long? Id, string ImagePath)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Task<CompanyLogo> RetVal = _GetCompanyLogo(SearchUsing.id, Id, ImagePath);
                return RetVal.Result;
            }
        }

        public static CompanyLogo GetCompanyLogo(string Slug, string ImagePath)
        {
            Task<CompanyLogo> RetVal = _GetCompanyLogo(SearchUsing.slug, Slug, ImagePath);
            return RetVal.Result;
        }

        private static async Task<CompanyLogo> _GetCompanyLogo(SearchUsing searchUsing, object searchValue, string ImagePath)
        {
            // check database first
            Storage.CacheStatus? cacheStatus = new Storage.CacheStatus();
            if (searchUsing == SearchUsing.id)
            {
                cacheStatus = Storage.GetCacheStatus("CompanyLogo", (long)searchValue);
            }
            else
            {
                cacheStatus = Storage.GetCacheStatus("CompanyLogo", (string)searchValue);
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

            CompanyLogo returnValue = new CompanyLogo();
            bool forceImageDownload = false;
            switch (cacheStatus)
            {
                case Storage.CacheStatus.NotPresent:
                    returnValue = await GetObjectFromServer(WhereClause, ImagePath);
                    if (returnValue != null)
                    {
                        Storage.NewCacheValue(returnValue);
                        forceImageDownload = true;
                    }
                    break;
                case Storage.CacheStatus.Expired:
                    try
                    {
                        returnValue = await GetObjectFromServer(WhereClause, ImagePath);
                        Storage.NewCacheValue(returnValue, true);
                        forceImageDownload = true;
                    }
                    catch (Exception ex)
                    {
                        Logging.Log(Logging.LogType.Warning, "Metadata: " + returnValue.GetType().Name, "An error occurred while connecting to IGDB. WhereClause: " + WhereClause, ex);
                        returnValue = Storage.GetCacheValue<CompanyLogo>(returnValue, "id", (long)searchValue);
                    }
                    break;
                case Storage.CacheStatus.Current:
                    returnValue = Storage.GetCacheValue<CompanyLogo>(returnValue, "id", (long)searchValue);
                    break;
                default:
                    throw new Exception("How did you get here?");
            }

            // check for presence of "original" quality file - download if absent or force download is true
            string localFile = Path.Combine(ImagePath, Communications.IGDBAPI_ImageSize.original.ToString(), returnValue.ImageId + ".jpg");
            if ((!File.Exists(localFile)) || forceImageDownload == true)
            {
                Logging.Log(Logging.LogType.Information, "Metadata: " + returnValue.GetType().Name, "Company logo download forced.");

                Communications comms = new Communications();
                comms.GetSpecificImageFromServer(ImagePath, returnValue.ImageId, Communications.IGDBAPI_ImageSize.original, null);
            }

            return returnValue;
        }

        private enum SearchUsing
        {
            id,
            slug
        }

        private static async Task<CompanyLogo> GetObjectFromServer(string WhereClause, string ImagePath)
        {
            // get Artwork metadata
            Communications comms = new Communications();
            var results = await comms.APIComm<CompanyLogo>(IGDBClient.Endpoints.CompanyLogos, fieldList, WhereClause);
            var result = results.First();

            return result;
        }
    }
}

