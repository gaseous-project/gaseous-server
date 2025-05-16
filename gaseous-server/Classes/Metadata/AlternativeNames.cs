using System;
using System.Threading.Tasks;
using HasheousClient.Models.Metadata.IGDB;


namespace gaseous_server.Classes.Metadata
{
    public class AlternativeNames
    {
        public const string fieldList = "fields checksum,comment,game,name;";

        public AlternativeNames()
        {
        }

        public static async Task<AlternativeName?> GetAlternativeNames(HasheousClient.Models.MetadataSources SourceType, long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                AlternativeName? RetVal = await Metadata.GetMetadataAsync<AlternativeName>(SourceType, (long)Id, false);
                return RetVal;
            }
        }
    }
}

