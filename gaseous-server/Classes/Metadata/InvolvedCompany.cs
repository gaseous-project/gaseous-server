using System;
using System.Threading.Tasks;
using HasheousClient.Models.Metadata.IGDB;

namespace gaseous_server.Classes.Metadata
{
    public class InvolvedCompanies
    {
        public const string fieldList = "fields *;";

        public InvolvedCompanies()
        {
        }

        public static async Task<InvolvedCompany?> GetInvolvedCompanies(long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                InvolvedCompany? RetVal = await Metadata.GetMetadataAsync<InvolvedCompany>(HasheousClient.Models.MetadataSources.IGDB, (long)Id, false);
                return RetVal;
            }
        }
    }
}

