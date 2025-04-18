using System;
using System.Data;
using System.Reflection;
using gaseous_server.Classes.Metadata;
using gaseous_server.Models;

namespace gaseous_server.Classes
{
    public static class DatabaseMigration
    {
        public static List<int> BackgroundUpgradeTargetSchemaVersions = new List<int>();

        public static void PreUpgradeScript(int TargetSchemaVersion, Database.databaseType? DatabaseType)
        {
            // load resources
            var assembly = Assembly.GetExecutingAssembly();

            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "";
            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            DataTable data;

            Logging.Log(Logging.LogType.Information, "Database", "Checking for pre-upgrade for schema version " + TargetSchemaVersion);

            switch (DatabaseType)
            {
                case Database.databaseType.MySql:
                    switch (TargetSchemaVersion)
                    {
                        case 1005:
                            Logging.Log(Logging.LogType.Information, "Database", "Running pre-upgrade for schema version " + TargetSchemaVersion);

                            // there was a mistake at dbschema version 1004-1005
                            // the first preview release of v1.7 reused dbschema version 1004
                            // if table "Relation_Game_AgeRatings" exists - then we need to apply the gaseous-fix-1005.sql script before applying the standard 1005 script
                            sql = "SELECT table_name FROM information_schema.tables WHERE table_schema = @dbname AND table_name = @tablename;";
                            dbDict.Add("dbname", Config.DatabaseConfiguration.DatabaseName);
                            dbDict.Add("tablename", "Relation_Game_AgeRatings");
                            data = db.ExecuteCMD(sql, dbDict);
                            if (data.Rows.Count == 0)
                            {
                                Logging.Log(Logging.LogType.Information, "Database", "Schema version " + TargetSchemaVersion + " requires a table which is missing.");

                                string resourceName = "gaseous_server.Support.Database.MySQL.gaseous-fix-1005.sql";
                                string dbScript = "";

                                string[] resources = Assembly.GetExecutingAssembly().GetManifestResourceNames();
                                if (resources.Contains(resourceName))
                                {
                                    using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                                    using (StreamReader reader = new StreamReader(stream))
                                    {
                                        dbScript = reader.ReadToEnd();

                                        // apply schema!
                                        Logging.Log(Logging.LogType.Information, "Database", "Applying schema version fix prior to version 1005");
                                        db.ExecuteCMD(dbScript, dbDict, 180);
                                    }
                                }
                            }
                            break;

                        case 1025:
                            Logging.Log(Logging.LogType.Information, "Database", "Running pre-upgrade for schema version " + TargetSchemaVersion);
                            // create the basic relation tables
                            // this is a blocking task
                            Storage.CreateRelationsTables<IGDB.Models.Game>();
                            Storage.CreateRelationsTables<IGDB.Models.Platform>();
                            break;
                    }
                    break;
            }
        }

