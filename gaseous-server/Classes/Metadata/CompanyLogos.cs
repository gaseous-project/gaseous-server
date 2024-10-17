using System;
using IGDB;
using IGDB.Models;


namespace gaseous_server.Classes.Metadata
{
    public class CompanyLogos
    {
        public const string fieldList = "fields alpha_channel,animated,checksum,height,image_id,url,width;";

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
                Task<CompanyLogo> RetVal = _GetCompanyLogo((long)Id, ImagePath);
                return RetVal.Result;
            }
        }

        private static async Task<CompanyLogo> _GetCompanyLogo(long searchValue, string ImagePath)
        {
            // check database first
            Storage.CacheStatus? cacheStatus = Storage.GetCacheStatus("CompanyLogo", searchValue);

            CompanyLogo returnValue = new CompanyLogo();
            bool forceImageDownload = false;
            switch (cacheStatus)
            {
                case Storage.CacheStatus.NotPresent:
                    returnValue = await GetObjectFromServer(searchValue, ImagePath);
                    if (returnValue != null)
                    {
                        Storage.NewCacheValue(returnValue);
                        forceImageDownload = true;
                    }
                    break;
                case Storage.CacheStatus.Expired:
                    try
                    {
                        returnValue = await GetObjectFromServer(searchValue, ImagePath);
                        Storage.NewCacheValue(returnValue, true);

                        // check if old value is different from the new value - only download if it's different
                        CompanyLogo oldImage = Storage.GetCacheValue<CompanyLogo>(returnValue, "id", (long)searchValue);
                        if (oldImage.ImageId != returnValue.ImageId)
                        {
                            forceImageDownload = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logging.Log(Logging.LogType.Warning, "Metadata: " + returnValue.GetType().Name, "An error occurred while connecting to IGDB. Id: " + searchValue, ex);
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

        private static async Task<CompanyLogo> GetObjectFromServer(long searchValue, string ImagePath)
        {
            // get Artwork metadata
            Communications comms = new Communications();
            var results = await comms.APIComm<CompanyLogo>(Communications.MetadataEndpoint.CompanyLogo, searchValue);
            var result = results.First();

            return result;
        }
    }
}

