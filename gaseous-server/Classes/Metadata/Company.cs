using System;
using System.Threading.Tasks;
using HasheousClient.Models.Metadata.IGDB;

namespace gaseous_server.Classes.Metadata
{
    public class Companies
    {
        public Companies()
        {
        }

        public static async Task<Company?> GetCompanies(HasheousClient.Models.MetadataSources SourceType, long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Company? RetVal = await Metadata.GetMetadataAsync<Company>(SourceType, (long)Id, false);
                return RetVal;
            }
        }
    }
}

