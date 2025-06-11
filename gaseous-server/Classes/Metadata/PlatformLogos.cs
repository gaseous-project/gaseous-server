using System;
using System.Threading.Tasks;
using HasheousClient.Models.Metadata.IGDB;
using static gaseous_server.Models.PlatformMapping;


namespace gaseous_server.Classes.Metadata
{
    public class PlatformLogos
    {
        public PlatformLogos()
        {
        }

        public static async Task<PlatformLogo?> GetPlatformLogo(long? Id, HasheousClient.Models.MetadataSources SourceType = HasheousClient.Models.MetadataSources.IGDB)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                PlatformLogo? RetVal = await Metadata.GetMetadataAsync<PlatformLogo>(SourceType, (long)Id, false);
                return RetVal;
            }
        }
    }
}

