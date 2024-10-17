using System;
using System.Data;
using System.Net;
using IGDB;
using IGDB.Models;

namespace gaseous_server.Classes.Metadata
{
    public class Platforms
    {
        public const string fieldList = "fields abbreviation,alternative_name,category,checksum,created_at,generation,name,platform_family,platform_logo,slug,summary,updated_at,url,versions,websites;";

        public Platforms()
        {

        }

        public static Platform? GetPlatform(long Id, bool forceRefresh = false, bool GetImages = false)
        {
            if (Id == 0)
            {
                Platform returnValue = new Platform();
                if (Storage.GetCacheStatus("Platform", 0) == Storage.CacheStatus.NotPresent)
                {
                    returnValue = new Platform
                    {
                        Id = 0,
                        Name = "Unknown Platform",
                        Slug = "Unknown"
                    };
                    Storage.NewCacheValue(returnValue);

                    return returnValue;
                }
                else
                {
                    return Storage.GetCacheValue<Platform>(returnValue, "id", 0);
                }
            }
            else
            {
                try
                {
                    Task<Platform> RetVal = _GetPlatform(SearchUsing.Id, Id, forceRefresh, GetImages);
                    return RetVal.Result;
                }
                catch (Exception ex)
                {
                    Logging.Log(Logging.LogType.Warning, "Metadata", "An error occurred fetching Platform Id " + Id, ex);
                    return null;
                }
            }
        }

        public static Platform GetPlatform(string Slug, bool forceRefresh = false, bool GetImages = false)
        {
            Task<Platform> RetVal = _GetPlatform(SearchUsing.Slug, Slug, forceRefresh, GetImages);
            return RetVal.Result;
        }

        private static async Task<Platform> _GetPlatform(SearchUsing searchUsing, object searchValue, bool forceRefresh, bool GetImages)
        {
            // check database first
            Storage.CacheStatus? cacheStatus = new Storage.CacheStatus();
            if (searchUsing == SearchUsing.Id)
            {
                cacheStatus = Storage.GetCacheStatus("Platform", (long)searchValue);
            }
            else
            {
                cacheStatus = Storage.GetCacheStatus("Platform", (string)searchValue);
            }

            if (forceRefresh == true)
            {
                if (cacheStatus == Storage.CacheStatus.Current) { cacheStatus = Storage.CacheStatus.Expired; }
            }

            Platform returnValue = new Platform();
            switch (cacheStatus)
            {
                case Storage.CacheStatus.NotPresent:
                    if (searchUsing == SearchUsing.Id)
                    {
                        returnValue = await GetObjectFromServer((long)searchValue);
                    }
                    else
                    {
                        returnValue = await GetObjectFromServer((string)searchValue);
                    }
                    Storage.NewCacheValue(returnValue);
                    UpdateSubClasses(returnValue, GetImages);
                    AddPlatformMapping(returnValue);
                    return returnValue;
                case Storage.CacheStatus.Expired:
                    try
                    {
                        if (searchUsing == SearchUsing.Id)
                        {
                            returnValue = await GetObjectFromServer((long)searchValue);
                        }
                        else
                        {
                            returnValue = await GetObjectFromServer((string)searchValue);
                        }
                        Storage.NewCacheValue(returnValue, true);
                        UpdateSubClasses(returnValue, GetImages);
                        AddPlatformMapping(returnValue);
                        return returnValue;
                    }
                    catch (Exception ex)
                    {
                        Logging.Log(Logging.LogType.Warning, "Metadata: " + returnValue.GetType().Name, "An error occurred while connecting to IGDB. Id/Slug: " + searchValue, ex);
                        return Storage.GetCacheValue<Platform>(returnValue, searchUsing.ToString(), searchValue);
                    }
                case Storage.CacheStatus.Current:
                    return Storage.GetCacheValue<Platform>(returnValue, searchUsing.ToString(), searchValue);
                default:
                    throw new Exception("How did you get here?");
            }
        }

        private static void UpdateSubClasses(Platform platform, bool GetImages)
        {
            if (platform.Versions != null)
            {
                foreach (long PlatformVersionId in platform.Versions.Ids)
                {
                    PlatformVersion platformVersion = PlatformVersions.GetPlatformVersion(PlatformVersionId, platform);
                }
            }

            if (GetImages == true)
            {
                if (platform.PlatformLogo != null)
                {
                    try
                    {
                        PlatformLogo platformLogo = PlatformLogos.GetPlatformLogo(platform.PlatformLogo.Id, Config.LibraryConfiguration.LibraryMetadataDirectory_Platform(platform));
                    }
                    catch (Exception ex)
                    {
                        Logging.Log(Logging.LogType.Warning, "Platform Update", "Unable to fetch platform logo", ex);
                    }
                }
            }
        }

        private static void AddPlatformMapping(Platform platform)
        {
            // ensure a mapping item exists for this platform
            Models.PlatformMapping.PlatformMapItem item = new Models.PlatformMapping.PlatformMapItem();
            try
            {
                Logging.Log(Logging.LogType.Information, "Platform Map", "Checking if " + platform.Name + " is in database.");
                item = Models.PlatformMapping.GetPlatformMap((long)platform.Id);
                // exists - skip
                Logging.Log(Logging.LogType.Information, "Platform Map", "Skipping import of " + platform.Name + " - already in database.");
            }
            catch
            {
                Logging.Log(Logging.LogType.Information, "Platform Map", "Importing " + platform.Name + " from predefined data.");
                // doesn't exist - add it
                item = new Models.PlatformMapping.PlatformMapItem
                {
                    IGDBId = (long)platform.Id,
                    IGDBName = platform.Name,
                    IGDBSlug = platform.Slug,
                    AlternateNames = new List<string> { platform.AlternativeName }
                };
                Models.PlatformMapping.WritePlatformMap(item, false, true);
            }
        }

        private enum SearchUsing
        {
            Id,
            Slug
        }

        private static async Task<Platform> GetObjectFromServer(string Slug)
        {
            // get platform metadata
            Communications comms = new Communications();
            var results = await comms.APIComm<Platform>(Communications.MetadataEndpoint.Platform, Slug);
            var result = results.First();

            return result;
        }

        private static async Task<Platform> GetObjectFromServer(long Id)
        {
            // get platform metadata
            Communications comms = new Communications();
            var results = await comms.APIComm<Platform>(Communications.MetadataEndpoint.Platform, Id);
            var result = results.First();

            return result;
        }

        public static void AssignAllPlatformsToGameIdZero()
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "SELECT * FROM Platform;";
            DataTable platformsTable = db.ExecuteCMD(sql);
            foreach (DataRow platformRow in platformsTable.Rows)
            {
                sql = "DELETE FROM Relation_Game_Platforms WHERE GameId = 0 AND PlatformsId = @Id; INSERT INTO Relation_Game_Platforms (GameId, PlatformsId) VALUES (0, @Id);";
                Dictionary<string, object> dbDict = new Dictionary<string, object>();
                dbDict.Add("Id", (long)platformRow["Id"]);
                db.ExecuteCMD(sql, dbDict);
            }
        }
    }
}

