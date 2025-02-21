using System;
using System.IO;
using gaseous_server.Classes;
using gaseous_signature_parser.models.RomSignatureObject;
using System.Data;

namespace gaseous_server.SignatureIngestors.XML
{
    public class XMLIngestor : QueueItemStatus
    {
        public void Import(string SearchPath, string ProcessedDirectory, gaseous_signature_parser.parser.SignatureParser XMLType)
        {
            // connect to database
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);

            string? XMLDBSearchPath = null;
            string? XMLDBProcessedDirectory = null;
            if (XMLType == gaseous_signature_parser.parser.SignatureParser.NoIntro)
            {
                XMLDBSearchPath = Path.Combine(SearchPath, "DB");
                XMLDBProcessedDirectory = Path.Combine(ProcessedDirectory, "DB");
                SearchPath = Path.Combine(SearchPath, "DAT");
                ProcessedDirectory = Path.Combine(ProcessedDirectory, "DAT");
            }

            // process provided files
            if (!Directory.Exists(SearchPath))
            {
                Directory.CreateDirectory(SearchPath);
            }
            if (!Directory.Exists(ProcessedDirectory))
            {
                Directory.CreateDirectory(ProcessedDirectory);
            }

            string[] PathContents = Directory.GetFiles(SearchPath);
            Array.Sort(PathContents);

            string[]? DBPathContents = null;
            if (XMLDBSearchPath != null)
            {
                if (!Directory.Exists(XMLDBSearchPath))
                {
                    Directory.CreateDirectory(XMLDBSearchPath);
                }
                if (!Directory.Exists(XMLDBProcessedDirectory))
                {
                    Directory.CreateDirectory(XMLDBProcessedDirectory);
                }

                DBPathContents = Directory.GetFiles(XMLDBSearchPath);
            }

            string sql = "";
            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            System.Data.DataTable sigDB;

