using System;
using System.Xml;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;

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

            // get hashes of TOSEC file
            var xmlStream = File.OpenRead(XMLFile);

            var md5 = MD5.Create();
            byte[] md5HashByte = md5.ComputeHash(xmlStream);
            string md5Hash = BitConverter.ToString(md5HashByte).Replace("-", "").ToLowerInvariant();

            var sha1 = SHA1.Create();
            byte[] sha1HashByte = sha1.ComputeHash(xmlStream);
            string sha1Hash = BitConverter.ToString(sha1HashByte).Replace("-", "").ToLowerInvariant();

            // load TOSEC file
            XmlDocument tosecXmlDoc = new XmlDocument();
            tosecXmlDoc.Load(XMLFile);

            objects.RomSignatureObject tosecObject = new objects.RomSignatureObject();

            // get header
            XmlNode xmlHeader = tosecXmlDoc.DocumentElement.SelectSingleNode("/datafile/header");
            tosecObject.SourceType = "TOSEC";
            tosecObject.SourceMd5 = md5Hash;
            tosecObject.SourceSHA1 = sha1Hash;
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
                    gameName = gameName.Replace("(demo) ", "");
                }
                else if (gameName.Contains("(demo-kiosk) ", StringComparison.CurrentCulture))
                {
                    gameObject.Demo = objects.RomSignatureObject.Game.DemoTypes.demo_kiosk;
                    gameName = gameName.Replace("(demo-kiosk) ", "");
                }
                else if (gameName.Contains("(demo-playable) ", StringComparison.CurrentCulture))
                {
                    gameObject.Demo = objects.RomSignatureObject.Game.DemoTypes.demo_playable;
                    gameName = gameName.Replace("(demo-playable) ", "");
                }
                else if (gameName.Contains("(demo-rolling) ", StringComparison.CurrentCulture))
                {
                    gameObject.Demo = objects.RomSignatureObject.Game.DemoTypes.demo_rolling;
                    gameName = gameName.Replace("(demo-rolling) ", "");
                }
                else if (gameName.Contains("(demo-slideshow) ", StringComparison.CurrentCulture))
                {
                    gameObject.Demo = objects.RomSignatureObject.Game.DemoTypes.demo_slideshow;
                    gameName = gameName.Replace("(demo-slideshow) ", "");
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
                    bool dateFound = false;

                    // verify the value
                    string dateToken = gameNameTokens[1].Replace(")", "");
                    if (dateToken.Length >= 4)
                    {
                        // test for possible year values
                        // first up - centuries
                        if (dateToken == "19xx" || dateToken == "20xx")
                        {
                            // date is a century
                            gameObject.Year = dateToken;
                            dateFound = true;
                        } else
                        {
                            // check for decades
                            for (UInt16 i = 0; i < 10; i++)
                            {
                                if (dateToken == "19" + i + "x" || dateToken == "20" + i + "x")
                                {
                                    // date is a decade
                                    gameObject.Year = dateToken;
                                    dateFound = true;
                                    break;
                                }
                            }

                            if (dateFound == false)
                            {
                                // check if the year is a four digit number
                                DateTime dateTime = new DateTime();
                                if (DateTime.TryParse(string.Format("1/1/{0}", dateToken), out dateTime))
                                {
                                    // is a valid year!
                                    gameObject.Year = dateToken;
                                    dateFound = true;
                                }

                                // if we still haven't found a valid date, check if the whole string is a valid date object
                                if (dateFound == false)
                                {
                                    if (DateTime.TryParse(dateToken, out dateTime))
                                    {
                                        // is a valid year!
                                        gameObject.Year = dateToken;
                                        dateFound = true;
                                    }
                                }

                                // if we still haven't found a valid date, check if the whole string is a valid date object, but with x's
                                // example: 19xx-12-2x
                                if (dateFound == false)
                                {
                                    if (DateTime.TryParse(dateToken.Replace("x", "0"), out dateTime))
                                    {
                                        // is a valid year!
                                        gameObject.Year = dateToken;
                                        dateFound = true;
                                    }
                                }

                                // if we still haven't found a valid date, perhaps it a year and month?
                                // example: 19xx-12
                                if (dateFound == false)
                                {
                                    if (DateTime.TryParse(dateToken.Replace("x", "0") + "-01", out dateTime))
                                    {
                                        // is a valid year!
                                        gameObject.Year = dateToken;
                                        dateFound = true;
                                    }
                                }
                            }
                        }
                    }
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
                                gameObject.Country = token;
                            }

                            // check for language
                            if (TOSECLanguage.ContainsKey(token))
                            {
                                gameObject.Language = token;
                            }

                            // check for copyright
                            if (TOSECCopyright.ContainsKey(token))
                            {
                                gameObject.Copyright = token;
                            }
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

                                // check for copyright
                                if (TOSECDevelopment.ContainsKey(token))
                                {
                                    romObject.DevelopmentStatus = token;
                                }

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
                                    (token + ")") == gameNameTokens.Last() &&
                                    (
                                        token != romObject.RomTypeMedia &&
                                        token != gameObject.Publisher &&
                                        token != gameObject.SystemVariant &&
                                        token != gameObject.Video &&
                                        token != gameObject.Country &&
                                        token != gameObject.Copyright &&
                                        token != gameObject.Language &&
                                        token != romObject.DevelopmentStatus
                                    )
                                   )
                                {
                                    // likely the media label?
                                    romObject.MediaLabel = token;
                                }

                                // process dump flags
                                if (rawToken.IndexOf("[") > 0)
                                {
                                    // has dump flags
                                    string rawDumpFlags = rawToken.Substring(rawToken.IndexOf("["));
                                    string[] dumpFlags = rawDumpFlags.Split("[");
                                    foreach (string dumpFlag in dumpFlags)
                                    {
                                        string dToken = dumpFlag.Replace("]", "");
                                        if (dToken.Length > 0)
                                        {
                                            string[] dTokenCompare = dToken.Split(" ");
                                            switch (dTokenCompare[0].Trim().ToLower())
                                            {
                                                case "cr":
                                                // cracked
                                                case "f":
                                                // fixed
                                                case "h":
                                                // hacked
                                                case "m":
                                                // modified
                                                case "p":
                                                // pirated
                                                case "t":
                                                // trained
                                                case "tr":
                                                // translated
                                                case "o":
                                                // overdump
                                                case "u":
                                                // underdump
                                                case "v":
                                                // virus
                                                case "b":
                                                // bad dump
                                                case "a":
                                                // alternate
                                                case "!":
                                                    // known verified dump
                                                    // -------------------
                                                    romObject.flags.Add(dToken);
                                                    break;
                                            }
                                            
                                        }
                                    }
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
                    if (existingGame.Name == gameObject.Name &&
                        existingGame.Year == gameObject.Year &&
                        existingGame.Publisher == gameObject.Publisher &&
                        existingGame.Country == gameObject.Country &&
                        existingGame.Language == gameObject.Language)
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

