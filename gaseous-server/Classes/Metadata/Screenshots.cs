﻿using System;
using IGDB;
using IGDB.Models;


namespace gaseous_server.Classes.Metadata
{
    public class Screenshots
    {
        const string fieldList = "fields alpha_channel,animated,checksum,game,height,image_id,url,width;";

        public Screenshots()
        {
        }

        public static Screenshot? GetScreenshot(long? Id, string ImagePath, bool GetImages)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Task<Screenshot> RetVal = _GetScreenshot(SearchUsing.id, Id, ImagePath, GetImages);
                return RetVal.Result;
            }
        }

        public static Screenshot GetScreenshot(string Slug, string ImagePath, bool GetImages)
        {
            Task<Screenshot> RetVal = _GetScreenshot(SearchUsing.slug, Slug, ImagePath, GetImages);
            return RetVal.Result;
        }

        private static async Task<Screenshot> _GetScreenshot(SearchUsing searchUsing, object searchValue, string ImagePath, bool GetImages = true)
        {
            // check database first
            Storage.CacheStatus? cacheStatus = new Storage.CacheStatus();
            if (searchUsing == SearchUsing.id)
            {
                cacheStatus = Storage.GetCacheStatus("Screenshot", (long)searchValue);
            }
            else
            {
                cacheStatus = Storage.GetCacheStatus("Screenshot", (string)searchValue);
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

            Screenshot returnValue = new Screenshot();
            bool forceImageDownload = false;
            ImagePath = Path.Combine(ImagePath, "Screenshots");
            switch (cacheStatus)
            {
                case Storage.CacheStatus.NotPresent:
                    returnValue = await GetObjectFromServer(WhereClause, ImagePath);
                    Storage.NewCacheValue(returnValue);
                    forceImageDownload = true;
                    break;
                case Storage.CacheStatus.Expired:
                    try
                    {
                        returnValue = await GetObjectFromServer(WhereClause, ImagePath);
                        Storage.NewCacheValue(returnValue, true);

                        // check if old value is different from the new value - only download if it's different
                        Screenshot oldImage = Storage.GetCacheValue<Screenshot>(returnValue, "id", (long)searchValue);
                        if (oldImage.ImageId != returnValue.ImageId)
                        {
                            forceImageDownload = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logging.Log(Logging.LogType.Warning, "Metadata: " + returnValue.GetType().Name, "An error occurred while connecting to IGDB. WhereClause: " + WhereClause, ex);
                        returnValue = Storage.GetCacheValue<Screenshot>(returnValue, "id", (long)searchValue);
                    }
                    break;
                case Storage.CacheStatus.Current:
                    returnValue = Storage.GetCacheValue<Screenshot>(returnValue, "id", (long)searchValue);
                    break;
                default:
                    throw new Exception("How did you get here?");
            }

            // check for presence of "original" quality file - download if absent or force download is true
            string localFile = Path.Combine(ImagePath, Communications.IGDBAPI_ImageSize.original.ToString(), returnValue.ImageId + ".jpg");
            if (GetImages == true)
            {
                if ((!File.Exists(localFile)) || forceImageDownload == true)
                {
                    Logging.Log(Logging.LogType.Information, "Metadata: " + returnValue.GetType().Name, "Screenshot download forced.");

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

        private static async Task<Screenshot> GetObjectFromServer(string WhereClause, string ImagePath)
        {
            // get Screenshot metadata
            Communications comms = new Communications();
            var results = await comms.APIComm<Screenshot>(IGDBClient.Endpoints.Screenshots, fieldList, WhereClause);
            var result = results.First();

            return result;
        }
    }
}

