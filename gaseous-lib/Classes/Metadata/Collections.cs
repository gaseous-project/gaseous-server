using System;
using System.Threading.Tasks;
using gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes;

namespace gaseous_server.Classes.Metadata
{
    public class Collections
    {
        public Collections()
        {
        }

        public static async Task<Collection?> GetCollections(FileSignature.MetadataSources SourceType, long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Collection? RetVal = await Metadata.GetMetadataAsync<Collection>(SourceType, (long)Id, false);
                return RetVal;
            }
        }
    }
}

