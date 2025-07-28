using System;
using System.Threading.Tasks;
using HasheousClient.Models.Metadata.IGDB;


namespace gaseous_server.Classes.Metadata
{
    public class AgeRatingCategorys
    {
        public AgeRatingCategorys()
        {
        }

        public static async Task<AgeRatingCategory?> GetAgeRatingCategory(FileSignature.MetadataSources SourceType, long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                AgeRatingCategory? RetVal = await Metadata.GetMetadataAsync<AgeRatingCategory>(SourceType, (long)Id, false);
                return RetVal;
            }
        }
    }
}