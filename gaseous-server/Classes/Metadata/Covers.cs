using System;
using System.Net;
using IGDB;
using IGDB.Models;
using Microsoft.CodeAnalysis.Elfie.Model.Strings;


namespace gaseous_server.Classes.Metadata
{
	public class Covers
    {
        const string fieldList = "fields alpha_channel,animated,checksum,game,height,image_id,url,width;";

        public Covers()
        {
        }

        public static Cover? GetCover(long? Id, string ImagePath, bool GetImages = true)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Task<Cover> RetVal = _GetCover(SearchUsing.id, Id, ImagePath, GetImages);
                return RetVal.Result;
            }
        }

        public static Cover GetCover(string Slug, string ImagePath, bool GetImages = true)
        {
            Task<Cover> RetVal = _GetCover(SearchUsing.slug, Slug, ImagePath, GetImages);
            return RetVal.Result;
        }

        private static async Task<Cover> _GetCover(SearchUsing searchUsing, object searchValue, string ImagePath, bool GetImages = true)
        {
            // check database first
            Storage.CacheStatus? cacheStatus = new Storage.CacheStatus();
            if (searchUsing == SearchUsing.id)
            {
                cacheStatus = Storage.GetCacheStatus("Cover", (long)searchValue);
            }
            else
            {
                cacheStatus = Storage.GetCacheStatus("Cover", (string)searchValue);
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

            Cover returnValue = new Cover();
            bool forceImageDownload = false;
            ImagePath = Path.Combine(ImagePath, "Covers");
            switch (cacheStatus)
            {
                case Storage.CacheStatus.NotPresent:
                    returnValue = await GetObjectFromServer(WhereClause, ImagePath);
                    Storage.NewCacheValue(returnValue);
                    if (GetImages == true) { forceImageDownload = true; }
                    break;  
                case Storage.CacheStatus.Expired:
                    try
                    {
                        returnValue = await GetObjectFromServer(WhereClause, ImagePath);
                        Storage.NewCacheValue(returnValue, true);
                        if (GetImages == true) { forceImageDownload = true; }
                    }
                    catch (Exception ex)
                    {
                        Logging.Log(Logging.LogType.Warning, "Metadata: " + returnValue.GetType().Name, "An error occurred while connecting to IGDB. WhereClause: " + WhereClause, ex);
                        returnValue = Storage.GetCacheValue<Cover>(returnValue, "id", (long)searchValue);
                    }
                    break;
                case Storage.CacheStatus.Current:
                    returnValue = Storage.GetCacheValue<Cover>(returnValue, "id", (long)searchValue);
                    break;
                default:
                    throw new Exception("How did you get here?");
            }

            if (forceImageDownload == true)
            {
                Logging.Log(Logging.LogType.Information, "Metadata: " + returnValue.GetType().Name, "Cover download forced.");

                // check for presence of image file - download if absent or force download is true
                List<Communications.IGDBAPI_ImageSize> imageSizes = new List<Communications.IGDBAPI_ImageSize>();
                imageSizes.AddRange(Enum.GetValues(typeof(Communications.IGDBAPI_ImageSize)).Cast<Communications.IGDBAPI_ImageSize>());
                foreach (Communications.IGDBAPI_ImageSize size in imageSizes)
                {
                    string localFile = Path.Combine(ImagePath, size.ToString(), returnValue.ImageId + ".jpg");
                    if ((!File.Exists(localFile)) || forceImageDownload == true)
                    {
                        Communications.GetSpecificImageFromServer(ImagePath, returnValue.ImageId, size, null);
                    }
                }
            }

            return returnValue;
        }

        private enum SearchUsing
        {
            id,
            slug
        }

        private static async Task<Cover> GetObjectFromServer(string WhereClause, string ImagePath)
        {
            // get Cover metadata
            Communications comms = new Communications();
            var results = await comms.APIComm<Cover>(IGDBClient.Endpoints.Covers, fieldList, WhereClause);
            var result = results.First();

            return result;
        }

        public static async void GetImageFromServer(string ImagePath, string ImageId)
        {
            Communications comms = new Communications();
            List<Communications.IGDBAPI_ImageSize> imageSizes = new List<Communications.IGDBAPI_ImageSize>();
            imageSizes.AddRange(Enum.GetValues(typeof(Communications.IGDBAPI_ImageSize)).Cast<Communications.IGDBAPI_ImageSize>());

            await comms.IGDBAPI_GetImage(imageSizes, ImageId, ImagePath);
        }
	}
}

