using System;
using HasheousClient.Models.Metadata.IGDB;


namespace gaseous_server.Classes.Metadata
{
    public class ReleaseDates
    {
        public const string fieldList = "fields category,checksum,created_at,date,game,human,m,platform,region,status,updated_at,y;";

        public ReleaseDates()
        {
        }

        public static ReleaseDate? GetReleaseDates(HasheousClient.Models.MetadataModel.MetadataSources SourceType, long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                ReleaseDate? RetVal = Metadata.GetMetadata<ReleaseDate>(SourceType, (long)Id, false);
                return RetVal;
            }
        }
    }
}

