using System;
using HasheousClient.Models.Metadata.IGDB;


namespace gaseous_server.Classes.Metadata
{
    public class Genres
    {
        public const string fieldList = "fields checksum,created_at,name,slug,updated_at,url;";

        public Genres()
        {
        }

        public static Genre? GetGenres(HasheousClient.Models.MetadataModel.MetadataSources SourceType, long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Genre? RetVal = Metadata.GetMetadata<Genre>(SourceType, (long)Id, false);
                return RetVal;
            }
        }
    }
}

