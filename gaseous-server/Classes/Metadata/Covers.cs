using System;
using HasheousClient.Models.Metadata.IGDB;

namespace gaseous_server.Classes.Metadata
{
    public class Covers
    {
        public const string fieldList = "fields alpha_channel,animated,checksum,game,game_localization,height,image_id,url,width;";

        public Covers()
        {
        }

        public static Cover? GetCover(HasheousClient.Models.MetadataModel.MetadataSources SourceType, long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Cover? RetVal = Metadata.GetMetadata<Cover>(SourceType, (long)Id, false);
                return RetVal;
            }
        }
    }
}

