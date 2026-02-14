using System;
using System.Data;
using System.Threading.Tasks;
using gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes;

namespace gaseous_server.Classes.Metadata
{
    public class PlatformVersions
    {
        public PlatformVersions()
        {
        }

        public static async Task<PlatformVersion?> GetPlatformVersion(FileSignature.MetadataSources SourceType, long Id)
        {
            if (Id == 0)
            {
                return null;
            }
            else
            {
                PlatformVersion? RetVal = await Metadata.GetMetadataAsync<PlatformVersion>(SourceType, Id, false);
                return RetVal;
            }
        }
    }
}

