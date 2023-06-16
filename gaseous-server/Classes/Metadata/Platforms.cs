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

        public static Platform? GetPlatform(long Id)
		{
            if (Id == 0)
            {
                Platform returnValue = new Platform();
                if (Storage.GetCacheStatus("platform", 0) == Storage.CacheStatus.NotPresent)
                {
                    returnValue = new Platform
                    {
                        Id = 0,
                        Name = "Unknown Platform"
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
                Task<Platform> RetVal = _GetPlatform(SearchUsing.id, Id);
                return RetVal.Result;
            }
        }

		public static Platform GetPlatform(string Slug)
		{
			Task<Platform> RetVal = _GetPlatform(SearchUsing.slug, Slug);
			return RetVal.Result;
		}

		private static async Task<Platform> _GetPlatform(SearchUsing searchUsing, object searchValue)
		{
            // check database first
            Storage.CacheStatus? cacheStatus = new Storage.CacheStatus();
            if (searchUsing == SearchUsing.id)
            {
                cacheStatus = Storage.GetCacheStatus("platform", (long)searchValue);
            }
            else
            {
                cacheStatus = Storage.GetCacheStatus("platform", (string)searchValue);
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
                    return returnValue;
                case Storage.CacheStatus.Expired:
                    returnValue = await GetObjectFromServer(WhereClause);
                    Storage.NewCacheValue(returnValue, true);
                    UpdateSubClasses(returnValue);
                    return returnValue;
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

