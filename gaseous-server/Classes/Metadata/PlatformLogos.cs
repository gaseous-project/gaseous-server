using System;
using IGDB;
using IGDB.Models;


namespace gaseous_server.Classes.Metadata
{
	public class PlatformLogos
    {
        const string fieldList = "fields alpha_channel,animated,checksum,height,image_id,url,width;";

        public PlatformLogos()
        {
        }

        public static PlatformLogo? GetPlatformLogo(long? Id, string ImagePath)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Task<PlatformLogo> RetVal = _GetPlatformLogo(SearchUsing.id, Id, ImagePath);
                return RetVal.Result;
            }
        }

        public static PlatformLogo GetPlatformLogo(string Slug, string ImagePath)
        {
            Task<PlatformLogo> RetVal = _GetPlatformLogo(SearchUsing.slug, Slug, ImagePath);
            return RetVal.Result;
        }

        private static async Task<PlatformLogo> _GetPlatformLogo(SearchUsing searchUsing, object searchValue, string ImagePath)
        {
            // check database first
            Storage.CacheStatus? cacheStatus = new Storage.CacheStatus();
            if (searchUsing == SearchUsing.id)
            {
                cacheStatus = Storage.GetCacheStatus("PlatformLogo", (long)searchValue);
            }
            else
            {
                cacheStatus = Storage.GetCacheStatus("PlatformLogo", (string)searchValue);
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

            PlatformLogo returnValue = new PlatformLogo();
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
                        
                        // check if old value is different from the new value - only download if it's different
                        PlatformLogo oldImage = Storage.GetCacheValue<PlatformLogo>(returnValue, "id", (long)searchValue);
                        if (oldImage.ImageId != returnValue.ImageId)
                        {
                            forceImageDownload = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logging.Log(Logging.LogType.Warning, "Metadata: " + returnValue.GetType().Name, "An error occurred while connecting to IGDB. WhereClause: " + WhereClause, ex);
                        returnValue = Storage.GetCacheValue<PlatformLogo>(returnValue, "id", (long)searchValue);
                    }
                    break;  
                case Storage.CacheStatus.Current:
                    returnValue = Storage.GetCacheValue<PlatformLogo>(returnValue, "id", (long)searchValue);
                    break;
                default:
                    throw new Exception("How did you get here?");
            }

            if (returnValue != null)
            {
                // check for presence of "original" quality file - download if absent or force download is true
                string localFile = Path.Combine(ImagePath, Communications.IGDBAPI_ImageSize.original.ToString(), returnValue.ImageId + ".jpg");
                if ((!File.Exists(localFile)) || forceImageDownload == true)
                {
                    Logging.Log(Logging.LogType.Information, "Metadata: " + returnValue.GetType().Name, "Platform logo download forced.");

                    Communications comms = new Communications();
                    comms.GetSpecificImageFromServer(ImagePath, returnValue.ImageId, Communications.IGDBAPI_ImageSize.original, null);
                }
            }

            return returnValue;
        }

        private enum SearchUsing
        {
            id,
            slug
        }

        private static async Task<PlatformLogo> GetObjectFromServer(string WhereClause, string ImagePath)
        {
            // get Artwork metadata
            Communications comms = new Communications();
            var results = await comms.APIComm<PlatformLogo>(IGDBClient.Endpoints.PlatformLogos, fieldList, WhereClause);
            var result = results.First();

            return result;
        }
	}
}

