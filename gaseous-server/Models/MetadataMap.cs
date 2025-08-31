using gaseous_server.Classes;
using gaseous_server.Classes.Metadata;
using HasheousClient.Models;

namespace gaseous_server.Models
{
    /// <summary>
    /// Provides the list of metadata sources supported by the application.
    /// </summary>
    public static class MetadataMapSupportedDataSources
    {
        /// <summary>
        /// Gets the list of metadata sources supported by the application.
        /// </summary>
        /// <value>A list of supported metadata sources.</value>
        public static List<MetadataSources> SupportedMetadataSources
        {
            get
            {
                return new List<MetadataSources>
                {
                    MetadataSources.None,
                    MetadataSources.IGDB,
                    MetadataSources.TheGamesDb
                };
            }
        }
    }

    /// <summary>
    /// Represents a mapping between a game's signature and its associated metadata sources, including a preferred source.
    /// </summary>
    public class MetadataMap
    {
        public long Id { get; set; }
        public long PlatformId { get; set; }
        public string SignatureGameName { get; set; }
        public List<MetadataMapItem> MetadataMapItems { get; set; }
        public MetadataMapItem? PreferredMetadataMapItem
        {
            get
            {
                if (MetadataMapItems == null || MetadataMapItems.Count == 0)
                {
                    return null;
                }
                return MetadataMapItems.FirstOrDefault(mmi => mmi.Preferred);
            }
        }

        public class MetadataMapItem
        {
            /// <summary>
            /// The source type of the metadata, represented as an enumeration.
            /// </summary>
            public FileSignature.MetadataSources SourceType { get; set; }

            /// <summary>
            /// The unique identifier for the source, which may be null if the source type is 'None'.
            /// </summary>
            public long? SourceId { get; set; }

            /// <summary>
            /// The unique identifier for the data source as provided by the automatic metadata fetcher, if applicable. Is used to restore the source if the user has marked it as not manual. If null, and the user has marked the source as not manual, continue to use the SourceId as-is, and wait for the automatic fetcher to update it.
            /// </summary>
            public long? AutomaticMetadataSourceId { get; set; }

            /// <summary>
            /// Indicates whether this metadata source is the preferred one for the associated game.
            /// </summary>
            public bool Preferred { get; set; }

            /// <summary>
            /// Indicates whether this metadata source was maunally configured by the user. Prevents automatic updates. Can be modified by the user. Altering the preferred source will not change this value.
            /// </summary>
            public bool IsManual { get; set; }

            /// <summary>
            /// Gets the slug for the source, if applicable.
            /// </summary>
            public string SourceSlug
            {
                get
                {
                    string slug = "";
                    switch (SourceType)
                    {
                        case FileSignature.MetadataSources.IGDB:
                            Game game = Games.GetGame(SourceType, (long)SourceId).Result;
                            if (game != null)
                            {
                                slug = game.Slug;
                            }
                            break;

                        default:
                            slug = SourceId.ToString();
                            break;
                    }

                    return slug;
                }
            }

            /// <summary>
            /// Gets the URL link to the metadata source based on the source type and identifier.
            /// </summary>
            public string link
            {
                get
                {
                    string link = "";
                    switch (SourceType)
                    {
                        case FileSignature.MetadataSources.IGDB:
                            link = $"https://www.igdb.com/games/{SourceSlug}";
                            break;

                        case FileSignature.MetadataSources.TheGamesDb:
                            link = $"https://thegamesdb.net/game.php?id={SourceId}";
                            break;

                        case FileSignature.MetadataSources.RetroAchievements:
                            link = $"https://retroachievements.org/game/{SourceId}";
                            break;

                        case FileSignature.MetadataSources.GiantBomb:
                            link = $"https://www.giantbomb.com/games/3030-{SourceId}/";
                            break;

                        default:
                            link = "";
                            break;
                    }

                    return link;
                }
            }

            /// <summary>
            /// Indicates whether the metadata source is supported by the application.
            /// </summary>
            public bool supportedDataSource
            {
                get
                {
                    if (MetadataMapSupportedDataSources.SupportedMetadataSources.Contains((MetadataSources)SourceType))
                    {
                        return true;
                    }
                    return false;
                }
            }
        }
    }
}