using System;
using HasheousClient.Models.Metadata.IGDB;


namespace gaseous_server.Classes.Metadata
{
    public class PlatformLogos
    {
        public const string fieldList = "fields alpha_channel,animated,checksum,height,image_id,url,width;";

        public PlatformLogos()
        {
        }

        public static PlatformLogo? GetPlatformLogo(long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                PlatformLogo? RetVal = Metadata.GetMetadata<PlatformLogo>(HasheousClient.Models.MetadataSources.IGDB, (long)Id, false);
                return RetVal;
            }
        }
    }
}

