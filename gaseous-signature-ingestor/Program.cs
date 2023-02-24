using System;
using System.IO;
using MySql.Data.MySqlClient;
using gaseous_romsignatureobject;
using gaseous_signature_parser.parsers;
using gaseous_tools;

string configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gaseous-server");

// process command line
string[] commandLineArgs = Environment.GetCommandLineArgs();

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

// check if configPath is valid and create it if not
if (!Directory.Exists(configPath))
{
    Directory.CreateDirectory(configPath);
}

// connect to database
string cs = @"server=localhost;userid=gaseous;password=gaseous;database=gaseous";
Database db = new gaseous_tools.Database(Database.databaseType.MySql, cs);

// process provided files
Console.WriteLine("Processing input files:");
if (Directory.Exists(tosecXML))
{
    Console.WriteLine("Processing TOSEC data files", ConsoleColor.Green);
    Console.WriteLine("");
    Console.WriteLine("");

    tosecXML = Path.GetFullPath(tosecXML);
    string[] tosecPathContents = Directory.GetFiles(tosecXML);
    int lineFileNameLength = 0;
    for (UInt16 i = 0; i < tosecPathContents.Length; ++i)
    {
        string tosecXMLFile = tosecPathContents[i];

        string statusOutput = i + " / " + tosecPathContents.Length + " : " + Path.GetFileName(tosecXMLFile);
        Console.SetCursorPosition(0, Console.CursorTop - 2);
        Console.WriteLine("\r " + statusOutput.PadRight(lineFileNameLength, ' ') + "\r");
        lineFileNameLength = statusOutput.Length;

        Console.WriteLine("Parsing file");

        TosecParser tosecParser = new TosecParser();
        RomSignatureObject tosecObject = tosecParser.Parse(tosecXMLFile);

        // store in database
        foreach (RomSignatureObject.Game gameObject in tosecObject.Games)
        {
            string sql = "";
            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            System.Data.DataTable sigDB;

            // store platform
            if (gameObject.System != null)
            {
                sql = "SELECT * FROM signatures_platforms WHERE platform=@platform";
                dbDict = new Dictionary<string, object>();
                dbDict.Add("platform", gameObject.System);

                sigDB = db.ExecuteCMD(sql, dbDict);
                if (sigDB.Rows.Count == 0)
                {
                    // entry not present, insert it
                    Console.SetCursorPosition(0, Console.CursorTop - 1);
                    Console.WriteLine("Saving platform: " + gameObject.System);

                    sql = "INSERT INTO signatures_platforms (platform) VALUES (@platform)";
                    db.ExecuteCMD(sql, dbDict);
                }
            }

            // store publisher
            if (gameObject.Publisher != null)
            {
                sql = "SELECT * FROM signatures_publishers WHERE publisher=@publisher";
                dbDict = new Dictionary<string, object>();
                dbDict.Add("publisher", gameObject.Publisher);

                sigDB = db.ExecuteCMD(sql, dbDict);
                if (sigDB.Rows.Count == 0)
                {
                    // entry not present, insert it
                    Console.SetCursorPosition(0, Console.CursorTop - 1);
                    Console.WriteLine("Saving publisher: " + gameObject.Publisher);

                    sql = "INSERT INTO signatures_publishers (publisher) VALUES (@publisher)";
                    db.ExecuteCMD(sql, dbDict);
                }
            }
        }
    }
}