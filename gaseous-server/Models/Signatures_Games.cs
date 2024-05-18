using System;
using System.Text.Json.Serialization;
using gaseous_signature_parser.models.RomSignatureObject;

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

        public SignatureFlags Flags = new SignatureFlags();

        public class SignatureFlags
        {
            public long IGDBPlatformId { get; set; }
            public string IGDBPlatformName { get; set; }
            public long IGDBGameId { get; set; }
        }

        public class GameItem : HasheousClient.Models.SignatureModel.GameItem
        {
            public GameItem()
            {

            }

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
                                else {
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

