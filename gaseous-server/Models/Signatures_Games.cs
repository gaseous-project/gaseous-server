using System;
using System.Text.Json.Serialization;
using gaseous_server.Classes;
using gaseous_server.Classes.Metadata;
using gaseous_signature_parser.models.RomSignatureObject;
using HasheousClient.Models;

namespace gaseous_server.Models
{
    public class Signatures_Games : HasheousClient.Models.SignatureModel
    {
        public Signatures_Games()
        {
        }

        [JsonIgnore]
        public int Score
        {
            get
            {
                int _score = 0;

                if (Game != null)
                {
                    _score = _score + Game.Score;
                }

                if (Rom != null)
                {
                    _score = _score + Rom.Score;
                }

                return _score;
            }
        }

        public GameItem Game = new GameItem();
        public RomItem Rom = new RomItem();

        public SignatureFlags Flags
        {
            get
            {
                SignatureFlags _flags = new SignatureFlags();

                // get default values for the platform
                var defaultPlatform = MetadataSources.Platforms.Find(x => x.Source == Config.MetadataConfiguration.DefaultMetadataSource);
                if (defaultPlatform != null)
                {
                    _flags.PlatformId = defaultPlatform.Id;
                    _flags.PlatformName = defaultPlatform.Name;
                    _flags.PlatformMetadataSource = defaultPlatform.Source;
                }

                if (_flags.PlatformId == 0)
                {
                    // fall back to the IGDB source if present
                    var igdbPlatform = MetadataSources.Platforms.Find(x => x.Source == HasheousClient.Models.MetadataSources.IGDB);
                    if (igdbPlatform != null)
                    {
                        _flags.PlatformId = igdbPlatform.Id;
                        _flags.PlatformName = igdbPlatform.Name;
                        _flags.PlatformMetadataSource = igdbPlatform.Source;
                    }
                    else
                    {
                        // fall back to none source if present
                        var nonePlatform = MetadataSources.Platforms.Find(x => x.Source == HasheousClient.Models.MetadataSources.None);
                        if (nonePlatform != null)
                        {
                            _flags.PlatformId = nonePlatform.Id;
                            _flags.PlatformName = nonePlatform.Name;
                            _flags.PlatformMetadataSource = nonePlatform.Source;
                        }
                    }
                }

                // get default values for the game
                var defaultGame = MetadataSources.Games.Find(x => x.Source == Config.MetadataConfiguration.DefaultMetadataSource);
                if (defaultGame != null)
                {
                    _flags.GameId = defaultGame.Id;
                    _flags.GameName = defaultGame.Name;
                    _flags.GameMetadataSource = defaultGame.Source;
                }

                if (_flags.GameId == 0)
                {
                    // fall back to the IGDB source if present
                    var igdbGame = MetadataSources.Games.Find(x => x.Source == HasheousClient.Models.MetadataSources.IGDB);
                    if (igdbGame != null)
                    {
                        _flags.GameId = igdbGame.Id;
                        _flags.GameName = igdbGame.Name;
                        _flags.GameMetadataSource = igdbGame.Source;
                    }
                    else
                    {
                        // fall back to none source if present
                        var noneGame = MetadataSources.Games.Find(x => x.Source == HasheousClient.Models.MetadataSources.None);
                        if (noneGame != null)
                        {
                            _flags.GameId = noneGame.Id;
                            _flags.GameName = noneGame.Name;
                            _flags.GameMetadataSource = noneGame.Source;
                        }
                    }
                }

                if (_flags.GameId == null || _flags.GameId == 0)
                {
                    _flags.GameId = 0;
                    if (this.Game.Name != null && this.Game.Name.Length > 0)
                    {
                        _flags.GameName = this.Game.Name;
                    }
                    else
                    {
                        _flags.GameName = "Unknown Game";
                    }
                    _flags.GameMetadataSource = HasheousClient.Models.MetadataSources.None;
                }

                return _flags;
            }
        }

        public SourceValues MetadataSources = new SourceValues();

        public class SourceValues
        {
            public List<SourceValueItem> Platforms = new List<SourceValueItem>();
            public List<SourceValueItem> Games = new List<SourceValueItem>();

            public class SourceValueItem
            {
                public long Id { get; set; }
                public string Name { get; set; }
                public HasheousClient.Models.MetadataSources Source { get; set; }
            }

            public void AddPlatform(long Id, string Name, HasheousClient.Models.MetadataSources Source)
            {
                // check that the platform doesn't already exist
                foreach (SourceValueItem item in Platforms)
                {
                    if (item.Id == Id)
                    {
                        return;
                    }
                }

                SourceValueItem newItem = new SourceValueItem();
                newItem.Id = Id;
                newItem.Name = Name;
                newItem.Source = Source;
                Platforms.Add(newItem);
            }

            public void AddGame(long Id, string Name, HasheousClient.Models.MetadataSources Source)
            {
                // check that the game doesn't already exist
                foreach (SourceValueItem item in Games)
                {
                    if (item.Id == Id)
                    {
                        return;
                    }
                }

                SourceValueItem newItem = new SourceValueItem();
                newItem.Id = Id;
                newItem.Name = Name;
                newItem.Source = Source;
                Games.Add(newItem);
            }
        }

