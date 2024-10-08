﻿using System;
using System.Net;
using IGDB;
using IGDB.Models;
using Microsoft.CodeAnalysis.Elfie.Model.Strings;


namespace gaseous_server.Classes.Metadata
{
    public class Covers
    {
        const string fieldList = "fields alpha_channel,animated,checksum,game,game_localization,height,image_id,url,width;";

        public Covers()
        {
        }

        public static Cover? GetCover(long? Id, string ImagePath, bool GetImages)
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

        public static Cover GetCover(string Slug, string ImagePath, bool GetImages)
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
                    forceImageDownload = true;
                    break;
                case Storage.CacheStatus.Expired:
                    try
                    {
                        returnValue = await GetObjectFromServer(WhereClause, ImagePath);
                        Storage.NewCacheValue(returnValue, true);

                        // check if old value is different from the new value - only download if it's different
                        Cover oldCover = Storage.GetCacheValue<Cover>(returnValue, "id", (long)searchValue);
                        if (oldCover.ImageId != returnValue.ImageId)
                        {
                            forceImageDownload = true;
                        }
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

            string localFile = Path.Combine(ImagePath, Communications.IGDBAPI_ImageSize.original.ToString(), returnValue.ImageId + ".jpg");
            if (GetImages == true)
            {
                if ((!File.Exists(localFile)) || forceImageDownload == true)
                {
                    Logging.Log(Logging.LogType.Information, "Metadata: " + returnValue.GetType().Name, "Cover download forced.");

                    // check for presence of image file - download if absent or force download is true
                    List<Communications.IGDBAPI_ImageSize> imageSizes = new List<Communications.IGDBAPI_ImageSize>{
                        Communications.IGDBAPI_ImageSize.cover_big,
                        Communications.IGDBAPI_ImageSize.cover_small,
                        Communications.IGDBAPI_ImageSize.original
                    };

                    Communications comms = new Communications();
                    foreach (Communications.IGDBAPI_ImageSize size in imageSizes)
                    {
                        localFile = Path.Combine(ImagePath, size.ToString(), returnValue.ImageId + ".jpg");
                        if ((!File.Exists(localFile)) || forceImageDownload == true)
                        {
                            comms.GetSpecificImageFromServer(ImagePath, returnValue.ImageId, size, null);
                        }
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
    }
}

