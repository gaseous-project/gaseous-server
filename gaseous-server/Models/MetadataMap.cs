using gaseous_server.Classes.Metadata;
using HasheousClient.Models;

namespace gaseous_server.Models
{
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
            public HasheousClient.Models.MetadataSources SourceType { get; set; }
            public long SourceId { get; set; }
            public bool Preferred { get; set; }
            public string SourceSlug
            {
                get
                {
                    string slug = "";
                    switch (SourceType)
                    {
                        case MetadataSources.IGDB:
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
            public string link
            {
                get
                {
                    string link = "";
                    switch (SourceType)
                    {
                        case MetadataSources.IGDB:
                            link = $"https://www.igdb.com/games/{SourceSlug}";
                            break;

                        case MetadataSources.TheGamesDb:
                            link = $"https://thegamesdb.net/game.php?id={SourceId}";
                            break;

                        case MetadataSources.RetroAchievements:
                            link = $"https://retroachievements.org/game/{SourceId}";
                            break;

                        default:
                            link = "";
                            break;
                    }

                    return link;
                }
            }
        }
    }
}