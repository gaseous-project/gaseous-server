using System;
using System.Threading.Tasks;
using gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes;

namespace gaseous_server.Classes.Metadata
{
    public class ExternalGames
    {
        public ExternalGames()
        {
        }

        public static async Task<ExternalGame?> GetExternalGames(FileSignature.MetadataSources SourceType, long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                ExternalGame? RetVal = await Metadata.GetMetadataAsync<ExternalGame>(SourceType, (long)Id, false);
                return RetVal;
            }
        }
    }
}

