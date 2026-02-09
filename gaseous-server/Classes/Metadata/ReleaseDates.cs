using System;
using System.Threading.Tasks;
using gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes;


namespace gaseous_server.Classes.Metadata
{
    public class ReleaseDates
    {
        public ReleaseDates()
        {
        }

        public static async Task<ReleaseDate?> GetReleaseDates(FileSignature.MetadataSources SourceType, long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                ReleaseDate? RetVal = await Metadata.GetMetadataAsync<ReleaseDate>(SourceType, (long)Id, false);
                return RetVal;
            }
        }
    }
}

