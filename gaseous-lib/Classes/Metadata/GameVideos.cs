using System;
using System.Threading.Tasks;
using gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes;


namespace gaseous_server.Classes.Metadata
{
    public class GamesVideos
    {
        public GamesVideos()
        {
        }

        public static async Task<GameVideo?> GetGame_Videos(FileSignature.MetadataSources SourceType, long? Id)
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

