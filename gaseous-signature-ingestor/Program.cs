using System;
using System.IO;
using MySql.Data.MySqlClient;
using gaseous_romsignatureobject;
using gaseous_signature_parser.parsers;
using gaseous_tools;
using MySqlX.XDevAPI;

string configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gaseous-server");

// process command line
string[] commandLineArgs = Environment.GetCommandLineArgs();

string tosecXML = "";
bool showGames = false;
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
                case "-showgames":
                    showGames = true;
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

tosecXML = "/Users/michaelgreen/Downloads/TOSEC - DAT Pack - Complete (3764) (TOSEC-v2023-01-23)/TOSEC";

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
    int lineGameNameLength = 0;

    string sql = "";
    Dictionary<string, object> dbDict = new Dictionary<string, object>();
    System.Data.DataTable sigDB;

    for (UInt16 i = 0; i < tosecPathContents.Length; ++i)
    {
        string tosecXMLFile = tosecPathContents[i];

        string statusOutput = (i + 1).ToString().PadLeft(7, ' ') + " / " + tosecPathContents.Length.ToString().PadLeft(7, ' ') + " : " + Path.GetFileName(tosecXMLFile);
        Console.SetCursorPosition(0, Console.CursorTop - 2);
        Console.WriteLine("\r " + statusOutput.PadRight(lineFileNameLength, ' ') + "\r");
        lineFileNameLength = statusOutput.Length;

        Console.WriteLine("  ==> Parsing file");

        TosecParser tosecParser = new TosecParser();
        RomSignatureObject tosecObject = tosecParser.Parse(tosecXMLFile);

        // store in database

        // store source object
        bool processGames = false;
        if (tosecObject.SourceMd5 != null)
        {
            sql = "SELECT * FROM signatures_sources WHERE sourcemd5=@sourcemd5";
            dbDict = new Dictionary<string, object>();
            if (tosecObject.Name != null)
            {
                dbDict.Add("name", tosecObject.Name);
            }
            else
            {
                dbDict.Add("name", "");
            }
            if (tosecObject.Description != null)
            {
                dbDict.Add("description", tosecObject.Description);
            }
            else
            {
                dbDict.Add("description", "");
            }
            if (tosecObject.Category != null)
            {
                dbDict.Add("category", tosecObject.Category);
            }
            else
            {
                dbDict.Add("category", "");
            }
            if (tosecObject.Version != null)
            {
                dbDict.Add("version", tosecObject.Version);
            }
            else
            {
                dbDict.Add("version", "");
            }
            if (tosecObject.Author != null)
            {
                dbDict.Add("author", tosecObject.Author);
            }
            else
            {
                dbDict.Add("author", "");
            }
            if (tosecObject.Email != null)
            {
                dbDict.Add("email", tosecObject.Email);
            }
            else
            {
                dbDict.Add("email", "");
            }
            if (tosecObject.Homepage != null)
            {
                dbDict.Add("homepage", tosecObject.Homepage);
            }
            else
            {
                dbDict.Add("homepage", "");
            }
            if (tosecObject.Url != null)
            {
                dbDict.Add("uri", tosecObject.Url.ToString());
            }
            else
            {
                dbDict.Add("uri", "");
            }
            if (tosecObject.SourceType != null)
            {
                dbDict.Add("sourcetype", tosecObject.SourceType);
            }
            else
            {
                dbDict.Add("sourcetype", "");
            }
            dbDict.Add("sourcemd5", tosecObject.SourceMd5);
            dbDict.Add("sourcesha1", tosecObject.SourceSHA1);

            sigDB = db.ExecuteCMD(sql, dbDict);
            if (sigDB.Rows.Count == 0)
            {
                // entry not present, insert it
                sql = "INSERT INTO signatures_sources (name, description, category, version, author, email, homepage, url, sourcetype, sourcemd5, sourcesha1) VALUES (@name, @description, @category, @version, @author, @email, @homepage, @uri, @sourcetype, @sourcemd5, @sourcesha1)";

                db.ExecuteCMD(sql, dbDict);

                processGames = true;
            }

            if (processGames == true)
            {
                for (int x = 0; x < tosecObject.Games.Count; ++x)
                {
                    RomSignatureObject.Game gameObject = tosecObject.Games[x];

                    // update display
                    if (showGames == true)
                    {
                        Console.SetCursorPosition(0, Console.CursorTop - 1);
                        string statusGameOutput = " ==> " + (x + 1).ToString().PadLeft(7, ' ') + " / " + tosecObject.Games.Count.ToString().PadLeft(7, ' ') + " : " + gameObject.Name;
                        Console.WriteLine("\r " + statusGameOutput.PadRight(lineGameNameLength, ' ') + "\r");
                        lineGameNameLength = statusGameOutput.Length;
                    }

                    // set up game dictionary
                    dbDict = new Dictionary<string, object>();
                    if (gameObject.Name != null)
                    {
                        dbDict.Add("name", gameObject.Name);
                    } else
                    {
                        dbDict.Add("name", "");
                    }
                    if (gameObject.Description != null)
                    {
                        dbDict.Add("description", gameObject.Description);
                    }
                    else
                    {
                        dbDict.Add("description", "");
                    }
                    if (gameObject.Year != null)
                    {
                        dbDict.Add("year", gameObject.Year);
                    }
                    else
                    {
                        dbDict.Add("year", "");
                    }
                    if (gameObject.Publisher != null)
                    {
                        dbDict.Add("publisher", gameObject.Publisher);
                    }
                    else
                    {
                        dbDict.Add("publisher", "");
                    }
                    dbDict.Add("demo", (int)gameObject.Demo);
                    if (gameObject.System != null)
                    {
                        dbDict.Add("system", gameObject.System);
                        dbDict.Add("platform", gameObject.System);
                    }
                    else
                    {
                        dbDict.Add("system", "");
                    }
                    if (gameObject.SystemVariant != null)
                    {
                        dbDict.Add("systemvariant", gameObject.SystemVariant);
                    }
                    else
                    {
                        dbDict.Add("systemvariant", "");
                    }
                    if (gameObject.Video != null)
                    {
                        dbDict.Add("video", gameObject.Video);
                    }
                    else
                    {
                        dbDict.Add("video", "");
                    }
                    if (gameObject.Country != null)
                    {
                        dbDict.Add("country", gameObject.Country);
                    }
                    else
                    {
                        dbDict.Add("country", "");
                    }
                    if (gameObject.Language != null)
                    {
                        dbDict.Add("language", gameObject.Language);
                    }
                    else
                    {
                        dbDict.Add("language", "");
                    }
                    if (gameObject.Copyright != null)
                    {
                        dbDict.Add("copyright", gameObject.Copyright);
                    }
                    else
                    {
                        dbDict.Add("copyright", "");
                    }

                    // store platform
                    int gameSystem = 0;
                    if (gameObject.System != null)
                    {
                        sql = "SELECT id FROM signatures_platforms WHERE platform=@platform";
                        
                        sigDB = db.ExecuteCMD(sql, dbDict);
                        if (sigDB.Rows.Count == 0)
                        {
                            // entry not present, insert it
                            sql = "INSERT INTO signatures_platforms (platform) VALUES (@platform); SELECT LAST_INSERT_ID()";
                            sigDB = db.ExecuteCMD(sql, dbDict);

                            gameSystem = int.Parse(sigDB.Rows[0][0].ToString());
                        } else
                        {
                            gameSystem = int.Parse(sigDB.Rows[0][0].ToString());
                        }
                    }
                    dbDict.Add("systemid", gameSystem);

                    // store publisher
                    int gamePublisher = 0;
                    if (gameObject.Publisher != null)
                    {
                        sql = "SELECT * FROM signatures_publishers WHERE publisher=@publisher";
                        
                        sigDB = db.ExecuteCMD(sql, dbDict);
                        if (sigDB.Rows.Count == 0)
                        {
                            // entry not present, insert it
                            sql = "INSERT INTO signatures_publishers (publisher) VALUES (@publisher); SELECT LAST_INSERT_ID()";
                            sigDB = db.ExecuteCMD(sql, dbDict);
                            gamePublisher = int.Parse(sigDB.Rows[0][0].ToString());
                        }
                        else
                        {
                            gamePublisher = int.Parse(sigDB.Rows[0][0].ToString());
                        }
                    }
                    dbDict.Add("publisherid", gamePublisher);

                    // store game
                    int gameId = 0;
                    sql = "SELECT * FROM signatures_games WHERE name=@name AND year=@year AND publisherid=@publisher AND systemid=@systemid AND country=@country AND language=@language";

                    sigDB = db.ExecuteCMD(sql, dbDict);
                    if (sigDB.Rows.Count == 0)
                    {
                        // entry not present, insert it
                        sql = "INSERT INTO signatures_games " +
                            "(name, description, year, publisherid, demo, systemid, systemvariant, video, country, language, copyright) VALUES " +
                            "(@name, @description, @year, @publisherid, @demo, @systemid, @systemvariant, @video, @country, @language, @copyright); SELECT LAST_INSERT_ID()";
                        sigDB = db.ExecuteCMD(sql, dbDict);

                        gameId = int.Parse(sigDB.Rows[0][0].ToString());
                    }
                    else
                    {
                        gameId = int.Parse(sigDB.Rows[0][0].ToString());
                    }

                    // store rom
                    foreach (RomSignatureObject.Game.Rom romObject in gameObject.Roms)
                    {
                        if (romObject.Md5 != null)
                        {
                            int romId = 0;
                            sql = "SELECT * FROM signatures_roms WHERE gameid=@gameid AND md5=@md5";
                            dbDict = new Dictionary<string, object>();
                            dbDict.Add("gameid", gameId);
                            if (romObject.Name != null)
                            {
                                dbDict.Add("name", romObject.Name);
                            } else
                            {
                                dbDict.Add("name", "");
                            }
                            if (romObject.Size != null)
                            {
                                dbDict.Add("size", romObject.Size);
                            }
                            else
                            {
                                dbDict.Add("size", 0);
                            }
                            if (romObject.Crc != null)
                            {
                                dbDict.Add("crc", romObject.Crc);
                            }
                            else
                            {
                                dbDict.Add("name", "");
                            }
                            dbDict.Add("md5", romObject.Md5);
                            if (romObject.Sha1 != null)
                            {
                                dbDict.Add("sha1", romObject.Sha1);
                            }
                            else
                            {
                                dbDict.Add("sha1", "");
                            }
                            if (romObject.DevelopmentStatus != null)
                            {
                                dbDict.Add("developmentstatus", romObject.DevelopmentStatus);
                            }
                            else
                            {
                                dbDict.Add("developmentstatus", "");
                            }
                            if (romObject.flags != null)
                            {
                                if (romObject.flags.Count > 0)
                                {
                                    dbDict.Add("flags", Newtonsoft.Json.JsonConvert.SerializeObject(romObject.flags));
                                } else
                                {
                                    dbDict.Add("flags", "{ }");
                                }
                            }
                            else
                            {
                                dbDict.Add("flags", "{ }");
                            }
                            dbDict.Add("romtype", (int)romObject.RomType);
                            if (romObject.RomTypeMedia != null)
                            {
                                dbDict.Add("romtypemedia", romObject.RomTypeMedia);
                            }
                            else
                            {
                                dbDict.Add("romtypemedia", "");
                            }
                            if (romObject.MediaLabel != null)
                            {
                                dbDict.Add("medialabel", romObject.MediaLabel);
                            }
                            else
                            {
                                dbDict.Add("medialabel", "");
                            }

                            sigDB = db.ExecuteCMD(sql, dbDict);
                            if (sigDB.Rows.Count == 0)
                            {
                                // entry not present, insert it
                                sql = "INSERT INTO signatures_roms (gameid, name, size, crc, md5, sha1, developmentstatus, flags, romtype, romtypemedia, medialabel) VALUES (@gameid, @name, @size, @crc, @md5, @sha1, @developmentstatus, @flags, @romtype, @romtypemedia, @medialabel); SELECT LAST_INSERT_ID()";
                                sigDB = db.ExecuteCMD(sql, dbDict);


                                romId = int.Parse(sigDB.Rows[0][0].ToString());
                            }
                            else
                            {
                                romId = int.Parse(sigDB.Rows[0][0].ToString());
                            }
                        }
                    }
                }
            }
        }
    }
}