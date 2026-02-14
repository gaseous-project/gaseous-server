using System;
using System.Threading.Tasks;
using gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes;

namespace gaseous_server.Classes.Metadata
{
    public class GameLocalizations
    {
        public GameLocalizations()
        {
        }

        public static async Task<GameLocalization?> GetGame_Localization(FileSignature.MetadataSources SourceType, long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                GameLocalization? RetVal = await Metadata.GetMetadataAsync<GameLocalization>(SourceType, (long)Id, false);

                return RetVal;
            }
        }
    }
}