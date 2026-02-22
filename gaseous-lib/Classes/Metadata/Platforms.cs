using System;
using System.Data;
using System.Net;
using System.Threading.Tasks;
using gaseous_server.Classes.Plugins.MetadataProviders;
using gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes;

namespace gaseous_server.Classes.Metadata
{
    public class Platforms
    {
        public Platforms()
        {

        }

        public static async Task<Platform?> GetPlatform(long Id, FileSignature.MetadataSources? SourceType = null, bool ForceRefresh = false)
        {
            FileSignature.MetadataSources Source = SourceType ?? Config.MetadataConfiguration.DefaultMetadataSource;

            if ((Id == 0) || (Id == null))
            {
                Platform returnValue = new Platform();
                Storage storage = new Storage(FileSignature.MetadataSources.None);
                if (await storage.GetCacheStatusAsync("Platform", 0) == Storage.CacheStatus.NotPresent)
                {
                    returnValue = new Platform
                    {
                        Id = 0,
                        Name = "Unknown Platform",
                        Slug = "unknown"
                    };
                    await storage.StoreCacheValue<Platform>(returnValue);

                    return returnValue;
                }
                else
                {
                    return await storage.GetCacheValue<Platform>(returnValue, "id", 0);
                }
            }
            else
            {
                return await Metadata.GetMetadataAsync<Platform>(Source, (long)Id, ForceRefresh);
            }
        }
    }
}

