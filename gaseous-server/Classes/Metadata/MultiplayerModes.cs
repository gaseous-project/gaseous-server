using System;
using HasheousClient.Models.Metadata.IGDB;


namespace gaseous_server.Classes.Metadata
{
    public class MultiplayerModes
    {
        public const string fieldList = "fields campaigncoop,checksum,dropin,game,lancoop,offlinecoop,offlinecoopmax,offlinemax,onlinecoop,onlinecoopmax,onlinemax,platform,splitscreen,splitscreenonline;";

        public MultiplayerModes()
        {
        }

        public static MultiplayerMode? GetGame_MultiplayerModes(HasheousClient.Models.MetadataSources SourceType, long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                MultiplayerMode? RetVal = Metadata.GetMetadata<MultiplayerMode>(SourceType, (long)Id, false);
                return RetVal;
            }
        }
    }
}

