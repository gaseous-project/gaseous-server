using System;
using HasheousClient.Models.Metadata.IGDB;

namespace gaseous_server.Classes.Metadata
{
    public class Companies
    {
        public const string fieldList = "fields change_date,change_date_category,changed_company_id,checksum,country,created_at,description,developed,logo,name,parent,published,slug,start_date,start_date_category,updated_at,url,websites;";

        public Companies()
        {
        }

        public static Company? GetCompanies(HasheousClient.Models.MetadataModel.MetadataSources SourceType, long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Company? RetVal = Metadata.GetMetadata<Company>(SourceType, (long)Id, false);
                return RetVal;
            }
        }
    }
}

