using System;
using System.Threading.Tasks;
using HasheousClient.Models.Metadata.IGDB;


namespace gaseous_server.Classes.Metadata
{
    public class GameModes
    {
        static List<GameModeItem> gameModeItemCache = new List<GameModeItem>();

        public GameModes()
        {
        }

        public static async Task<GameMode?> GetGame_Modes(HasheousClient.Models.MetadataSources SourceType, long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                // check cache for game mode
                if (gameModeItemCache.Find(x => x.Id == Id && x.SourceType == SourceType) != null)
                {
                    GameModeItem gameModeItem = gameModeItemCache.Find(x => x.Id == Id && x.SourceType == SourceType);

                    GameMode? nGameMode = new GameMode
                    {
                        Id = gameModeItem.Id,
                        Name = gameModeItem.Name
                    };

                    return nGameMode;
                }

                GameMode? RetVal = await Metadata.GetMetadataAsync<GameMode>(SourceType, (long)Id, false);

                if (RetVal != null)
                {
                    // add game mode to cache
                    if (gameModeItemCache.Find(x => x.Id == Id && x.SourceType == SourceType) == null)
                    {
                        GameModeItem gameModeItem = new GameModeItem();
                        gameModeItem.Id = (long)Id;
                        gameModeItem.SourceType = SourceType;
                        gameModeItem.Name = RetVal.Name;
                        gameModeItemCache.Add(gameModeItem);
                    }
                }

                return RetVal;
            }
        }
    }

    class GameModeItem
    {
        public long Id { get; set; }
        public HasheousClient.Models.MetadataSources SourceType { get; set; }
        public string Name { get; set; }
    }
}

