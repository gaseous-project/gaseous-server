// parse command line
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Newtonsoft.Json;

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

List<gaseous_identifier.objects.RomSignatureObject> romSignatures = new List<gaseous_identifier.objects.RomSignatureObject>();
System.Collections.ArrayList availablePlatforms = new System.Collections.ArrayList();

// load TOSEC XML files
if (tosecXML != null && tosecXML.Length > 0)
{
    tosecXML = Path.GetFullPath(tosecXML);
    Console.WriteLine("TOSEC is enabled");
    Console.WriteLine("TOSEC XML search path: " + tosecXML);

    string[] tosecPathContents = Directory.GetFiles(tosecXML);
    int lastCLILineLength = 0;
    for (UInt16 i = 0; i < tosecPathContents.Length; ++i)
    {
        string tosecXMLFile = tosecPathContents[i];

        gaseous_identifier.classes.TosecParser tosecParser = new gaseous_identifier.classes.TosecParser();
        gaseous_identifier.objects.RomSignatureObject tosecObject = tosecParser.Parse(tosecXMLFile);

        string statusOutput = i + " / " + tosecPathContents.Length + " : " + Path.GetFileName(tosecXMLFile);
        Console.Write("\r " + statusOutput.PadRight(lastCLILineLength, ' ') + "\r");
        lastCLILineLength = statusOutput.Length;

        foreach (gaseous_identifier.objects.RomSignatureObject.Game gameRom in tosecObject.Games)
        {
            if (!availablePlatforms.Contains(gameRom.System))
            {
                availablePlatforms.Add(gameRom.System);
            }
        }

        romSignatures.Add(tosecObject);
    }
    Console.WriteLine("");
} else
{
    Console.WriteLine("TOSEC is disabled.");
}
Console.WriteLine(romSignatures.Count + " TOSEC files loaded");

// Summarise signatures
if (availablePlatforms.Count > 0)
{
    availablePlatforms.Sort();
    Console.WriteLine("Platforms loaded:");
    foreach (string platform in availablePlatforms)
    {
        Console.WriteLine(" * " + platform);
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
    string sha1Hash = BitConverter.ToString(sha1HashByte).Replace("-", "").ToLowerInvariant();

    bool gameFound = false;
    foreach (gaseous_identifier.objects.RomSignatureObject tosecList in romSignatures)
    {
        foreach (gaseous_identifier.objects.RomSignatureObject.Game gameObject in tosecList.Games)
        {
            foreach (gaseous_identifier.objects.RomSignatureObject.Game.Rom romObject in gameObject.Roms)
            {
                if (romObject.Md5 != null)
                {
                    if (md5Hash == romObject.Md5.ToLowerInvariant())
                    {
                        // match
                        gameFound = true;
                        Console.WriteLine(romObject.Name);

                        gaseous_identifier.objects.RomSignatureObject.Game gameSignature = gameObject;
                        gameSignature.Roms.Clear();
                        gameSignature.Roms.Add(romObject);

                        var jsonSerializerSettings = new JsonSerializerSettings();
                        jsonSerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
                        jsonSerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(gameSignature, Newtonsoft.Json.Formatting.Indented, jsonSerializerSettings));
                        break;
                    }
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