using System;
using System.Threading.Tasks;
using HasheousClient.Models.Metadata.IGDB;

namespace gaseous_server.Classes.Metadata
{
    public class GameLocalizations
    {
        public const string fieldList = "fields checksum,cover,created_at,game,name,region,updated_at;";

        public GameLocalizations()
        {
        }

        public static async Task<GameLocalization?> GetGame_Locatization(HasheousClient.Models.MetadataSources SourceType, long? Id)
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