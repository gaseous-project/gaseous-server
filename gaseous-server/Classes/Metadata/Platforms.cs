using System;
using System.Data;
using System.Net;
using IGDB;
using IGDB.Models;

namespace gaseous_server.Classes.Metadata
{
    public class Platforms
    {
        const string fieldList = "fields *;";

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
                    Task<Platform> RetVal = _GetPlatform(SearchUsing.id, Id, forceRefresh, GetImages);
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
            Task<Platform> RetVal = _GetPlatform(SearchUsing.slug, Slug, forceRefresh, GetImages);
            return RetVal.Result;
        }

        private static async Task<Platform> _GetPlatform(SearchUsing searchUsing, object searchValue, bool forceRefresh, bool GetImages)
        {
            // check database first
            Storage.CacheStatus? cacheStatus = new Storage.CacheStatus();
            if (searchUsing == SearchUsing.id)
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

            // set up where clause
            string WhereClause = "";
            string searchField = "";
            switch (searchUsing)
            {
                case SearchUsing.id:
                    WhereClause = "where id = " + searchValue;
                    searchField = "id";
                    break;
                case SearchUsing.slug:
                    WhereClause = "where slug = \"" + searchValue + "\"";
                    searchField = "slug";
                    break;
                default:
                    throw new Exception("Invalid search type");
            }

            Platform returnValue = new Platform();
            switch (cacheStatus)
            {
                case Storage.CacheStatus.NotPresent:
                    returnValue = await GetObjectFromServer(WhereClause);
                    Storage.NewCacheValue(returnValue);
                    UpdateSubClasses(returnValue, GetImages);
                    AddPlatformMapping(returnValue);
                    return returnValue;
                case Storage.CacheStatus.Expired:
                    try
                    {
                        returnValue = await GetObjectFromServer(WhereClause);
                        Storage.NewCacheValue(returnValue, true);
                        UpdateSubClasses(returnValue, GetImages);
                        AddPlatformMapping(returnValue);
                        return returnValue;
                    }
                    catch (Exception ex)
                    {
                        Logging.Log(Logging.LogType.Warning, "Metadata: " + returnValue.GetType().Name, "An error occurred while connecting to IGDB. WhereClause: " + WhereClause, ex);
                        return Storage.GetCacheValue<Platform>(returnValue, searchField, searchValue);
                    }
                case Storage.CacheStatus.Current:
                    return Storage.GetCacheValue<Platform>(returnValue, searchField, searchValue);
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
            id,
            slug
        }

        private static async Task<Platform> GetObjectFromServer(string WhereClause)
        {
            // get platform metadata
            Communications comms = new Communications();
            var results = await comms.APIComm<Platform>(IGDBClient.Endpoints.Platforms, fieldList, WhereClause);
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

