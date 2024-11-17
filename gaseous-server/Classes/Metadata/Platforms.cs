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

        public static Platform? GetPlatform(long Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Platform? RetVal = Metadata.GetMetadata<Platform>(Communications.MetadataSource, (long)Id, false);
                return RetVal;
            }
        }

        public static Platform GetPlatform(string Slug, bool forceRefresh = false, bool GetImages = false)
        {
            throw new NotImplementedException();
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

