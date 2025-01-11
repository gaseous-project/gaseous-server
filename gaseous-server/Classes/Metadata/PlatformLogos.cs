using System;
using HasheousClient.Models.Metadata.IGDB;
using static gaseous_server.Models.PlatformMapping;


namespace gaseous_server.Classes.Metadata
{
    public class PlatformLogos
    {
        public const string fieldList = "fields alpha_channel,animated,checksum,height,image_id,url,width;";

        public PlatformLogos()
        {
        }

        public static PlatformLogo? GetPlatformLogo(long? Id, HasheousClient.Models.MetadataSources SourceType = HasheousClient.Models.MetadataSources.IGDB)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                PlatformLogo? RetVal = Metadata.GetMetadata<PlatformLogo>(SourceType, (long)Id, false);
                return RetVal;
            }
        }
    }
}

