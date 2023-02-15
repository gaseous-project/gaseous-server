using System;
using System.Xml;
using System.IO;
using System.Reflection;

namespace gaseous_identifier.classes
{
	public class TosecParser
	{
		public objects.RomSignatureObject Parse(string XMLFile)
		{
            // load resources
            var assembly = Assembly.GetExecutingAssembly();
            // load systems list
            List<string> TOSECSystems = new List<string>();
            var resourceName = "gaseous_identifier.Support.Parsers.TOSEC.Systems.txt";
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
            {
                TOSECSystems = reader.ReadToEnd().Split(Environment.NewLine).ToList<string>();
            }
            // load video list
            List<string> TOSECVideo = new List<string>();
            resourceName = "gaseous_identifier.Support.Parsers.TOSEC.Video.txt";
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                TOSECVideo = reader.ReadToEnd().Split(Environment.NewLine).ToList<string>();
            }
            // load country list
            Dictionary<string, string> TOSECCountry = new Dictionary<string, string>();
            resourceName = "gaseous_identifier.Support.Parsers.TOSEC.Country.txt";
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                do
                {
                    string[] line = reader.ReadLine().Split(",");
                    TOSECCountry.Add(line[0], line[1]);
                } while (reader.EndOfStream == false);
            }
            // load language list
            Dictionary<string, string> TOSECLanguage = new Dictionary<string, string>();
            resourceName = "gaseous_identifier.Support.Parsers.TOSEC.Language.txt";
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                do
                {
                    string[] line = reader.ReadLine().Split(",");
                    TOSECLanguage.Add(line[0], line[1]);
                } while (reader.EndOfStream == false);
            }
            // load copyright list
            Dictionary<string, string> TOSECCopyright = new Dictionary<string, string>();
            resourceName = "gaseous_identifier.Support.Parsers.TOSEC.Copyright.txt";
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                do
                {
                    string[] line = reader.ReadLine().Split(",");
                    TOSECCopyright.Add(line[0], line[1]);
                } while (reader.EndOfStream == false);
            }
            // load development status list
            Dictionary<string, string> TOSECDevelopment = new Dictionary<string, string>();
            resourceName = "gaseous_identifier.Support.Parsers.TOSEC.DevelopmentStatus.txt";
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                do
                {
                    string[] line = reader.ReadLine().Split(",");
                    TOSECDevelopment.Add(line[0], line[1]);
                } while (reader.EndOfStream == false);
            }

            // load TOSEC file
            XmlDocument tosecXmlDoc = new XmlDocument();
            tosecXmlDoc.Load(XMLFile);

            objects.RomSignatureObject tosecObject = new objects.RomSignatureObject();

            // get header
            XmlNode xmlHeader = tosecXmlDoc.DocumentElement.SelectSingleNode("/datafile/header");
            foreach (XmlNode childNode in xmlHeader.ChildNodes)
            {
                switch (childNode.Name.ToLower())
                {
                    case "name":
                        tosecObject.Name = childNode.InnerText;
                        break;

                    case "description":
                        tosecObject.Description = childNode.InnerText;
                        break;

                    case "category":
                        tosecObject.Category = childNode.InnerText;
                        break;

                    case "version":
                        tosecObject.Version = childNode.InnerText;
                        break;

                    case "author":
                        tosecObject.Author = childNode.InnerText;
                        break;

                    case "email":
                        tosecObject.Email = childNode.InnerText;
                        break;

                    case "homepage":
                        tosecObject.Homepage = childNode.InnerText;
                        break;

                    case "url":
                        try
                        {
                            tosecObject.Url = new Uri(childNode.InnerText);
                        }
                        catch
                        {
                            tosecObject.Url = null;
                        }
                        break;
                }
            }

            // get games
            tosecObject.Games = new List<gaseous_identifier.objects.RomSignatureObject.Game>();
            XmlNodeList xmlGames = tosecXmlDoc.DocumentElement.SelectNodes("/datafile/game");
            foreach (XmlNode xmlGame in xmlGames)
            {
                objects.RomSignatureObject.Game gameObject = new objects.RomSignatureObject.Game();

                // parse game name
                string gameName = xmlGame.Attributes["name"].Value;

                // before split, save and remove the demo tag if present
                if (gameName.Contains("(demo) ", StringComparison.CurrentCulture))
                {
                    gameObject.Demo = objects.RomSignatureObject.Game.DemoTypes.demo;
                    gameName.Replace("(demo) ", "");
                }
                else if (gameName.Contains("(demo-kiosk) ", StringComparison.CurrentCulture))
                {
                    gameObject.Demo = objects.RomSignatureObject.Game.DemoTypes.demo_kiosk;
                    gameName.Replace("(demo-kiosk) ", "");
                }
                else if (gameName.Contains("(demo-playable) ", StringComparison.CurrentCulture))
                {
                    gameObject.Demo = objects.RomSignatureObject.Game.DemoTypes.demo_playable;
                    gameName.Replace("(demo-playable) ", "");
                }
                else if (gameName.Contains("(demo-rolling) ", StringComparison.CurrentCulture))
                {
                    gameObject.Demo = objects.RomSignatureObject.Game.DemoTypes.demo_rolling;
                    gameName.Replace("(demo-rolling) ", "");
                }
                else if (gameName.Contains("(demo-slideshow) ", StringComparison.CurrentCulture))
                {
                    gameObject.Demo = objects.RomSignatureObject.Game.DemoTypes.demo_slideshow;
                    gameName.Replace("(demo-slideshow) ", "");
                }
                else
                {
                    gameObject.Demo = objects.RomSignatureObject.Game.DemoTypes.NotDemo;
                }

                string[] gameNameTokens = gameName.Split("(");
                // game title should be first item
                gameObject.Name = gameNameTokens[0].Trim();

                // game year should be second item
                if (gameNameTokens.Length >= 2)
                {
                    gameObject.Year = gameNameTokens[1].Replace(")", "").Trim();
                }
                else
                {
                    gameObject.Year = "";
                }
                // game publisher should be third item
                if (gameNameTokens.Length >= 3)
                {
                    gameObject.Publisher = gameNameTokens[2].Replace(")", "").Trim();
                }
                else
                {
                    gameObject.Publisher = "";
                }
                // process remaining tokens
                // set default values
                gameObject.System = tosecObject.Name.Split(" - ")[0];
                // process title values
                UInt16 StartToken = 0;
                foreach (string rawToken in gameNameTokens)
                {
                    if (StartToken > 2)
                    {
                        string[] tokenSplit = rawToken.Split("[");

                        // replace the extra closing bracket
                        string token = tokenSplit[0].Replace(")", "").Trim();

                        // perform tests on the token to see what it is
                        // exclude strings that start with [ in this part
                        if (!(token.StartsWith("[") && token.EndsWith("]")))
                        {
                            // check for systems
                            if (TOSECSystems.Contains(token, StringComparer.CurrentCulture))
                            {
                                // this is a system token
                                gameObject.SystemVariant = token;
                            }

                            // check for video
                            if (TOSECVideo.Contains(token, StringComparer.CurrentCulture))
                            {
                                // this is a system token
                                gameObject.Video = token;
                            }

                            // check for country
                            if (TOSECCountry.ContainsKey(token))
                            {
                                gameObject.Country = new KeyValuePair<string, string>(token, TOSECCountry[token]);
                            }

                            // check for language
                            if (TOSECLanguage.ContainsKey(token))
                            {
                                gameObject.Language = new KeyValuePair<string, string>(token, TOSECLanguage[token]);
                            }

                            // check for copyright
                            if (TOSECCopyright.ContainsKey(token))
                            {
                                gameObject.Copyright = new KeyValuePair<string, string>(token, TOSECCopyright[token]);
                            }

                            // check for copyright
                            if (TOSECDevelopment.ContainsKey(token))
                            {
                                gameObject.DevelopmentStatus = new KeyValuePair<string, string>(token, TOSECDevelopment[token]);
                            }
                        }
                        else
                        {
                            // handle the square bracket tokens
                        }
                    }
                    StartToken += 1;
                }









                gameObject.Roms = new List<objects.RomSignatureObject.Game.Rom>();

                // get the roms
                foreach (XmlNode xmlGameDetail in xmlGame.ChildNodes)
                {
                    switch (xmlGameDetail.Name.ToLower())
                    {
                        case "description":
                            gameObject.Description = xmlGameDetail.InnerText;
                            break;

                        case "rom":
                            objects.RomSignatureObject.Game.Rom romObject = new objects.RomSignatureObject.Game.Rom();
                            romObject.Name = xmlGameDetail.Attributes["name"]?.Value;
                            romObject.Size = UInt64.Parse(xmlGameDetail.Attributes["size"]?.Value);
                            romObject.Crc = xmlGameDetail.Attributes["crc"]?.Value;
                            romObject.Md5 = xmlGameDetail.Attributes["md5"]?.Value;
                            romObject.Sha1 = xmlGameDetail.Attributes["sha1"]?.Value;

                            // parse name
                            string[] romNameTokens = romObject.Name.Split("(");
                            foreach (string rawToken in gameNameTokens) {
                                string[] tokenSplit = rawToken.Split("[");

                                // replace the extra closing bracket
                                string token = tokenSplit[0].Replace(")", "").Trim();

                                // check for media type
                                if (token.StartsWith("Disc") ||
                                token.StartsWith("Disk") ||
                                token.StartsWith("File") ||
                                token.StartsWith("Part") ||
                                token.StartsWith("Side") ||
                                token.StartsWith("Tape"))
                                {
                                    string[] tokens = token.Split(" ");
                                    switch (tokens[0])
                                    {
                                        case "Disc":
                                            romObject.RomType = objects.RomSignatureObject.Game.Rom.RomTypes.Disc;
                                            break;
                                        case "Disk":
                                            romObject.RomType = objects.RomSignatureObject.Game.Rom.RomTypes.Disk;
                                            break;
                                        case "File":
                                            romObject.RomType = objects.RomSignatureObject.Game.Rom.RomTypes.File;
                                            break;
                                        case "Part":
                                            romObject.RomType = objects.RomSignatureObject.Game.Rom.RomTypes.Part;
                                            break;
                                        case "Side":
                                            romObject.RomType = objects.RomSignatureObject.Game.Rom.RomTypes.Side;
                                            break;
                                        case "Tape":
                                            romObject.RomType = objects.RomSignatureObject.Game.Rom.RomTypes.Tape;
                                            break;
                                    }
                                    romObject.RomTypeMedia = token;
                                }

                                // check for media label
                                if (token.Length > 0 &&
                                    token == gameNameTokens.Last() &&
                                    gameNameTokens.Length > 2 &&
                                    (
                                        token != romObject.RomTypeMedia &&
                                        token != gameObject.Publisher &&
                                        token != gameObject.Country.Key)
                                    )
                                {
                                    // likely the media label?
                                    romObject.MediaLabel = token;
                                }
                            }

                            gameObject.Roms.Add(romObject);
                            break;
                    }
                }

                // search for existing gameObject to update
                bool existingGameFound = false;
                foreach (gaseous_identifier.objects.RomSignatureObject.Game existingGame in tosecObject.Games)
                {
                    if (existingGame.Name == gameObject.Name && existingGame.Year == gameObject.Year && existingGame.Publisher == gameObject.Publisher)
                    {
                        existingGame.Roms.AddRange(gameObject.Roms);
                        existingGameFound = true;
                        break;
                    }
                }
                if (existingGameFound == false)
                {
                    tosecObject.Games.Add(gameObject);
                }
            }

            return tosecObject;
        }
	}
}

