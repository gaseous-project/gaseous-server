using System;
using System.Threading.Tasks;
using gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes;


namespace gaseous_server.Classes.Metadata
{
    public class AgeRatingOrganizations
    {
        public AgeRatingOrganizations()
        {
        }

        public static async Task<AgeRatingOrganization?> GetAgeRatingOrganization(FileSignature.MetadataSources SourceType, long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                AgeRatingOrganization? RetVal = await Metadata.GetMetadataAsync<AgeRatingOrganization>(SourceType, (long)Id, false);
                return RetVal;
            }
        }
    }
}