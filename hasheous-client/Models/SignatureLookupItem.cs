namespace HasheousClient.Models
{
    public class SignatureLookupItem
    {
        public LookupResponseModel? Signature { get; set; }
        public List<MetadataResult>? MetadataResults { get; set; }

        public class MetadataResult
        {
            public object PlatformId { get; set; }
            public MatchMethod PlatformMatchMethod { get; set; }
            public object GameId { get; set; }
            public MatchMethod GameMatchMethod { get; set; }
            public HasheousClient.Models.MetadataModel.MetadataSources Source { get; set; }
        }
    }

    /// <summary>
    /// The method used to match the signature to the IGDB source
    /// </summary>
    public enum MatchMethod
    {
        /// <summary>
        /// No match
        /// </summary>
        NoMatch = 0,

        /// <summary>
        /// Automatic matches are subject to change - depending on IGDB
        /// </summary>
        Automatic = 1,

        /// <summary>
        /// Manual matches will never change
        /// </summary>
        Manual = 2,

        /// <summary>
        /// Too many matches to successfully match
        /// </summary>
        AutomaticTooManyMatches = 3
    }
}