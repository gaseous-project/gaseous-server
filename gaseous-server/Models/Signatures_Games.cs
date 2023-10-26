using System;
using System.Text.Json.Serialization;
using gaseous_signature_parser.models.RomSignatureObject;

namespace gaseous_server.Models
{
	public class Signatures_Games
	{
		public Signatures_Games()
		{
		}

        public GameItem? Game { get; set; }
        public RomItem? Rom { get; set; }

        //[JsonIgnore]
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

        public SignatureFlags Flags = new SignatureFlags();

		public class GameItem
		{
            public Int32? Id { get; set; }
            public string? Name { get; set; }
            public string? Description { get; set; }
            public string? Year { get; set; }
            public string? Publisher { get; set; }
            public DemoTypes Demo { get; set; }
            public string? System { get; set; }
            public string? SystemVariant { get; set; }
            public string? Video { get; set; }
            public string? Country { get; set; }
            public string? Language { get; set; }
            public string? Copyright { get; set; }
            
            public enum DemoTypes
            {
                NotDemo = 0,
                demo = 1,
                demo_kiosk = 2,
                demo_playable = 3,
                demo_rolling = 4,
                demo_slideshow = 5
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

        public class RomItem
        {
            public Int32? Id { get; set; }
            public string? Name { get; set; }
            public Int64? Size { get; set; }
            public string? Crc { get; set; }
            public string? Md5 { get; set; }
            public string? Sha1 { get; set; }

            public string? DevelopmentStatus { get; set; }

            public List<KeyValuePair<string, object>> Attributes { get; set; } = new List<KeyValuePair<string, object>>();

            public RomSignatureObject.Game.Rom.RomTypes RomType { get; set; }
            public string? RomTypeMedia { get; set; }
            public string? MediaLabel { get; set; }

            public RomSignatureObject.Game.Rom.SignatureSourceType SignatureSource { get; set; }

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
        }

        public class SignatureFlags
        {
            public long IGDBPlatformId { get; set; }
            public string IGDBPlatformName { get; set; }
        }
    }
}

