using System;
using HasheousClient.Models.Metadata.IGDB;


namespace gaseous_server.Classes.Metadata
{
    public class AgeRatingContentDescriptions
    {
        public const string fieldList = "fields category,checksum,description;";

        public AgeRatingContentDescriptions()
        {
        }

        public static AgeRatingContentDescription? GetAgeRatingContentDescriptions(HasheousClient.Models.MetadataSources SourceType, long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                AgeRatingContentDescription? RetVal = Metadata.GetMetadata<AgeRatingContentDescription>(SourceType, (long)Id, false);
                return RetVal;
            }
        }
    }
}

