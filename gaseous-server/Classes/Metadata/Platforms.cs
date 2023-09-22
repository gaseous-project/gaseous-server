using System;
using System.Data;
using System.Net;
using gaseous_tools;
using IGDB;
using IGDB.Models;

namespace gaseous_server.Classes.Metadata
{
    public class Platforms
	{
        const string fieldList = "fields abbreviation,alternative_name,category,checksum,created_at,generation,name,platform_family,platform_logo,slug,summary,updated_at,url,versions,websites;";

        public Platforms()
		{

		}

        private static IGDBClient igdb = new IGDBClient(
                    // Found in Twitch Developer portal for your app
                    Config.IGDB.ClientId,
                    Config.IGDB.Secret
                );

        public static Platform? GetPlatform(long Id, bool forceRefresh = false)
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
                Task<Platform> RetVal = _GetPlatform(SearchUsing.id, Id, forceRefresh);
                return RetVal.Result;
            }
        }

		public static Platform GetPlatform(string Slug, bool forceRefresh = false)
		{
			Task<Platform> RetVal = _GetPlatform(SearchUsing.slug, Slug, forceRefresh);
			return RetVal.Result;
		}

		private static async Task<Platform> _GetPlatform(SearchUsing searchUsing, object searchValue, bool forceRefresh)
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

            Platform returnValue = new Platform();
            switch (cacheStatus)
            {
                case Storage.CacheStatus.NotPresent:
                    returnValue = await GetObjectFromServer(WhereClause);
                    Storage.NewCacheValue(returnValue);
                    UpdateSubClasses(returnValue);
                    AddPlatformMapping(returnValue);
                    return returnValue;
                case Storage.CacheStatus.Expired:
                    try
                    {
                        returnValue = await GetObjectFromServer(WhereClause);
                        Storage.NewCacheValue(returnValue, true);
                        UpdateSubClasses(returnValue);
                        AddPlatformMapping(returnValue);
                        return returnValue;
                    }
                    catch (Exception ex)
                    {
                        Logging.Log(Logging.LogType.Warning, "Metadata: " + returnValue.GetType().Name, "An error occurred while connecting to IGDB. WhereClause: " + WhereClause, ex);
                        return Storage.GetCacheValue<Platform>(returnValue, "id", (long)searchValue);
                    }
                case Storage.CacheStatus.Current:
                    return Storage.GetCacheValue<Platform>(returnValue, "id", (long)searchValue);
                default:
                    throw new Exception("How did you get here?");
            }
        }

        private static void UpdateSubClasses(Platform platform)
        {
            if (platform.Versions != null)
            {
                foreach (long PlatformVersionId in platform.Versions.Ids)
                {
                    PlatformVersion platformVersion = PlatformVersions.GetPlatformVersion(PlatformVersionId, platform);
                }
            }

            if (platform.PlatformLogo != null)
            {
                PlatformLogo platformLogo = PlatformLogos.GetPlatformLogo(platform.PlatformLogo.Id, Config.LibraryConfiguration.LibraryMetadataDirectory_Platform(platform));
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
                item = new Models.PlatformMapping.PlatformMapItem{
                    IGDBId = (long)platform.Id,
                    IGDBName = platform.Name,
                    IGDBSlug = platform.Slug,
                    AlternateNames = new List<string>{ platform.AlternativeName }
                };
                Models.PlatformMapping.WritePlatformMap(item, false);
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
            var results = await igdb.QueryAsync<Platform>(IGDBClient.Endpoints.Platforms, query: fieldList + " " + WhereClause + ";");
            var result = results.First();

            return result;
        }
    }
}

