using System;
using System.Data;
using System.Net;
using System.Threading.Tasks;
using gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes;

namespace gaseous_server.Classes.Metadata
{
    public class Platforms
    {
        public Platforms()
        {

        }

        public static async Task<Platform?> GetPlatform(long Id, FileSignature.MetadataSources? SourceType = null)
        {
            FileSignature.MetadataSources Source = SourceType ?? Config.MetadataConfiguration.DefaultMetadataSource;

            if ((Id == 0) || (Id == null))
            {
                Platform returnValue = new Platform();
                if (await Storage.GetCacheStatusAsync(Source, "Platform", 0) == Storage.CacheStatus.NotPresent)
                {
                    returnValue = new Platform
                    {
                        Id = 0,
                        Name = "Unknown Platform",
                        Slug = "unknown"
                    };
                    await Storage.NewCacheValue(Source, returnValue);

                    return returnValue;
                }
                else
                {
                    return await Storage.GetCacheValue<Platform>(Source, returnValue, "id", 0);
                }
            }
            else
            {
                Platform? RetVal = new Platform();
                RetVal = (Platform?)await Storage.GetCacheValue<Platform>(FileSignature.MetadataSources.None, RetVal, "Id", (long)Id);
                if (Source != FileSignature.MetadataSources.None)
                {
                    if (RetVal == null)
                    {
                        RetVal = await Metadata.GetMetadataAsync<Platform>(Source, (long)Id, false);
                    }
                }
                return RetVal;
            }
        }
    }
}