        public static void PostUpgradeScript(int TargetSchemaVersion, Database.databaseType? DatabaseType)
        {
            var assembly = Assembly.GetExecutingAssembly();

            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "";
            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            DataTable data;

            switch (DatabaseType)
            {
                case Database.databaseType.MySql:
                    switch (TargetSchemaVersion)
                    {
                        case 1002:
                            // this is a safe background task
                            BackgroundUpgradeTargetSchemaVersions.Add(1002);
                            break;

                        case 1004:
                            // needs to run on start up

                            // copy root path to new libraries format
                            string oldRoot = Path.Combine(Config.LibraryConfiguration.LibraryRootDirectory, "Library");
                            sql = "INSERT INTO GameLibraries (Name, Path, DefaultLibrary, DefaultPlatform) VALUES (@name, @path, @defaultlibrary, @defaultplatform); SELECT CAST(LAST_INSERT_ID() AS SIGNED);";
                            dbDict.Add("name", "Default");
                            dbDict.Add("path", oldRoot);
                            dbDict.Add("defaultlibrary", 1);
                            dbDict.Add("defaultplatform", 0);
                            data = db.ExecuteCMD(sql, dbDict);

                            // apply the new library id to the existing roms
                            sql = "UPDATE Games_Roms SET LibraryId=@libraryid;";
                            dbDict.Clear();
                            dbDict.Add("libraryid", data.Rows[0][0]);
                            db.ExecuteCMD(sql, dbDict);
                            break;

                        case 1016:
                            // delete old format LastRun_* settings from settings table
                            sql = "DELETE FROM Settings WHERE Setting LIKE 'LastRun_%';";
                            db.ExecuteNonQuery(sql);
                            break;

                        case 1023:
                            // load country list
                            Logging.Log(Logging.LogType.Information, "Database Upgrade", "Adding country look up table contents");

                            string countryResourceName = "gaseous_server.Support.Country.txt";
                            using (Stream stream = assembly.GetManifestResourceStream(countryResourceName))
                            using (StreamReader reader = new StreamReader(stream))
                            {
                                do
                                {
                                    string[] line = reader.ReadLine().Split("|");

                                    sql = "INSERT INTO Country (Code, Value) VALUES (@code, @value);";
                                    dbDict = new Dictionary<string, object>{
                                { "code", line[0] },
                                { "value", line[1] }
                            };
                                    db.ExecuteNonQuery(sql, dbDict);
                                } while (reader.EndOfStream == false);
                            }

                            // load language list
                            Logging.Log(Logging.LogType.Information, "Database Upgrade", "Adding language look up table contents");

                            string languageResourceName = "gaseous_server.Support.Language.txt";
                            using (Stream stream = assembly.GetManifestResourceStream(languageResourceName))
                            using (StreamReader reader = new StreamReader(stream))
                            {
                                do
                                {
                                    string[] line = reader.ReadLine().Split("|");

                                    sql = "INSERT INTO Language (Code, Value) VALUES (@code, @value);";
                                    dbDict = new Dictionary<string, object>{
                                { "code", line[0] },
                                { "value", line[1] }
                            };
                                    db.ExecuteNonQuery(sql, dbDict);
                                } while (reader.EndOfStream == false);
                            }
                            break;

                        case 1024:
                            // create profiles for all existing users
                            sql = "SELECT * FROM Users;";
                            data = db.ExecuteCMD(sql);
                            foreach (DataRow row in data.Rows)
                            {
                                // get legacy avatar from UserAvatars table
                                sql = "SELECT Avatar FROM UserAvatars WHERE UserId = @userid;";
                                dbDict = new Dictionary<string, object>
                                {
                                    { "userid", row["Id"] }
                                };
                                DataTable avatarData = db.ExecuteCMD(sql, dbDict);

                                sql = "INSERT INTO UserProfiles (Id, UserId, DisplayName, Quip, Avatar, AvatarExtension, UnstructuredData) VALUES (@id, @userid, @displayname, @quip, @avatar, @avatarextension, @data);";
                                dbDict = new Dictionary<string, object>
                                {
                                    { "id", Guid.NewGuid() },
                                    { "userid", row["Id"] },
                                    { "displayname", row["Email"] },
                                    { "quip", "" },
                                    { "avatar", avatarData.Rows.Count > 0 ? avatarData.Rows[0]["Avatar"] : null },
                                    { "avatarextension", avatarData.Rows.Count > 0 ? ".jpg" : null },
                                    { "data", "{}" }
                                };
                                db.ExecuteNonQuery(sql, dbDict);
                            }

                            // update all rom paths to use the new format
                            sql = "SELECT * FROM GameLibraries;";
                            data = db.ExecuteCMD(sql);
                            foreach (DataRow row in data.Rows)
                            {
                                sql = "SELECT * FROM Games_Roms WHERE LibraryId = @libraryid;";
                                dbDict = new Dictionary<string, object>
                                {
                                    { "libraryid", row["Id"] }
                                };
                                DataTable romData = db.ExecuteCMD(sql, dbDict);

                                string libraryRootPath = (string)row["Path"];
                                if (libraryRootPath.EndsWith(Path.DirectorySeparatorChar.ToString()) == false)
                                {
                                    libraryRootPath += Path.DirectorySeparatorChar;
                                }

                                bool GetLastThreeElements = false;
                                if ((int)row["DefaultLibrary"] == 1)
                                {
                                    GetLastThreeElements = true;
                                }

                                foreach (DataRow romRow in romData.Rows)
                                {
                                    string existingPath = (string)romRow["RelativePath"];
                                    string newPath = "";

                                    if (GetLastThreeElements == true)
                                    {
                                        // strip all but the last 3 elements from existingPath separated by directory separator
                                        // this mode only works for the default library
                                        string[] pathParts = existingPath.Split(Path.DirectorySeparatorChar);
                                        if (pathParts.Length > 3)
                                        {
                                            newPath = Path.Combine(pathParts[pathParts.Length - 3], pathParts[pathParts.Length - 2], pathParts[pathParts.Length - 1]);
                                        }
                                        else
                                        {
                                            newPath = existingPath;
                                        }
                                    }
                                    else
                                    {
                                        // strip the library root path from the existing path
                                        if (existingPath.StartsWith(libraryRootPath))
                                        {
                                            newPath = existingPath.Substring(libraryRootPath.Length);
                                        }
                                        else
                                        {
                                            newPath = existingPath;
                                        }
                                    }

                                    Logging.Log(Logging.LogType.Information, "Database Upgrade", "Updating ROM path from " + existingPath + " to " + newPath);

                                    sql = "UPDATE Games_Roms SET RelativePath = @newpath WHERE Id = @id;";
                                    dbDict = new Dictionary<string, object>
                                    {
                                        { "newpath", newPath },
                                        { "id", romRow["Id"] }
                                    };
                                    db.ExecuteNonQuery(sql, dbDict);
                                }
                            }

                            // get all tables that have the prefix "Relation_" and drop them
                            sql = "SELECT table_name FROM information_schema.tables WHERE table_schema = @dbname AND table_name LIKE 'Relation_%';";
                            dbDict = new Dictionary<string, object>
                            {
                                { "dbname", Config.DatabaseConfiguration.DatabaseName }
                            };
                            data = db.ExecuteCMD(sql, dbDict);
                            foreach (DataRow row in data.Rows)
                            {
                                sql = "DROP TABLE " + (string)row["table_name"] + ";";
                                db.ExecuteNonQuery(sql);
                            }

                            // migrating metadata is a safe background task
                            BackgroundUpgradeTargetSchemaVersions.Add(1024);
                            break;
                    }
                    break;
            }
        }

