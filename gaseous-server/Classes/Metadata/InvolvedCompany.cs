using System;
using System.Threading.Tasks;
using gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes;

namespace gaseous_server.Classes.Metadata
{
    public class InvolvedCompanies
    {
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
                InvolvedCompany? RetVal = await Metadata.GetMetadataAsync<InvolvedCompany>(FileSignature.MetadataSources.IGDB, (long)Id, false);
                return RetVal;
            }
        }
    }
}

