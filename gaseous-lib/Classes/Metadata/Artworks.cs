using System;
using System.Threading.Tasks;
using gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes;

namespace gaseous_server.Classes.Metadata
{
    public class Artworks
    {
        public Artworks()
        {
        }

        public static async Task<Artwork?> GetArtwork(FileSignature.MetadataSources SourceType, long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Artwork? RetVal = await Metadata.GetMetadataAsync<Artwork>(SourceType, (long)Id, false);
                return RetVal;
            }
        }
    }
}