        public static void UpgradeScriptBackgroundTasks()
        {
            foreach (int TargetSchemaVersion in BackgroundUpgradeTargetSchemaVersions)
            {
                switch (TargetSchemaVersion)
                {
                    case 1002:
                        MySql_1002_MigrateMetadataVersion();
                        break;

                    case 1024:
                        MySql_1024_MigrateMetadataVersion();
                        break;
                }
            }
        }

        public static void MySql_1002_MigrateMetadataVersion()
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
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

        public static void MySql_1024_MigrateMetadataVersion()
        {
            FileSignature fileSignature = new FileSignature();

            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "SELECT * FROM view_Games_Roms WHERE RomDataVersion = 1;";
            DataTable data = db.ExecuteCMD(sql);
            long count = 1;
            foreach (DataRow row in data.Rows)
            {
                Logging.Log(Logging.LogType.Information, "Database Migration", "Updating ROM table for ROM (" + count + " / " + data.Rows.Count + "): " + (string)row["Name"]);

                GameLibrary.LibraryItem library = GameLibrary.GetLibrary((int)row["LibraryId"]);
                Common.hashObject hash = new Common.hashObject()
                {
                    md5hash = (string)row["MD5"],
                    sha1hash = (string)row["SHA1"]
                };
                Signatures_Games signature = fileSignature.GetFileSignature(
                    library,
                    hash,
                    new FileInfo((string)row["Path"]),
                    (string)row["Path"]
                );

                HasheousClient.Models.Metadata.IGDB.Platform platform = Platforms.GetPlatform((long)row["PlatformId"]);

                ImportGame.StoreGame(library, hash, signature, platform, (string)row["Path"], (long)row["Id"]);

                count += 1;
            }
        }
    }
}