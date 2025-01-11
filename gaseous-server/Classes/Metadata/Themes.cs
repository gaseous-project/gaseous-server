using System;
using HasheousClient.Models.Metadata.IGDB;


namespace gaseous_server.Classes.Metadata
{
    public class Themes
    {
        public const string fieldList = "fields checksum,created_at,name,slug,updated_at,url;";

        public Themes()
        {
        }

        public static Theme? GetGame_Themes(HasheousClient.Models.MetadataSources SourceType, long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Theme? RetVal = Metadata.GetMetadata<Theme>(SourceType, (long)Id, false);
                return RetVal;
            }
        }
    }
}

