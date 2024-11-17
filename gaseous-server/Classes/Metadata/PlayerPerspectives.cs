using System;
using HasheousClient.Models.Metadata.IGDB;


namespace gaseous_server.Classes.Metadata
{
    public class PlayerPerspectives
    {
        public const string fieldList = "fields checksum,created_at,name,slug,updated_at,url;";

        public PlayerPerspectives()
        {
        }

        public static PlayerPerspective? GetGame_PlayerPerspectives(HasheousClient.Models.MetadataModel.MetadataSources SourceType, long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                PlayerPerspective? RetVal = Metadata.GetMetadata<PlayerPerspective>(SourceType, (long)Id, false);
                return RetVal;
            }
        }
    }
}

