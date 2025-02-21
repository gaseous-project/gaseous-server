using System;
using HasheousClient.Models.Metadata.IGDB;

namespace gaseous_server.Classes.Metadata
{
    public class CompanyLogos
    {
        public const string fieldList = "fields alpha_channel,animated,checksum,height,image_id,url,width;";

        public CompanyLogos()
        {
        }

        public static CompanyLogo? GetCompanyLogo(long? Id, string ImagePath)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                CompanyLogo? RetVal = Metadata.GetMetadata<CompanyLogo>((long)Id, false);
                return RetVal;
            }
        }
    }
}

