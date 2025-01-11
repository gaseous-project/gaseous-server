using System;
using HasheousClient.Models.Metadata.IGDB;


namespace gaseous_server.Classes.Metadata
{
    public class Screenshots
    {
        public const string fieldList = "fields alpha_channel,animated,checksum,game,height,image_id,url,width;";

        public Screenshots()
        {
        }

        public static Screenshot? GetScreenshot(HasheousClient.Models.MetadataSources SourceType, long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Screenshot? RetVal = Metadata.GetMetadata<Screenshot>(SourceType, (long)Id, false);
                return RetVal;
            }
        }
    }
}

