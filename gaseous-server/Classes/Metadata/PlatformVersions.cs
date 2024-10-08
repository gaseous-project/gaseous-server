﻿using System;
using System.Data;
using IGDB;
using IGDB.Models;

namespace gaseous_server.Classes.Metadata
{
    public class PlatformVersions
    {
        const string fieldList = "fields checksum,companies,connectivity,cpu,graphics,main_manufacturer,media,memory,name,online,os,output,platform_logo,platform_version_release_dates,resolutions,slug,sound,storage,summary,url;";

        public PlatformVersions()
        {
        }

        public static PlatformVersion? GetPlatformVersion(long Id, Platform ParentPlatform, bool GetImages = false)
        {
            if (Id == 0)
            {
                return null;
            }
            else
            {
                Task<PlatformVersion> RetVal = _GetPlatformVersion(SearchUsing.id, Id, ParentPlatform, GetImages);
                return RetVal.Result;
            }
        }

        public static PlatformVersion GetPlatformVersion(string Slug, Platform ParentPlatform, bool GetImages = false)
        {
            Task<PlatformVersion> RetVal = _GetPlatformVersion(SearchUsing.slug, Slug, ParentPlatform, GetImages);
            return RetVal.Result;
        }

        private static async Task<PlatformVersion> _GetPlatformVersion(SearchUsing searchUsing, object searchValue, Platform ParentPlatform, bool GetImages)
        {
            // check database first
            Storage.CacheStatus? cacheStatus = new Storage.CacheStatus();
            if (searchUsing == SearchUsing.id)
            {
                cacheStatus = Storage.GetCacheStatus("PlatformVersion", (long)searchValue);
            }
            else
            {
                cacheStatus = Storage.GetCacheStatus("PlatformVersion", (string)searchValue);
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

            PlatformVersion returnValue = new PlatformVersion();
            switch (cacheStatus)
            {
                case Storage.CacheStatus.NotPresent:
                    returnValue = await GetObjectFromServer(WhereClause);
                    if (returnValue != null)
                    {
                        Storage.NewCacheValue(returnValue);
                        UpdateSubClasses(ParentPlatform, returnValue, GetImages);
                    }
                    return returnValue;
                case Storage.CacheStatus.Expired:
                    try
                    {
                        returnValue = await GetObjectFromServer(WhereClause);
                        Storage.NewCacheValue(returnValue, true);
                        UpdateSubClasses(ParentPlatform, returnValue, GetImages);
                    }
                    catch (Exception ex)
                    {
                        Logging.Log(Logging.LogType.Warning, "Metadata: " + returnValue.GetType().Name, "An error occurred while connecting to IGDB. WhereClause: " + WhereClause, ex);
                        returnValue = Storage.GetCacheValue<PlatformVersion>(returnValue, "id", (long)searchValue);
                    }
                    return returnValue;
                case Storage.CacheStatus.Current:
                    return Storage.GetCacheValue<PlatformVersion>(returnValue, "id", (long)searchValue);
                default:
                    throw new Exception("How did you get here?");
            }
        }

        private static void UpdateSubClasses(Platform ParentPlatform, PlatformVersion platformVersion, bool GetImages)
        {
            if (GetImages == true)
            {
                if (platformVersion.PlatformLogo != null)
                {
                    try
                    {
                        PlatformLogo platformLogo = PlatformLogos.GetPlatformLogo(platformVersion.PlatformLogo.Id, Path.Combine(Config.LibraryConfiguration.LibraryMetadataDirectory_Platform(ParentPlatform), "Versions", platformVersion.Slug));
                    }
                    catch (Exception ex)
                    {
                        Logging.Log(Logging.LogType.Warning, "Platform Update", "Unable to fetch platform logo", ex);
                    }
                }
            }
        }

        private enum SearchUsing
        {
            id,
            slug
        }

        private static async Task<PlatformVersion?> GetObjectFromServer(string WhereClause)
        {
            // get PlatformVersion metadata
            Communications comms = new Communications();
            var results = await comms.APIComm<PlatformVersion>(IGDBClient.Endpoints.PlatformVersions, fieldList, WhereClause);
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

