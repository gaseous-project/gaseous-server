using System;
using System.Threading.Tasks;
using HasheousClient.Models.Metadata.IGDB;

namespace gaseous_server.Classes.Metadata
{
    public class Franchises
    {
        public Franchises()
        {
        }

        public static async Task<Franchise?> GetFranchises(FileSignature.MetadataSources SourceType, long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Franchise? RetVal = await Metadata.GetMetadataAsync<Franchise>(SourceType, (long)Id, false);
                return RetVal;
            }
        }
    }
}

