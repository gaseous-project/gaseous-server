using System;
using System.Threading.Tasks;
using HasheousClient.Models.Metadata.IGDB;


namespace gaseous_server.Classes.Metadata
{
    public class GamesVideos
    {
        public const string fieldList = "fields checksum,game,name,video_id;";

        public GamesVideos()
        {
        }

        public static async Task<GameVideo?> GetGame_Videos(HasheousClient.Models.MetadataSources SourceType, long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                GameVideo? RetVal = await Metadata.GetMetadataAsync<GameVideo>(SourceType, (long)Id, false);
                return RetVal;
            }
        }
    }
}

