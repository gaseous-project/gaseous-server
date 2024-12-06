using HasheousClient.Models;

namespace gaseous_server.Models
{
    public class MetadataMap
    {
        public long Id { get; set; }
        public long PlatformId { get; set; }
        public string SignatureGameName { get; set; }
        public List<MetadataMapItem> MetadataMapItems { get; set; }

        public class MetadataMapItem
        {
            public HasheousClient.Models.MetadataSources SourceType { get; set; }
            public long SourceId { get; set; }
            public bool Preferred { get; set; }
        }
    }
}