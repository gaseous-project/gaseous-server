using gaseous_server.Classes;
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
            public FileSignature.MetadataSources SourceType { get; set; }
            public long SourceId { get; set; }
            public bool Preferred { get; set; }
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
            public bool supportedDataSource
            {
                get
                {
                    return SourceType switch
                    {
                        FileSignature.MetadataSources.None => true,
                        FileSignature.MetadataSources.IGDB => true,
                        FileSignature.MetadataSources.TheGamesDb => true,
                        _ => false,
                    };
                }
            }
        }
    }
}