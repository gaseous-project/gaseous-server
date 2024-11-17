using System;
using HasheousClient.Models.Metadata.IGDB;


namespace gaseous_server.Classes.Metadata
{
    public class GameModes
    {
        public const string fieldList = "fields checksum,created_at,name,slug,updated_at,url;";

        public GameModes()
        {
        }

        public static GameMode? GetGame_Modes(HasheousClient.Models.MetadataModel.MetadataSources SourceType, long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                GameMode? RetVal = Metadata.GetMetadata<GameMode>(SourceType, (long)Id, false);
                return RetVal;
            }
        }
    }
}

