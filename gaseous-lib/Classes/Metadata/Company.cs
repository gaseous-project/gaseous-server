using System;
using System.Threading.Tasks;
using gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes;

namespace gaseous_server.Classes.Metadata
{
    public class Companies
    {
        public Companies()
        {
        }

        public static async Task<Company?> GetCompanies(FileSignature.MetadataSources SourceType, long? Id)
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

