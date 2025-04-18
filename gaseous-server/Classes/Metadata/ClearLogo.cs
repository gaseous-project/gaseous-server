using System;
using HasheousClient.Models.Metadata.IGDB;

namespace gaseous_server.Classes.Metadata
{
    public class ClearLogos
    {
        public ClearLogos()
        {
        }

        public static ClearLogo? GetClearLogo(HasheousClient.Models.MetadataSources SourceType, long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                ClearLogo? RetVal = Metadata.GetMetadata<ClearLogo>(SourceType, (long)Id, false);
                return RetVal;
            }
        }
    }
}