using System;
using System.Threading.Tasks;
using HasheousClient.Models.Metadata.IGDB;


namespace gaseous_server.Classes.Metadata
{
    public class AgeRatingContentDescriptionsV2
    {
        public AgeRatingContentDescriptionsV2()
        {
        }

        public static async Task<AgeRatingContentDescriptionV2?> GetAgeRatingContentDescriptionsV2(FileSignature.MetadataSources SourceType, long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                AgeRatingContentDescriptionV2? RetVal = await Metadata.GetMetadataAsync<AgeRatingContentDescriptionV2>(SourceType, (long)Id, false);
                return RetVal;
            }
        }
    }
}

