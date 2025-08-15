using System;
using System.Threading.Tasks;
using HasheousClient.Models.Metadata.IGDB;

namespace gaseous_server.Classes.Metadata
{
    public class Covers
    {
        public Covers()
        {
        }

        public static async Task<Cover?> GetCover(FileSignature.MetadataSources SourceType, long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Cover? RetVal = await Metadata.GetMetadataAsync<Cover>(SourceType, (long)Id, false);
                return RetVal;
            }
        }
    }
}

