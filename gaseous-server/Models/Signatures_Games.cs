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

                foreach (SourceValues.SourceValueItem source in MetadataSources.Platforms)
                {
                    if (source.Source == Config.MetadataConfiguration.DefaultMetadataSource)
                    {
                        _flags.PlatformId = source.Id;
                        _flags.PlatformName = source.Name;
                        _flags.PlatformMetadataSource = source.Source;
                        break;
                    }
                }

                if (_flags.PlatformId == 0)
                {
                    // fall back to the IGDB source if present
                    foreach (SourceValues.SourceValueItem source in MetadataSources.Platforms)
                    {
                        if (source.Source == HasheousClient.Models.MetadataSources.IGDB)
                        {
                            _flags.PlatformId = source.Id;
                            _flags.PlatformName = source.Name;
                            _flags.PlatformMetadataSource = source.Source;
                            break;
                        }
                    }
                }

                foreach (SourceValues.SourceValueItem source in MetadataSources.Games)
                {
                    if (source.Source == Config.MetadataConfiguration.DefaultMetadataSource)
                    {
                        _flags.GameId = source.Id;
                        _flags.GameName = source.Name;
                        _flags.GameMetadataSource = source.Source;
                        break;
                    }
                }

                if (_flags.GameId == null || _flags.GameId == 0)
                {
                    _flags.GameId = 0;
                    _flags.GameName = "Unknown Game";
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

