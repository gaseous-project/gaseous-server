using System;
using System.Threading.Tasks;
using gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes;


namespace gaseous_server.Classes.Metadata
{
    public class AlternativeNames
    {
        public AlternativeNames()
        {
        }

        public static async Task<AlternativeName?> GetAlternativeNames(FileSignature.MetadataSources SourceType, long? Id)
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

