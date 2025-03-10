using System;
using HasheousClient.Models.Metadata.IGDB;

namespace gaseous_server.Classes.Metadata
{
    public class Regions
    {
        public const string fieldList = "fields category,checksum,created_at,identifier,name,updated_at;";

        public Regions()
        {
        }

        public static Region? GetGame_Region(HasheousClient.Models.MetadataSources SourceType, long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Region? RetVal = Metadata.GetMetadata<Region>(SourceType, (long)Id, false);

                return RetVal;
            }
        }
    }
}