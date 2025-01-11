using System;
using System.Data;
using System.Net;
using HasheousClient.Models.Metadata.IGDB;

namespace gaseous_server.Classes.Metadata
{
    public class Platforms
    {
        public const string fieldList = "fields abbreviation,alternative_name,category,checksum,created_at,generation,name,platform_family,platform_logo,slug,summary,updated_at,url,versions,websites;";

        public Platforms()
        {

        }

        public static Platform? GetPlatform(long Id, HasheousClient.Models.MetadataSources? SourceType = null)
        {
            HasheousClient.Models.MetadataSources Source = SourceType ?? Communications.MetadataSource;

            if ((Id == 0) || (Id == null))
            {
                Platform returnValue = new Platform();
                if (Storage.GetCacheStatus(Source, "Platform", 0) == Storage.CacheStatus.NotPresent)
                {
                    returnValue = new Platform
                    {
                        Id = 0,
                        Name = "Unknown Platform",
                        Slug = "unknown"
                    };
                    Storage.NewCacheValue(Source, returnValue);

                    return returnValue;
                }
                else
                {
                    return Storage.GetCacheValue<Platform>(Source, returnValue, "id", 0);
                }
            }
            else
            {
                Platform? RetVal = new Platform();
                RetVal = (Platform?)Storage.GetCacheValue<Platform>(HasheousClient.Models.MetadataSources.None, RetVal, "Id", (long)Id);
                if (Source != HasheousClient.Models.MetadataSources.None)
                {
                    if (RetVal == null)
                    {
                        RetVal = Metadata.GetMetadata<Platform>(Source, (long)Id, false);
                    }
                }
                return RetVal;
            }
        }

        public static Platform GetPlatform(string Slug)
        {
            // get platform id from slug - query Platform table
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string query = "SELECT Id FROM Platform WHERE slug = @slug AND SourceId = @sourceid;";
            DataTable result = db.ExecuteCMD(query, new Dictionary<string, object> { { "@slug", Slug }, { "@sourceid", HasheousClient.Models.MetadataSources.IGDB } });
            if (result.Rows.Count == 0)
            {
                throw new Metadata.InvalidMetadataId(Slug);
            }

            long Id = (long)result.Rows[0]["Id"];
            return GetPlatform(Id);
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
    }
}

