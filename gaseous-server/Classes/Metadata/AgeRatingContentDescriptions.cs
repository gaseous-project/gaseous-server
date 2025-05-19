using System;
using System.Threading.Tasks;
using HasheousClient.Models.Metadata.IGDB;


namespace gaseous_server.Classes.Metadata
{
    public class AgeRatingContentDescriptions
    {
        public const string fieldList = "fields category,checksum,description;";

        public AgeRatingContentDescriptions()
        {
        }

        public static async Task<AgeRatingContentDescription?> GetAgeRatingContentDescriptions(HasheousClient.Models.MetadataSources SourceType, long? Id)
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

