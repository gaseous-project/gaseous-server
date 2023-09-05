using System;
using System.IO;
using MySql.Data.MySqlClient;
using gaseous_signature_parser.models.RomSignatureObject;
using gaseous_tools;
using MySqlX.XDevAPI;
using System.Data;

namespace gaseous_server.SignatureIngestors.XML
{
    public class XMLIngestor
    {
        public void Import(string SearchPath, gaseous_signature_parser.parser.SignatureParser XMLType)
        {
            // connect to database
            Database db = new gaseous_tools.Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);

            // process provided files
            Logging.Log(Logging.LogType.Information, "Signature Ingestor - XML", "Importing from " + SearchPath);
            if (!Directory.Exists(SearchPath))
            {
                Directory.CreateDirectory(SearchPath);
            }

            string[] PathContents = Directory.GetFiles(SearchPath);
            Array.Sort(PathContents);

            string sql = "";
            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            System.Data.DataTable sigDB;

            for (UInt16 i = 0; i < PathContents.Length; ++i)
            {
                string XMLFile = PathContents[i];

                // check xml file md5
                Common.hashObject hashObject = new Common.hashObject(XMLFile);
                sql = "SELECT * FROM Signatures_Sources WHERE SourceMD5=@sourcemd5";
                dbDict = new Dictionary<string, object>();
                dbDict.Add("sourcemd5", hashObject.md5hash);
                sigDB = db.ExecuteCMD(sql, dbDict);

                if (sigDB.Rows.Count == 0)
                {
                    try
                    {
                        Logging.Log(Logging.LogType.Information, "Signature Ingestor - XML", "Importing file: " + XMLFile);

                        // start parsing file
                        gaseous_signature_parser.parser Parser = new gaseous_signature_parser.parser();
                        RomSignatureObject Object = Parser.ParseSignatureDAT(XMLFile, XMLType);

                        // store in database
                        string[] flipNameAndDescription = {
                            "MAMEArcade",
                            "MAMEMess"
                        };

                        // store source object
                        bool processGames = false;
                        if (Object.SourceMd5 != null)
                        {
                            sql = "SELECT * FROM Signatures_Sources WHERE SourceMD5=@sourcemd5";
                            dbDict = new Dictionary<string, object>();
                            dbDict.Add("name", Common.ReturnValueIfNull(Object.Name, ""));
                            dbDict.Add("description", Common.ReturnValueIfNull(Object.Description, ""));
                            dbDict.Add("category", Common.ReturnValueIfNull(Object.Category, ""));
                            dbDict.Add("version", Common.ReturnValueIfNull(Object.Version, ""));
                            dbDict.Add("author", Common.ReturnValueIfNull(Object.Author, ""));
                            dbDict.Add("email", Common.ReturnValueIfNull(Object.Email, ""));
                            dbDict.Add("homepage", Common.ReturnValueIfNull(Object.Homepage, ""));
                            dbDict.Add("uri", Common.ReturnValueIfNull(Object.Url, ""));
                            dbDict.Add("sourcetype", Common.ReturnValueIfNull(Object.SourceType, ""));
                            dbDict.Add("sourcemd5", Object.SourceMd5);
                            dbDict.Add("sourcesha1", Object.SourceSHA1);

                            sigDB = db.ExecuteCMD(sql, dbDict);
                            if (sigDB.Rows.Count == 0)
                            {
                                // entry not present, insert it
                                sql = "INSERT INTO Signatures_Sources (Name, Description, Category, Version, Author, Email, Homepage, Url, SourceType, SourceMD5, SourceSHA1) VALUES (@name, @description, @category, @version, @author, @email, @homepage, @uri, @sourcetype, @sourcemd5, @sourcesha1)";

                                db.ExecuteCMD(sql, dbDict);

                                processGames = true;
                            }

                            if (processGames == true)
                            {
                                for (int x = 0; x < Object.Games.Count; ++x)
                                {
                                    RomSignatureObject.Game gameObject = Object.Games[x];

                                    // set up game dictionary
                                    dbDict = new Dictionary<string, object>();
                                    if (flipNameAndDescription.Contains(Object.SourceType)) 
                                    {
                                        dbDict.Add("name", Common.ReturnValueIfNull(gameObject.Description, ""));
                                        dbDict.Add("description", Common.ReturnValueIfNull(gameObject.Name, ""));
                                    }
                                    else
                                    {
                                        dbDict.Add("name", Common.ReturnValueIfNull(gameObject.Name, ""));
                                        dbDict.Add("description", Common.ReturnValueIfNull(gameObject.Description, ""));
                                    }
                                    dbDict.Add("year", Common.ReturnValueIfNull(gameObject.Year, ""));
                                    dbDict.Add("publisher", Common.ReturnValueIfNull(gameObject.Publisher, ""));
                                    dbDict.Add("demo", (int)gameObject.Demo);
                                    dbDict.Add("system", Common.ReturnValueIfNull(gameObject.System, ""));
                                    dbDict.Add("platform", Common.ReturnValueIfNull(gameObject.System, ""));
                                    dbDict.Add("systemvariant", Common.ReturnValueIfNull(gameObject.SystemVariant, ""));
                                    dbDict.Add("video", Common.ReturnValueIfNull(gameObject.Video, ""));
                                    dbDict.Add("country", Common.ReturnValueIfNull(gameObject.Country, ""));
                                    dbDict.Add("language", Common.ReturnValueIfNull(gameObject.Language, ""));
                                    dbDict.Add("copyright", Common.ReturnValueIfNull(gameObject.Copyright, ""));

                                    // store platform
                                    int gameSystem = 0;
                                    if (gameObject.System != null)
                                    {
                                        sql = "SELECT Id FROM Signatures_Platforms WHERE Platform=@platform";

                                        sigDB = db.ExecuteCMD(sql, dbDict);
                                        if (sigDB.Rows.Count == 0)
                                        {
                                            // entry not present, insert it
                                            sql = "INSERT INTO Signatures_Platforms (Platform) VALUES (@platform); SELECT CAST(LAST_INSERT_ID() AS SIGNED);";
                                            sigDB = db.ExecuteCMD(sql, dbDict);

                                            gameSystem = Convert.ToInt32(sigDB.Rows[0][0]);
                                        }
                                        else
                                        {
                                            gameSystem = (int)sigDB.Rows[0][0];
                                        }
                                    }
                                    dbDict.Add("systemid", gameSystem);

                                    // store publisher
                                    int gamePublisher = 0;
                                    if (gameObject.Publisher != null)
                                    {
                                        sql = "SELECT * FROM Signatures_Publishers WHERE Publisher=@publisher";

                                        sigDB = db.ExecuteCMD(sql, dbDict);
                                        if (sigDB.Rows.Count == 0)
                                        {
                                            // entry not present, insert it
                                            sql = "INSERT INTO Signatures_Publishers (Publisher) VALUES (@publisher); SELECT CAST(LAST_INSERT_ID() AS SIGNED);";
                                            sigDB = db.ExecuteCMD(sql, dbDict);
                                            gamePublisher = Convert.ToInt32(sigDB.Rows[0][0]);
                                        }
                                        else
                                        {
                                            gamePublisher = (int)sigDB.Rows[0][0];
                                        }
                                    }
                                    dbDict.Add("publisherid", gamePublisher);

                                    // store game
                                    int gameId = 0;
                                    sql = "SELECT * FROM Signatures_Games WHERE Name=@name AND Year=@year AND Publisherid=@publisher AND Systemid=@systemid AND Country=@country AND Language=@language";

                                    sigDB = db.ExecuteCMD(sql, dbDict);
                                    if (sigDB.Rows.Count == 0)
                                    {
                                        // entry not present, insert it
                                        sql = "INSERT INTO Signatures_Games " +
                                            "(Name, Description, Year, PublisherId, Demo, SystemId, SystemVariant, Video, Country, Language, Copyright) VALUES " +
                                            "(@name, @description, @year, @publisherid, @demo, @systemid, @systemvariant, @video, @country, @language, @copyright); SELECT CAST(LAST_INSERT_ID() AS SIGNED);";
                                        sigDB = db.ExecuteCMD(sql, dbDict);

                                        gameId = Convert.ToInt32(sigDB.Rows[0][0]);
                                    }
                                    else
                                    {
                                        gameId = (int)sigDB.Rows[0][0];
                                    }

                                    // store rom
                                    foreach (RomSignatureObject.Game.Rom romObject in gameObject.Roms)
                                    {
                                        if (romObject.Md5 != null || romObject.Sha1 != null)
                                        {
                                            int romId = 0;
                                            sql = "SELECT * FROM Signatures_Roms WHERE GameId=@gameid AND MD5=@md5";
                                            dbDict = new Dictionary<string, object>();
                                            dbDict.Add("gameid", gameId);
                                            dbDict.Add("name", Common.ReturnValueIfNull(romObject.Name, ""));
                                            dbDict.Add("size", Common.ReturnValueIfNull(romObject.Size, ""));
                                            dbDict.Add("crc", Common.ReturnValueIfNull(romObject.Crc, ""));
                                            dbDict.Add("md5", Common.ReturnValueIfNull(romObject.Md5, ""));
                                            dbDict.Add("sha1", Common.ReturnValueIfNull(romObject.Sha1, ""));
                                            dbDict.Add("developmentstatus", Common.ReturnValueIfNull(romObject.DevelopmentStatus, ""));

                                            if (romObject.Attributes != null)
                                            {
                                                if (romObject.Attributes.Count > 0)
                                                {
                                                    dbDict.Add("attributes", Newtonsoft.Json.JsonConvert.SerializeObject(romObject.Attributes));
                                                }
                                                else
                                                {
                                                    dbDict.Add("attributes", "[ ]");
                                                }
                                            }
                                            else
                                            {
                                                dbDict.Add("attributes", "[ ]");
                                            }
                                            dbDict.Add("romtype", (int)romObject.RomType);
                                            dbDict.Add("romtypemedia", Common.ReturnValueIfNull(romObject.RomTypeMedia, ""));
                                            dbDict.Add("medialabel", Common.ReturnValueIfNull(romObject.MediaLabel, ""));
                                            dbDict.Add("metadatasource", romObject.SignatureSource);

                                            sigDB = db.ExecuteCMD(sql, dbDict);
                                            if (sigDB.Rows.Count == 0)
                                            {
                                                // entry not present, insert it
                                                sql = "INSERT INTO Signatures_Roms (GameId, Name, Size, CRC, MD5, SHA1, DevelopmentStatus, Attributes, RomType, RomTypeMedia, MediaLabel, MetadataSource) VALUES (@gameid, @name, @size, @crc, @md5, @sha1, @developmentstatus, @attributes, @romtype, @romtypemedia, @medialabel, @metadatasource); SELECT CAST(LAST_INSERT_ID() AS SIGNED);";
                                                sigDB = db.ExecuteCMD(sql, dbDict);


                                                romId = Convert.ToInt32(sigDB.Rows[0][0]);
                                            }
                                            else
                                            {
                                                romId = (int)sigDB.Rows[0][0];
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logging.Log(Logging.LogType.Warning, "Signature Ingestor - XML", "Invalid import file: " + XMLFile, ex);
                    }
                }
                else
                {
                    Logging.Log(Logging.LogType.Debug, "Signature Ingestor - XML", "Rejecting already imported file: " + XMLFile);
                }
            }
        }

        public void MigrateMetadatVersion() {
            Database db = new gaseous_tools.Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "";
            Dictionary<string, object> dbDict = new Dictionary<string, object>();

            // update signature roms to v2
            sql = "SELECT Id, Flags, Attributes, IngestorVersion FROM Signatures_Roms WHERE IngestorVersion = 1";
            DataTable data = db.ExecuteCMD(sql);
            if (data.Rows.Count > 0)
            {
                Logging.Log(Logging.LogType.Information, "Signature Ingestor - Database Update", "Updating " + data.Rows.Count + " database entries");
                int Counter = 0;
                int LastCounterCheck = 0;
                foreach (DataRow row in data.Rows) 
                {
                    List<string> Flags = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>((string)Common.ReturnValueIfNull(row["flags"], "[]"));
                    List<KeyValuePair<string, object>> Attributes = new List<KeyValuePair<string, object>>();
                    foreach (string Flag in Flags)
                    {
                        if (Flag.StartsWith("a"))
                        {
                            Attributes.Add(
                                new KeyValuePair<string, object>(
                                    "a",
                                    Flag
                                )
                            );
                        }
                        else
                        {
                            string[] FlagCompare = Flag.Split(' ');
                            switch (FlagCompare[0].Trim().ToLower())
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
                                    string shavedToken = Flag.Substring(FlagCompare[0].Trim().Length).Trim();
                                    Attributes.Add(new KeyValuePair<string, object>(
                                        FlagCompare[0].Trim().ToLower(),
                                        shavedToken
                                    ));
                                    break;
                            }
                        }
                    }

                    string AttributesJson;
                    if (Attributes.Count > 0)
                    {
                        AttributesJson = Newtonsoft.Json.JsonConvert.SerializeObject(Attributes);
                    }
                    else
                    {
                        AttributesJson = "[]";
                    }

                    string updateSQL = "UPDATE Signatures_Roms SET Attributes=@attributes, IngestorVersion=2 WHERE Id=@id";
                    dbDict = new Dictionary<string, object>();
                    dbDict.Add("attributes", AttributesJson);
                    dbDict.Add("id", (int)row["Id"]);
                    db.ExecuteCMD(updateSQL, dbDict);

                    if ((Counter - LastCounterCheck) > 10) 
                    {
                        LastCounterCheck = Counter;
                        Logging.Log(Logging.LogType.Information, "Signature Ingestor - Database Update", "Updating " + Counter + " / " + data.Rows.Count + " database entries");
                    }
                    Counter += 1;
                }
            }
        }
    }
}