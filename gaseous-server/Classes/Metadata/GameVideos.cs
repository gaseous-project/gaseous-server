using System;
using HasheousClient.Models.Metadata.IGDB;


namespace gaseous_server.Classes.Metadata
{
    public class GamesVideos
    {
        public const string fieldList = "fields checksum,game,name,video_id;";

        public GamesVideos()
        {
        }

        public static GameVideo? GetGame_Videos(HasheousClient.Models.MetadataSources SourceType, long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                GameVideo? RetVal = Metadata.GetMetadata<GameVideo>(SourceType, (long)Id, false);
                return RetVal;
            }
        }
    }
}

