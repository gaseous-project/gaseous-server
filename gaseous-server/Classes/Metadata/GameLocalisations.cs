using System;
using System.Threading.Tasks;
using HasheousClient.Models.Metadata.IGDB;

namespace gaseous_server.Classes.Metadata
{
    public class GameLocalizations
    {
        public GameLocalizations()
        {
        }

        public static async Task<GameLocalization?> GetGame_Locatization(FileSignature.MetadataSources SourceType, long? Id)
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