using System;
using HasheousClient.Models.Metadata.IGDB;

namespace gaseous_server.Classes.Metadata
{
    public class Collections
    {
        public const string fieldList = "fields as_child_relations,as_parent_relations,checksum,created_at,games,name,slug,type,updated_at,url;";

        public Collections()
        {
        }

        public static Collection? GetCollections(HasheousClient.Models.MetadataModel.MetadataSources SourceType, long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Collection? RetVal = Metadata.GetMetadata<Collection>(SourceType, (long)Id, false);
                return RetVal;
            }
        }
    }
}

