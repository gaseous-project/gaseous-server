namespace gaseous_server.Models
{
    public class FixMatchModel
    {
        public class MetadataMatch
        {
            public HasheousClient.Models.MetadataSources Source { get; set; }

            public string PlatformId { get; set; }

            public string GameId { get; set; }

            public MetadataMatch()
            {
            }

            public MetadataMatch(HasheousClient.Models.MetadataSources source, string platformId, string gameId)
            {
                Source = source;
                PlatformId = platformId;
                GameId = gameId;
            }
        }

        public string MD5 { get; set; }
        public string SHA1 { get; set; }
        public string SHA256 { get; set; }
        public string CRC { get; set; }

        public List<MetadataMatch>? MetadataMatches { get; set; }

        public FixMatchModel()
        {
        }

        public FixMatchModel(string md5, string sha1, List<MetadataMatch> metadataMatches)
        {
            MD5 = md5;
            SHA1 = sha1;
            MetadataMatches = metadataMatches;
        }
    }
}