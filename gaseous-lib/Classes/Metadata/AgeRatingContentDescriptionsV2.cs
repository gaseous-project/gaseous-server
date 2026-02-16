using System;
using System.Threading.Tasks;
using gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes;


namespace gaseous_server.Classes.Metadata
{
    public class AgeRatingContentDescriptions
    {
        public AgeRatingContentDescriptions()
        {
        }

        public static async Task<AgeRatingContentDescription?> GetAgeRatingContentDescriptions(FileSignature.MetadataSources SourceType, long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                AgeRatingContentDescription? RetVal = await Metadata.GetMetadataAsync<AgeRatingContentDescription>(SourceType, (long)Id, false);
                return RetVal;
            }
        }
    }
}

