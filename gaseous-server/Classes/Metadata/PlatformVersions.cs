using System;
using System.Data;
using IGDB;
using IGDB.Models;

namespace gaseous_server.Classes.Metadata
{
    public class PlatformVersions
    {
        public const string fieldList = "fields checksum,companies,connectivity,cpu,graphics,main_manufacturer,media,memory,name,online,os,output,platform_logo,platform_version_release_dates,resolutions,slug,sound,storage,summary,url;";

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
                Task<PlatformVersion> RetVal = _GetPlatformVersion((long)Id, ParentPlatform, GetImages);
                return RetVal.Result;
            }
        }

        private static async Task<PlatformVersion> _GetPlatformVersion(long searchValue, Platform ParentPlatform, bool GetImages)
        {
            // check database first
            Storage.CacheStatus? cacheStatus = Storage.GetCacheStatus("PlatformVersion", searchValue);

            PlatformVersion returnValue = new PlatformVersion();
            switch (cacheStatus)
            {
                case Storage.CacheStatus.NotPresent:
                    returnValue = await GetObjectFromServer(searchValue);
                    if (returnValue != null)
                    {
                        Storage.NewCacheValue(returnValue);
                        UpdateSubClasses(ParentPlatform, returnValue, GetImages);
                    }
                    return returnValue;
                case Storage.CacheStatus.Expired:
                    try
                    {
                        returnValue = await GetObjectFromServer(searchValue);
                        Storage.NewCacheValue(returnValue, true);
                        UpdateSubClasses(ParentPlatform, returnValue, GetImages);
                    }
                    catch (Exception ex)
                    {
                        Logging.Log(Logging.LogType.Warning, "Metadata: " + returnValue.GetType().Name, "An error occurred while connecting to IGDB. Id: " + searchValue, ex);
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

        private static async Task<PlatformVersion?> GetObjectFromServer(long searchValue)
        {
            // get PlatformVersion metadata
            Communications comms = new Communications();
            var results = await comms.APIComm<PlatformVersion>(Communications.MetadataEndpoint.PlatformVersion, searchValue);
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

