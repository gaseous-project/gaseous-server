using System;
using System.Threading.Tasks;
using gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes;

namespace gaseous_server.Classes.Metadata
{
    public class ClearLogos
    {
        public ClearLogos()
        {
        }

        public static async Task<ClearLogo?> GetClearLogo(FileSignature.MetadataSources SourceType, long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                ClearLogo? RetVal = await Metadata.GetMetadataAsync<ClearLogo>(SourceType, (long)Id, false);
                return RetVal;
            }
        }
    }
}