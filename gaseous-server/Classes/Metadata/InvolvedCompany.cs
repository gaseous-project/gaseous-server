using System;
using HasheousClient.Models.Metadata.IGDB;

namespace gaseous_server.Classes.Metadata
{
    public class InvolvedCompanies
    {
        public const string fieldList = "fields *;";

        public InvolvedCompanies()
        {
        }

        public static InvolvedCompany? GetInvolvedCompanies(long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                InvolvedCompany? RetVal = Metadata.GetMetadata<InvolvedCompany>(HasheousClient.Models.MetadataSources.IGDB, (long)Id, false);
                return RetVal;
            }
        }
    }
}

