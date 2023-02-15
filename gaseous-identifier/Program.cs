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
List<gaseous_identifier.objects.RomSignatureObject> tosecLists = new List<gaseous_identifier.objects.RomSignatureObject>();
if (tosecXML != null && tosecXML.Length > 0)
{
    tosecXML = Path.GetFullPath(tosecXML);
    Console.WriteLine("TOSEC is enabled");
    Console.WriteLine("TOSEC XML search path: " + tosecXML);

    string[] tosecPathContents = Directory.GetFiles(tosecXML);
    foreach (string tosecXMLFile in tosecPathContents)
    {
        gaseous_identifier.classes.TosecParser tosecParser = new gaseous_identifier.classes.TosecParser();
        gaseous_identifier.objects.RomSignatureObject tosecObject = tosecParser.Parse(tosecXMLFile);

        Console.Write(".");
        tosecLists.Add(tosecObject);
    }
    Console.WriteLine("");
} else
{
    Console.WriteLine("TOSEC is disabled, title matching will be by file name only.");
}
Console.WriteLine(tosecLists.Count + " TOSEC files loaded");

if (tosecLists.Count > 0)
{
    Console.WriteLine("TOSEC lists available:");
    foreach (gaseous_identifier.objects.RomSignatureObject tosecList in tosecLists)
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
    foreach (gaseous_identifier.objects.RomSignatureObject tosecList in tosecLists)
    {
        foreach (gaseous_identifier.objects.RomSignatureObject.Game gameObject in tosecList.Games)
        {
            foreach (gaseous_identifier.objects.RomSignatureObject.Game.Rom romObject in gameObject.Roms)
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