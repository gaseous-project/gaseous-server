using System;
using System.Threading.Tasks;
using HasheousClient.Models.Metadata.IGDB;

namespace gaseous_server.Classes.Metadata
{
    public class Franchises
    {
        public const string fieldList = "fields checksum,created_at,games,name,slug,updated_at,url;";

        public Franchises()
        {
        }

        public static async Task<Franchise?> GetFranchises(HasheousClient.Models.MetadataSources SourceType, long? Id)
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

