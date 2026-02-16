namespace HasheousClient.Models
{
    public class MetadataModel
    {
        /// <summary>
        /// Supported metadata sources
        /// </summary>
        public enum MetadataSources
        {
            /// <summary>
            /// None - always returns null for metadata requests - should not really be using this source
            /// </summary>
            None,

            /// <summary>
            /// IGDB - queries the IGDB service for metadata
            /// </summary>
            IGDB,

            /// <summary>
            /// Hasheous - queries the specified hasheous server for metadata
            /// </summary>
            Hasheous
        }

        /// <summary>
        /// Support signature sources
        /// </summary>
        public enum SignatureSources
        {
            /// <summary>
            /// Uses only the local database - ensure that DAT's have been loaded in order to match ROMs
            /// </summary>
            LocalOnly,

            /// <summary>
            /// Hasheous - queries the specified hasheous server for signatures
            /// </summary>
            Hasheous
        }
    }
}