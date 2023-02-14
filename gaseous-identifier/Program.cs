// parse command line
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

string[] commandLineArgs = Environment.GetCommandLineArgs();

string scanPath = "./";
string tosecXML = "";

string inArgument = "";
foreach (string commandLineArg in commandLineArgs)
{
    if (commandLineArg != commandLineArgs[0])
    {
        if (inArgument == "")
        {
            switch (commandLineArg.ToLower())
            {
                case "-scanpath":
                    inArgument = commandLineArg.ToLower();
                    break;
                case "-tosecpath":
                    inArgument = commandLineArg.ToLower();
                    break;
                default:
                    break;
            }
        }
        else
        {
            switch (inArgument)
            {
                case "-scanpath":
                    scanPath = commandLineArg;
                    break;
                case "-tosecpath":
                    tosecXML = commandLineArg;
                    break;
                default:
                    break;
            }
            inArgument = "";
        }
    }
}

scanPath = Path.GetFullPath(scanPath);
Console.WriteLine("ROM search path: " + scanPath);

System.Collections.ArrayList TOSEC = new System.Collections.ArrayList();
List<gaseous_identifier.classes.tosecXML> tosecLists = new List<gaseous_identifier.classes.tosecXML>();
UInt32 GameCounter = 0;
if (tosecXML != null && tosecXML.Length > 0)
{
    tosecXML = Path.GetFullPath(tosecXML);
    Console.WriteLine("TOSEC is enabled");
    Console.WriteLine("TOSEC XML search path: " + tosecXML);

    string[] tosecPathContents = Directory.GetFiles(tosecXML);
    foreach (string tosecXMLFile in tosecPathContents)
    {
        XmlDocument tosecXmlDoc = new XmlDocument();
        tosecXmlDoc.Load(tosecXMLFile);

        gaseous_identifier.classes.tosecXML tosecObject = new gaseous_identifier.classes.tosecXML();

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
                    } catch
                    {
                        tosecObject.Url = null;
                    }
                    break;
            }
        }

        // get games
        tosecObject.Games = new List<gaseous_identifier.classes.tosecXML.Game>();
        XmlNodeList xmlGames = tosecXmlDoc.DocumentElement.SelectNodes("/datafile/game");
        foreach (XmlNode xmlGame in xmlGames)
        {
            gaseous_identifier.classes.tosecXML.Game gameObject = new gaseous_identifier.classes.tosecXML.Game();

            // parse game name
            string gameName = xmlGame.Attributes["name"].Value;
            string[] gameNameTokens = gameName.Split("(");
            // game title should be first item
            gameObject.Name = gameNameTokens[0].Trim();
            // game year should be second item
            if (gameNameTokens.Length == 2)
            {
                gameObject.Year = gameNameTokens[1].Replace(")", "").Trim();
            } else
            {
                gameObject.Year = "";
            }
            // game publisher should be third item
            if (gameNameTokens.Length == 3)
            {
                gameObject.Publisher = gameNameTokens[2].Replace(")", "").Trim();
            } else
            {
                gameObject.Publisher = "";
            }

            gameObject.Roms = new List<gaseous_identifier.classes.tosecXML.Game.Rom>();

            // get the roms
            foreach (XmlNode xmlGameDetail in xmlGame.ChildNodes)
            {
                switch (xmlGameDetail.Name.ToLower())
                {
                    case "description":
                        gameObject.Description = xmlGameDetail.InnerText;
                        break;

                    case "rom":
                        gaseous_identifier.classes.tosecXML.Game.Rom romObject = new gaseous_identifier.classes.tosecXML.Game.Rom();
                        romObject.Name = xmlGameDetail.Attributes["name"]?.Value;
                        romObject.Size = UInt64.Parse(xmlGameDetail.Attributes["size"]?.Value);
                        romObject.Crc = xmlGameDetail.Attributes["crc"]?.Value;
                        romObject.Md5 = xmlGameDetail.Attributes["md5"]?.Value;
                        romObject.Sha1 = xmlGameDetail.Attributes["sha1"]?.Value;

                        gameObject.Roms.Add(romObject);
                        break;
                }
            }

            // search for existing gameObject to update
            bool existingGameFound = false;
            foreach (gaseous_identifier.classes.tosecXML.Game existingGame in tosecObject.Games)
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
                GameCounter += 1;
            }
        }

        Console.Write(".");
        tosecLists.Add(tosecObject);
    }
    Console.WriteLine("");
} else
{
    Console.WriteLine("TOSEC is disabled, title matching will be by file name only.");
}
Console.WriteLine(tosecLists.Count + " TOSEC files loaded - " + GameCounter + " games cataloged");

if (tosecLists.Count > 0)
{
    Console.WriteLine("TOSEC lists available:");
    foreach (gaseous_identifier.classes.tosecXML tosecList in tosecLists)
    {
        Console.WriteLine(" * " + tosecList.Name);
    }
}

Console.WriteLine("Examining files");
string[] romPathContents = Directory.GetFiles(scanPath);
foreach (string romFile in romPathContents)
{
    Console.WriteLine("Checking " + romFile);

    var stream = File.OpenRead(romFile);

    var md5 = MD5.Create();
    byte[] md5HashByte = md5.ComputeHash(stream);
    string md5Hash = BitConverter.ToString(md5HashByte).Replace("-", "").ToLowerInvariant();

    var sha1 = SHA1.Create();
    byte[] sha1HashByte = sha1.ComputeHash(stream);
    string sha1Hash = BitConverter.ToString(md5HashByte).Replace("-", "").ToLowerInvariant();

    bool gameFound = false;
    foreach (gaseous_identifier.classes.tosecXML tosecList in tosecLists)
    {
        foreach (gaseous_identifier.classes.tosecXML.Game gameObject in tosecList.Games)
        {
            foreach (gaseous_identifier.classes.tosecXML.Game.Rom romObject in gameObject.Roms)
            {
                if (md5Hash == romObject.Md5)
                {
                    // match
                    gameFound = true;
                    Console.WriteLine(romObject.Name);
                    break;
                }
            }
            if (gameFound == true) { break; }
        }
        if (gameFound == true) { break; }
    }
    if (gameFound == false)
    {
        Console.WriteLine("File not found in TOSEC library");
    }
}