            for (UInt16 i = 0; i < PathContents.Length; ++i)
            {
                string XMLFile = PathContents[i];

                SetStatus(i + 1, PathContents.Length, "Processing signature file: " + XMLFile);

                Logging.Log(Logging.LogType.Information, "Signature Ingest", "(" + (i + 1) + " / " + PathContents.Length + ") Processing " + XMLType.ToString() + " DAT file: " + XMLFile);

                string? DBFile = null;
                if (XMLDBSearchPath != null)
                {
                    switch (XMLType)
                    {
                        case gaseous_signature_parser.parser.SignatureParser.NoIntro:
                            for (UInt16 x = 0; x < DBPathContents.Length; x++)
                            {
                                string tempDBFileName = Path.GetFileNameWithoutExtension(DBPathContents[x].Replace(" (DB Export)", ""));
                                if (tempDBFileName == Path.GetFileNameWithoutExtension(XMLFile))
                                {
                                    DBFile = DBPathContents[x];
                                    Logging.Log(Logging.LogType.Information, "Signature Ingest", "Using DB file: " + DBFile);
                                    break;
                                }
                            }
                            break;
                    }
                }

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
                        // start parsing file
                        gaseous_signature_parser.parser Parser = new gaseous_signature_parser.parser();
                        RomSignatureObject Object = Parser.ParseSignatureDAT(XMLFile, DBFile, XMLType);

                        // store in database
                        string[] flipNameAndDescription = {
                            "MAMEArcade",
                            "MAMEMess"
                        };

                        // store source object
                        bool processGames = false;
                        if (Object.SourceMd5 != null)
                        {
                            int sourceId = 0;

                            sql = "SELECT * FROM Signatures_Sources WHERE `SourceMD5`=@sourcemd5";
                            dbDict = new Dictionary<string, object>
                            {
                                { "name", Common.ReturnValueIfNull(Object.Name, "") },
                                { "description", Common.ReturnValueIfNull(Object.Description, "") },
                                { "category", Common.ReturnValueIfNull(Object.Category, "") },
                                { "version", Common.ReturnValueIfNull(Object.Version, "") },
                                { "author", Common.ReturnValueIfNull(Object.Author, "") },
                                { "email", Common.ReturnValueIfNull(Object.Email, "") },
                                { "homepage", Common.ReturnValueIfNull(Object.Homepage, "") }
                            };
                            if (Object.Url == null)
                            {
                                dbDict.Add("uri", "");
                            }
                            else
                            {
                                dbDict.Add("uri", Common.ReturnValueIfNull(Object.Url.ToString(), ""));
                            }
                            dbDict.Add("sourcetype", Common.ReturnValueIfNull(Object.SourceType, ""));
                            dbDict.Add("sourcemd5", Object.SourceMd5);
                            dbDict.Add("sourcesha1", Object.SourceSHA1);

                            sigDB = db.ExecuteCMD(sql, dbDict);
                            if (sigDB.Rows.Count == 0)
                            {
                                // entry not present, insert it
                                sql = "INSERT INTO Signatures_Sources (`Name`, `Description`, `Category`, `Version`, `Author`, `Email`, `Homepage`, `Url`, `SourceType`, `SourceMD5`, `SourceSHA1`) VALUES (@name, @description, @category, @version, @author, @email, @homepage, @uri, @sourcetype, @sourcemd5, @sourcesha1); SELECT CAST(LAST_INSERT_ID() AS SIGNED);";

                                sigDB = db.ExecuteCMD(sql, dbDict);

                                sourceId = Convert.ToInt32(sigDB.Rows[0][0]);

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

                                    List<int> gameCountries = new List<int>();
                                    if (
                                        gameObject.Country != null
                                        )
                                    {
                                        foreach (string country in gameObject.Country.Keys)
                                        {
                                            int countryId = -1;
                                            countryId = Common.GetLookupByCode(Common.LookupTypes.Country, (string)Common.ReturnValueIfNull(country.Trim(), ""));
                                            if (countryId == -1)
                                            {
                                                countryId = Common.GetLookupByValue(Common.LookupTypes.Country, (string)Common.ReturnValueIfNull(country.Trim(), ""));

                                                if (countryId == -1)
                                                {
                                                    Logging.Log(Logging.LogType.Warning, "Signature Ingest", "Unable to locate country id for " + country.Trim());
                                                    sql = "INSERT INTO Country (`Code`, `Value`) VALUES (@code, @name); SELECT CAST(LAST_INSERT_ID() AS SIGNED);";
                                                    Dictionary<string, object> countryDict = new Dictionary<string, object>{
                                                        { "code", country.Trim() },
                                                        { "name", country.Trim() }
                                                    };
                                                    countryId = int.Parse(db.ExecuteCMD(sql, countryDict).Rows[0][0].ToString());
                                                }
                                            }

                                            if (countryId > 0)
                                            {
                                                gameCountries.Add(countryId);
                                            }
                                        }
                                    }

                                    List<int> gameLanguages = new List<int>();
                                    if (
                                        gameObject.Language != null
                                        )
                                    {
                                        foreach (string language in gameObject.Language.Keys)
                                        {
                                            int languageId = -1;
                                            languageId = Common.GetLookupByCode(Common.LookupTypes.Language, (string)Common.ReturnValueIfNull(language.Trim(), ""));
                                            if (languageId == -1)
                                            {
                                                languageId = Common.GetLookupByValue(Common.LookupTypes.Language, (string)Common.ReturnValueIfNull(language.Trim(), ""));

                                                if (languageId == -1)
                                                {
                                                    Logging.Log(Logging.LogType.Warning, "Signature Ingest", "Unable to locate language id for " + language.Trim());
                                                    sql = "INSERT INTO Language (`Code`, `Value`) VALUES (@code, @name); SELECT CAST(LAST_INSERT_ID() AS SIGNED);";
                                                    Dictionary<string, object> langDict = new Dictionary<string, object>{
                                                        { "code", language.Trim() },
                                                        { "name", language.Trim() }
                                                    };
                                                    languageId = int.Parse(db.ExecuteCMD(sql, langDict).Rows[0][0].ToString());
                                                }
                                            }

                                            if (languageId > 0)
                                            {
                                                gameLanguages.Add(languageId);
                                            }
                                        }
                                    }

                                    dbDict.Add("copyright", Common.ReturnValueIfNull(gameObject.Copyright, ""));

                                    // store platform
                                    int gameSystem = 0;
                                    if (gameObject.System != null)
                                    {
                                        sql = "SELECT `Id` FROM Signatures_Platforms WHERE `Platform`=@platform";

                                        sigDB = db.ExecuteCMD(sql, dbDict);
                                        if (sigDB.Rows.Count == 0)
                                        {
                                            // entry not present, insert it
                                            sql = "INSERT INTO Signatures_Platforms (`Platform`) VALUES (@platform); SELECT CAST(LAST_INSERT_ID() AS SIGNED);";
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
                                        sql = "SELECT * FROM Signatures_Publishers WHERE `Publisher`=@publisher";

                                        sigDB = db.ExecuteCMD(sql, dbDict);
                                        if (sigDB.Rows.Count == 0)
                                        {
                                            // entry not present, insert it
                                            sql = "INSERT INTO Signatures_Publishers (`Publisher`) VALUES (@publisher); SELECT CAST(LAST_INSERT_ID() AS SIGNED);";
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
                                    long gameId = 0;
                                    sql = "SELECT * FROM Signatures_Games WHERE `Name`=@name AND `Year`=@year AND `PublisherId`=@publisherid AND `SystemId`=@systemid";

                                    sigDB = db.ExecuteCMD(sql, dbDict);
                                    if (sigDB.Rows.Count == 0)
                                    {
                                        // entry not present, insert it
                                        sql = "INSERT INTO Signatures_Games " +
                                            "(`Name`, `Description`, `Year`, `PublisherId`, `Demo`, `SystemId`, `SystemVariant`, `Video`, `Copyright`) VALUES " +
                                            "(@name, @description, @year, @publisherid, @demo, @systemid, @systemvariant, @video, @copyright); SELECT CAST(LAST_INSERT_ID() AS SIGNED);";
                                        sigDB = db.ExecuteCMD(sql, dbDict);

                                        gameId = Convert.ToInt32(sigDB.Rows[0][0]);
                                    }
                                    else
                                    {
                                        gameId = (int)sigDB.Rows[0][0];
                                    }

                                    // insert countries
                                    foreach (int gameCountry in gameCountries)
                                    {
                                        try
                                        {
                                            sql = "SELECT * FROM Signatures_Games_Countries WHERE GameId = @gameid AND CountryId = @Countryid";
                                            Dictionary<string, object> countryDict = new Dictionary<string, object>{
                                                { "gameid", gameId },
                                                { "Countryid", gameCountry }
                                            };
                                            if (db.ExecuteCMD(sql, countryDict).Rows.Count == 0)
                                            {
                                                sql = "INSERT INTO Signatures_Games_Countries (GameId, CountryId) VALUES (@gameid, @Countryid)";
                                                db.ExecuteCMD(sql, countryDict);
                                            }
                                        }
                                        catch
                                        {
                                            Console.WriteLine("Game id: " + gameId + " with Country " + gameCountry);
                                        }
                                    }

                                    // insert languages
                                    foreach (int gameLanguage in gameLanguages)
                                    {
                                        try
                                        {
                                            sql = "SELECT * FROM Signatures_Games_Languages WHERE GameId = @gameid AND LanguageId = @languageid";
                                            Dictionary<string, object> langDict = new Dictionary<string, object>{
                                                { "gameid", gameId },
                                                { "languageid", gameLanguage }
                                            };
                                            if (db.ExecuteCMD(sql, langDict).Rows.Count == 0)
                                            {
                                                sql = "INSERT INTO Signatures_Games_Languages (GameId, LanguageId) VALUES (@gameid, @languageid)";
                                                db.ExecuteCMD(sql, langDict);
                                            }
                                        }
                                        catch
                                        {
                                            Console.WriteLine("Game id: " + gameId + " with language " + gameLanguage);
                                        }
                                    }

                                    // store rom
                                    foreach (RomSignatureObject.Game.Rom romObject in gameObject.Roms)
                                    {
                                        if (romObject.Md5 != null || romObject.Sha1 != null)
                                        {
                                            long romId = 0;
                                            sql = "SELECT * FROM Signatures_Roms WHERE `GameId`=@gameid AND (`MD5`=@md5 OR `SHA1`=@sha1)";
                                            dbDict = new Dictionary<string, object>();
                                            dbDict.Add("gameid", gameId);
                                            dbDict.Add("name", Common.ReturnValueIfNull(romObject.Name, ""));
                                            dbDict.Add("size", Common.ReturnValueIfNull(romObject.Size, "0"));
                                            dbDict.Add("crc", Common.ReturnValueIfNull(romObject.Crc, "").ToString().ToLower());
                                            dbDict.Add("md5", Common.ReturnValueIfNull(romObject.Md5, "").ToString().ToLower());
                                            dbDict.Add("sha1", Common.ReturnValueIfNull(romObject.Sha1, "").ToString().ToLower());
                                            dbDict.Add("developmentstatus", Common.ReturnValueIfNull(romObject.DevelopmentStatus, ""));

                                            if (romObject.Attributes != null)
                                            {
                                                if (romObject.Attributes.Count > 0)
                                                {
                                                    dbDict.Add("attributes", Newtonsoft.Json.JsonConvert.SerializeObject(romObject.Attributes));
                                                }
                                                else
                                                {
                                                    dbDict.Add("attributes", "");
                                                }
                                            }
                                            else
                                            {
                                                dbDict.Add("attributes", "");
                                            }
                                            dbDict.Add("romtype", (int)romObject.RomType);
                                            dbDict.Add("romtypemedia", Common.ReturnValueIfNull(romObject.RomTypeMedia, ""));
                                            dbDict.Add("medialabel", Common.ReturnValueIfNull(romObject.MediaLabel, ""));
                                            dbDict.Add("metadatasource", romObject.SignatureSource);
                                            dbDict.Add("ingestorversion", 2);

                                            sigDB = db.ExecuteCMD(sql, dbDict);
                                            if (sigDB.Rows.Count == 0)
                                            {
                                                // entry not present, insert it
                                                sql = "INSERT INTO Signatures_Roms (`GameId`, `Name`, `Size`, `CRC`, `MD5`, `SHA1`, `DevelopmentStatus`, `Attributes`, `RomType`, `RomTypeMedia`, `MediaLabel`, `MetadataSource`, `IngestorVersion`) VALUES (@gameid, @name, @size, @crc, @md5, @sha1, @developmentstatus, @attributes, @romtype, @romtypemedia, @medialabel, @metadatasource, @ingestorversion); SELECT CAST(LAST_INSERT_ID() AS SIGNED);";
                                                sigDB = db.ExecuteCMD(sql, dbDict);

                                                romId = Convert.ToInt32(sigDB.Rows[0][0]);
                                            }
                                            else
                                            {
                                                romId = (int)sigDB.Rows[0][0];
                                            }

                                            // map the rom to the source
                                            sql = "SELECT * FROM Signatures_RomToSource WHERE SourceId=@sourceid AND RomId=@romid;";
                                            dbDict.Add("romid", romId);
                                            dbDict.Add("sourceId", sourceId);

                                            sigDB = db.ExecuteCMD(sql, dbDict);
                                            if (sigDB.Rows.Count == 0)
                                            {
                                                sql = "INSERT INTO Signatures_RomToSource (`SourceId`, `RomId`) VALUES (@sourceid, @romid);";
                                                db.ExecuteCMD(sql, dbDict);
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        File.Move(XMLFile, Path.Combine(ProcessedDirectory, Path.GetFileName(XMLFile)));
                        if (DBFile != null)
                        {
                            File.Move(DBFile, Path.Combine(XMLDBProcessedDirectory, Path.GetFileName(DBFile)));
                        }
                    }
                    catch (Exception ex)
                    {
                        Logging.Log(Logging.LogType.Warning, "Signature Ingest", "Error ingesting " + XMLType.ToString() + " file: " + XMLFile, ex);
                    }
                }
                else
                {
                    Logging.Log(Logging.LogType.Information, "Signature Ingest", "Rejecting already imported " + XMLType.ToString() + " file: " + XMLFile);
                    File.Move(XMLFile, Path.Combine(ProcessedDirectory, Path.GetFileName(XMLFile)));
                    if (DBFile != null)
                    {
                        File.Move(DBFile, Path.Combine(XMLDBProcessedDirectory, Path.GetFileName(DBFile)));
                    }
                }
            }
            ClearStatus();
        }
    }
}