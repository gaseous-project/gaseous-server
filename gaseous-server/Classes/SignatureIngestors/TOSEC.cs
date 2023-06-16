﻿using System;
using System.IO;
using MySql.Data.MySqlClient;
using gaseous_romsignatureobject;
using gaseous_signature_parser.parsers;
using gaseous_tools;
using MySqlX.XDevAPI;

namespace gaseous_server.SignatureIngestors.TOSEC
{
    public class TOSECIngestor
    {
        public void Import(string SearchPath)
        {
            // connect to database
            Database db = new gaseous_tools.Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);

            // process provided files
            Logging.Log(Logging.LogType.Information, "Signature Ingestor - TOSEC", "Importing from " + SearchPath);
            if (Directory.Exists(Config.LibraryConfiguration.LibrarySignatureImportDirectory_TOSEC))
            {
                string[] tosecPathContents = Directory.GetFiles(SearchPath);
                Array.Sort(tosecPathContents);
                
                string sql = "";
                Dictionary<string, object> dbDict = new Dictionary<string, object>();
                System.Data.DataTable sigDB;

                for (UInt16 i = 0; i < tosecPathContents.Length; ++i)
                {
                    string tosecXMLFile = tosecPathContents[i];

                    // check tosec file md5
                    Common.hashObject hashObject = new Common.hashObject(tosecXMLFile);
                    sql = "SELECT * FROM signatures_sources WHERE sourcemd5=@sourcemd5";
                    dbDict = new Dictionary<string, object>();
                    dbDict.Add("sourcemd5", hashObject.md5hash);
                    sigDB = db.ExecuteCMD(sql, dbDict);

                    if (sigDB.Rows.Count == 0)
                    {
                        Logging.Log(Logging.LogType.Information, "Signature Ingestor - TOSEC", "Importing file: " + tosecXMLFile);

                        // start parsing file
                        TosecParser tosecParser = new TosecParser();
                        RomSignatureObject tosecObject = tosecParser.Parse(tosecXMLFile);

                        // store in database

                        // store source object
                        bool processGames = false;
                        if (tosecObject.SourceMd5 != null)
                        {
                            sql = "SELECT * FROM signatures_sources WHERE sourcemd5=@sourcemd5";
                            dbDict = new Dictionary<string, object>();
                            dbDict.Add("name", Common.ReturnValueIfNull(tosecObject.Name, ""));
                            dbDict.Add("description", Common.ReturnValueIfNull(tosecObject.Description, ""));
                            dbDict.Add("category", Common.ReturnValueIfNull(tosecObject.Category, ""));
                            dbDict.Add("version", Common.ReturnValueIfNull(tosecObject.Version, ""));
                            dbDict.Add("author", Common.ReturnValueIfNull(tosecObject.Author, ""));
                            dbDict.Add("email", Common.ReturnValueIfNull(tosecObject.Email, ""));
                            dbDict.Add("homepage", Common.ReturnValueIfNull(tosecObject.Homepage, ""));
                            dbDict.Add("uri", Common.ReturnValueIfNull(tosecObject.Url, ""));
                            dbDict.Add("sourcetype", Common.ReturnValueIfNull(tosecObject.SourceType, ""));
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

                                    // set up game dictionary
                                    dbDict = new Dictionary<string, object>();
                                    dbDict.Add("name", Common.ReturnValueIfNull(gameObject.Name, ""));
                                    dbDict.Add("description", Common.ReturnValueIfNull(gameObject.Description, ""));
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
                                        sql = "SELECT id FROM signatures_platforms WHERE platform=@platform";

                                        sigDB = db.ExecuteCMD(sql, dbDict);
                                        if (sigDB.Rows.Count == 0)
                                        {
                                            // entry not present, insert it
                                            sql = "INSERT INTO signatures_platforms (platform) VALUES (@platform); SELECT CAST(LAST_INSERT_ID() AS SIGNED);";
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
                                        sql = "SELECT * FROM signatures_publishers WHERE publisher=@publisher";

                                        sigDB = db.ExecuteCMD(sql, dbDict);
                                        if (sigDB.Rows.Count == 0)
                                        {
                                            // entry not present, insert it
                                            sql = "INSERT INTO signatures_publishers (publisher) VALUES (@publisher); SELECT CAST(LAST_INSERT_ID() AS SIGNED);";
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
                                    sql = "SELECT * FROM signatures_games WHERE name=@name AND year=@year AND publisherid=@publisher AND systemid=@systemid AND country=@country AND language=@language";

                                    sigDB = db.ExecuteCMD(sql, dbDict);
                                    if (sigDB.Rows.Count == 0)
                                    {
                                        // entry not present, insert it
                                        sql = "INSERT INTO signatures_games " +
                                            "(name, description, year, publisherid, demo, systemid, systemvariant, video, country, language, copyright) VALUES " +
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
                                        if (romObject.Md5 != null)
                                        {
                                            int romId = 0;
                                            sql = "SELECT * FROM signatures_roms WHERE gameid=@gameid AND md5=@md5";
                                            dbDict = new Dictionary<string, object>();
                                            dbDict.Add("gameid", gameId);
                                            dbDict.Add("name", Common.ReturnValueIfNull(romObject.Name, ""));
                                            dbDict.Add("size", Common.ReturnValueIfNull(romObject.Size, ""));
                                            dbDict.Add("crc", Common.ReturnValueIfNull(romObject.Crc, ""));
                                            dbDict.Add("md5", romObject.Md5);
                                            dbDict.Add("sha1", Common.ReturnValueIfNull(romObject.Sha1, ""));
                                            dbDict.Add("developmentstatus", Common.ReturnValueIfNull(romObject.DevelopmentStatus, ""));

                                            if (romObject.flags != null)
                                            {
                                                if (romObject.flags.Count > 0)
                                                {
                                                    dbDict.Add("flags", Newtonsoft.Json.JsonConvert.SerializeObject(romObject.flags));
                                                }
                                                else
                                                {
                                                    dbDict.Add("flags", "[ ]");
                                                }
                                            }
                                            else
                                            {
                                                dbDict.Add("flags", "[ ]");
                                            }
                                            dbDict.Add("romtype", (int)romObject.RomType);
                                            dbDict.Add("romtypemedia", Common.ReturnValueIfNull(romObject.RomTypeMedia, ""));
                                            dbDict.Add("medialabel", Common.ReturnValueIfNull(romObject.MediaLabel, ""));

                                            sigDB = db.ExecuteCMD(sql, dbDict);
                                            if (sigDB.Rows.Count == 0)
                                            {
                                                // entry not present, insert it
                                                sql = "INSERT INTO signatures_roms (gameid, name, size, crc, md5, sha1, developmentstatus, flags, romtype, romtypemedia, medialabel) VALUES (@gameid, @name, @size, @crc, @md5, @sha1, @developmentstatus, @flags, @romtype, @romtypemedia, @medialabel); SELECT CAST(LAST_INSERT_ID() AS SIGNED);";
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
                    else
                    {
                        Logging.Log(Logging.LogType.Debug, "Signature Ingestor - TOSEC", "Rejecting already imported file: " + tosecXMLFile);
                    }
                }
            }
        }
    }
}