        public class SignatureFlags
        {
            public long PlatformId { get; set; }
            public string PlatformName { get; set; }
            public long GameId { get; set; }
            public string GameName { get; set; }
            public HasheousClient.Models.MetadataSources PlatformMetadataSource { get; set; }
            public HasheousClient.Models.MetadataSources GameMetadataSource { get; set; }
        }

        public class GameItem : HasheousClient.Models.SignatureModel.GameItem
        {
            public GameItem()
            {

            }

            public string UserManual { get; set; }

            [JsonIgnore]
            public int Score
            {
                get
                {
                    // calculate a score based on the availablility of data
                    int _score = 0;
                    var properties = this.GetType().GetProperties();
                    foreach (var prop in properties)
                    {
                        if (prop.GetGetMethod() != null)
                        {
                            switch (prop.Name.ToLower())
                            {
                                case "id":
                                case "score":
                                    break;
                                case "name":
                                case "year":
                                case "publisher":
                                case "system":
                                    if (prop.PropertyType == typeof(string))
                                    {
                                        if (prop.GetValue(this) != null)
                                        {
                                            string propVal = prop.GetValue(this).ToString();
                                            if (propVal.Length > 0)
                                            {
                                                _score = _score + 10;
                                            }
                                        }
                                    }
                                    break;
                                default:
                                    if (prop.PropertyType == typeof(string))
                                    {
                                        if (prop.GetValue(this) != null)
                                        {
                                            string propVal = prop.GetValue(this).ToString();
                                            if (propVal.Length > 0)
                                            {
                                                _score = _score + 1;
                                            }
                                        }
                                    }
                                    break;
                            }
                        }
                    }

                    return _score;
                }
            }
        }

        public class RomItem : HasheousClient.Models.SignatureModel.RomItem
        {
            [JsonIgnore]
            public int Score
            {
                get
                {
                    // calculate a score based on the availablility of data
                    int _score = 0;
                    var properties = this.GetType().GetProperties();
                    foreach (var prop in properties)
                    {
                        if (prop.GetGetMethod() != null)
                        {
                            switch (prop.Name.ToLower())
                            {
                                case "name":
                                case "size":
                                case "crc":
                                case "developmentstatus":
                                case "flags":
                                case "attributes":
                                case "romtypemedia":
                                case "medialabel":
                                    if (prop.PropertyType == typeof(string) || prop.PropertyType == typeof(Int64) || prop.PropertyType == typeof(List<string>))
                                    {
                                        if (prop.GetValue(this) != null)
                                        {
                                            string propVal = prop.GetValue(this).ToString();
                                            if (propVal.Length > 0)
                                            {
                                                _score = _score + 10;
                                            }
                                        }
                                    }
                                    break;
                                default:
                                    if (prop.PropertyType == typeof(string))
                                    {
                                        if (prop.GetValue(this) != null)
                                        {
                                            string propVal = prop.GetValue(this).ToString();
                                            if (propVal.Length > 0)
                                            {
                                                _score = _score + 1;
                                            }
                                        }
                                    }
                                    break;
                            }
                        }
                    }

                    return _score;
                }
            }

            public class MediaType
            {
                public MediaType(SignatureSourceType Source, string MediaTypeString)
                {
                    switch (Source)
                    {
                        case RomItem.SignatureSourceType.TOSEC:
                            string[] typeString = MediaTypeString.Split(" ");

                            string inType = "";
                            foreach (string typeStringVal in typeString)
                            {
                                if (inType == "")
                                {
                                    switch (typeStringVal.ToLower())
                                    {
                                        case "disk":
                                            Media = RomItem.RomTypes.Disk;

                                            inType = typeStringVal;
                                            break;
                                        case "disc":
                                            Media = RomItem.RomTypes.Disc;

                                            inType = typeStringVal;
                                            break;
                                        case "file":
                                            Media = RomItem.RomTypes.File;

                                            inType = typeStringVal;
                                            break;
                                        case "part":
                                            Media = RomItem.RomTypes.Part;

                                            inType = typeStringVal;
                                            break;
                                        case "tape":
                                            Media = RomItem.RomTypes.Tape;

                                            inType = typeStringVal;
                                            break;
                                        case "of":
                                            inType = typeStringVal;
                                            break;
                                        case "side":
                                            inType = typeStringVal;
                                            break;
                                    }
                                }
                                else
                                {
                                    switch (inType.ToLower())
                                    {
                                        case "disk":
                                        case "disc":
                                        case "file":
                                        case "part":
                                        case "tape":
                                            Number = int.Parse(typeStringVal);
                                            break;
                                        case "of":
                                            Count = int.Parse(typeStringVal);
                                            break;
                                        case "side":
                                            Side = typeStringVal;
                                            break;
                                    }
                                    inType = "";
                                }
                            }

                            break;

                        default:
                            break;

                    }
                }

                public RomItem.RomTypes? Media { get; set; }

                public int? Number { get; set; }

                public int? Count { get; set; }

                public string? Side { get; set; }
            }
        }
    }
}

