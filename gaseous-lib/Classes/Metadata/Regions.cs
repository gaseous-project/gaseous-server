using System;
using System.Threading.Tasks;
using gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes;

namespace gaseous_server.Classes.Metadata
{
    public class Regions
    {
        public Regions()
        {
        }

        public static async Task<Region?> GetGame_Region(FileSignature.MetadataSources SourceType, long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Region? RetVal = await Metadata.GetMetadataAsync<Region>(SourceType, (long)Id, false);

                return RetVal;
            }
        }
    }
}