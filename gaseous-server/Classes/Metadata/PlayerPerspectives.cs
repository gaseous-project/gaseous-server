using System;
using System.Threading.Tasks;
using HasheousClient.Models.Metadata.IGDB;


namespace gaseous_server.Classes.Metadata
{
    public class PlayerPerspectives
    {
        static List<PlayerPerspectiveItem> playerPerspectiveItemCache = new List<PlayerPerspectiveItem>();

        public PlayerPerspectives()
        {
        }

        public static async Task<PlayerPerspective?> GetGame_PlayerPerspectives(HasheousClient.Models.MetadataSources SourceType, long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                // check cache for player perspective
                if (playerPerspectiveItemCache.Find(x => x.Id == Id && x.SourceType == SourceType) != null)
                {
                    PlayerPerspectiveItem playerPerspectiveItem = playerPerspectiveItemCache.Find(x => x.Id == Id && x.SourceType == SourceType);

                    PlayerPerspective? nPlayerPerspective = new PlayerPerspective
                    {
                        Id = playerPerspectiveItem.Id,
                        Name = playerPerspectiveItem.Name
                    };

                    return nPlayerPerspective;
                }

                PlayerPerspective? RetVal = await Metadata.GetMetadataAsync<PlayerPerspective>(SourceType, (long)Id, false);

                if (RetVal != null)
                {
                    // add player perspective to cache
                    if (playerPerspectiveItemCache.Find(x => x.Id == Id && x.SourceType == SourceType) == null)
                    {
                        PlayerPerspectiveItem playerPerspectiveItem = new PlayerPerspectiveItem();
                        playerPerspectiveItem.Id = (long)Id;
                        playerPerspectiveItem.SourceType = SourceType;
                        playerPerspectiveItem.Name = RetVal.Name;
                        playerPerspectiveItemCache.Add(playerPerspectiveItem);
                    }
                }

                return RetVal;
            }
        }
    }

    class PlayerPerspectiveItem
    {
        public long Id { get; set; }
        public HasheousClient.Models.MetadataSources SourceType { get; set; }
        public string Name { get; set; }
    }
}

