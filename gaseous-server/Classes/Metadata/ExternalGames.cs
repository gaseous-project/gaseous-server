using System;
using HasheousClient.Models.Metadata.IGDB;

namespace gaseous_server.Classes.Metadata
{
    public class ExternalGames
    {
        public const string fieldList = "fields category,checksum,countries,created_at,game,media,name,platform,uid,updated_at,url,year;";

        public ExternalGames()
        {
        }

        public static ExternalGame? GetExternalGames(HasheousClient.Models.MetadataModel.MetadataSources SourceType, long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                ExternalGame? RetVal = Metadata.GetMetadata<ExternalGame>(SourceType, (long)Id, false);
                return RetVal;
            }
        }
    }
}

