using System;
using System.Threading.Tasks;
using HasheousClient.Models.Metadata.IGDB;

namespace gaseous_server.Classes.Metadata
{
    public class Collections
    {
        public const string fieldList = "fields as_child_relations,as_parent_relations,checksum,created_at,games,name,slug,type,updated_at,url;";

        public Collections()
        {
        }

        public static async Task<Collection?> GetCollections(HasheousClient.Models.MetadataSources SourceType, long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Collection? RetVal = await Metadata.GetMetadataAsync<Collection>(SourceType, (long)Id, false);
                return RetVal;
            }
        }
    }
}

