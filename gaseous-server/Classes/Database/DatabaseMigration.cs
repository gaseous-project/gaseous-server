using System;
using System.Data;
using System.Reflection;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using gaseous_server.Classes.Metadata;
using gaseous_server.Classes.Plugins.MetadataProviders;
using gaseous_server.Models;

namespace gaseous_server.Classes
{
    public static class DatabaseMigration
    {
        public static List<int> BackgroundUpgradeTargetSchemaVersions = new List<int>();

        public static async Task PreUpgradeScript(int TargetSchemaVersion, Database.databaseType? DatabaseType)
        {
            // load resources
            var assembly = Assembly.GetExecutingAssembly();

            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "";
            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            DataTable data;

            Logging.LogKey(Logging.LogType.Information, "process.database", "database.checking_pre_upgrade_for_schema_version", null, new[] { TargetSchemaVersion.ToString() });

            switch (DatabaseType)
            {
                case Database.databaseType.MySql:
                    switch (TargetSchemaVersion)
                    {
                        case 1005:
                            Logging.LogKey(Logging.LogType.Information, "process.database", "database.running_pre_upgrade_for_schema_version", null, new[] { TargetSchemaVersion.ToString() });

                            // there was a mistake at dbschema version 1004-1005
                            // the first preview release of v1.7 reused dbschema version 1004
                            // if table "Relation_Game_AgeRatings" exists - then we need to apply the gaseous-fix-1005.sql script before applying the standard 1005 script
                            sql = "SELECT table_name FROM information_schema.tables WHERE table_schema = @dbname AND table_name = @tablename;";
                            dbDict.Add("dbname", Config.DatabaseConfiguration.DatabaseName);
                            dbDict.Add("tablename", "Relation_Game_AgeRatings");
                            data = await db.ExecuteCMDAsync(sql, dbDict);
                            if (data.Rows.Count == 0)
                            {
                                Logging.LogKey(Logging.LogType.Information, "process.database", "database.schema_version_requires_missing_table", null, new[] { TargetSchemaVersion.ToString() });

                                string resourceName = "gaseous_server.Support.Database.MySQL.gaseous-fix-1005.sql";
                                string dbScript = "";

                                string[] resources = Assembly.GetExecutingAssembly().GetManifestResourceNames();
                                if (resources.Contains(resourceName))
                                {
                                    using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                                    using (StreamReader reader = new StreamReader(stream))
                                    {
                                        dbScript = await reader.ReadToEndAsync();

                                        // apply schema!
                                        Logging.LogKey(Logging.LogType.Information, "process.database", "database.applying_schema_version_fix_prior_to", null, new[] { "1005" });
                                        await db.ExecuteCMDAsync(dbScript, dbDict, 180);
                                    }
                                }
                            }
                            break;

                        case 1027:
                            Logging.LogKey(Logging.LogType.Information, "process.database", "database.running_pre_upgrade_for_schema_version", null, new[] { TargetSchemaVersion.ToString() });
                            // create the basic relation tables
                            // this is a blocking task
                            await Storage.CreateRelationsTables<IGDB.Models.Game>();
                            await Storage.CreateRelationsTables<IGDB.Models.Platform>();

                            // drop source id from all metadata tables if it exists
                            var tablesToDropSourceId = new List<string>
                            {
                                "AgeGroup","AgeRating","AgeRatingContentDescription","AlternativeName","Artwork","Collection","Company","CompanyLogo","Cover","ExternalGame","Franchise","Game","GameMode","GameVideo","Genre","InvolvedCompany","MultiplayerMode","Platform","PlatformLogo","PlatformVersion","PlayerPerspective","ReleaseDate","Screenshot","Theme","GameLocalization","Region"
                            };
                            foreach (var table in tablesToDropSourceId)
                            {
                                // check if the column exists
                                sql = $"SELECT * FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = '{Config.DatabaseConfiguration.DatabaseName}' AND TABLE_NAME = '{table}' AND COLUMN_NAME = 'SourceId';";
                                dbDict.Clear();
                                data = await db.ExecuteCMDAsync(sql, dbDict);
                                if (data.Rows.Count > 0)
                                {
                                    // column exists, drop it
                                    sql = $"ALTER TABLE {table} DROP COLUMN SourceId;"; // MySQL does not support IF EXISTS in ALTER TABLE
                                    await db.ExecuteCMDAsync(sql, dbDict);
                                    Logging.LogKey(Logging.LogType.Information, "process.database", "database.dropped_sourceid_column_from_table", null, new[] { table });
                                }

                                switch (table)
                                {
                                    case "ReleaseDate":
                                        // check if month and/or year columns exist
                                        sql = $"SELECT * FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = '{Config.DatabaseConfiguration.DatabaseName}' AND TABLE_NAME = '{table}' AND COLUMN_NAME IN ('Month', 'Year');";
                                        data = await db.ExecuteCMDAsync(sql, dbDict);
                                        foreach (DataRow row in data.Rows)
                                        {
                                            sql = "";
                                            if (row["COLUMN_NAME"].ToString() == "Month")
                                            {
                                                sql += "ALTER TABLE ReleaseDate DROP COLUMN Month, CHANGE `m` `Month` int(11) DEFAULT NULL;";
                                            }
                                            if (row["COLUMN_NAME"].ToString() == "Year")
                                            {
                                                sql += "ALTER TABLE ReleaseDate DROP COLUMN Year, CHANGE `y` `Year` int(11) DEFAULT NULL;";
                                            }
                                            if (!string.IsNullOrEmpty(sql))
                                            {
                                                await db.ExecuteCMDAsync(sql, dbDict);
                                                Logging.LogKey(Logging.LogType.Information, "process.database", "database.dropped_column_from_releasedate_table", null, new[] { row["COLUMN_NAME"].ToString() ?? "" });
                                            }
                                        }
                                        break;
                                }
                            }
                            break;

                        case 1031:
                            Logging.LogKey(Logging.LogType.Information, "process.database", "database.running_pre_upgrade_for_schema_version", null, new[] { TargetSchemaVersion.ToString() });
                            // build tables for metadata storage
                            Metadata.Utility.TableBuilder.BuildTables();
                            sql = "RENAME TABLE AgeGroup TO Metadata_AgeGroup; RENAME TABLE ClearLogo TO Metadata_ClearLogo;";
                            dbDict.Clear();
                            await db.ExecuteCMDAsync(sql, dbDict);
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
                            Logging.LogKey(Logging.LogType.Information, "process.database", "database.adding_country_lookup_table_contents");

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
                            Logging.LogKey(Logging.LogType.Information, "process.database", "database.adding_language_lookup_table_contents");

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
                            // attempt to re-import signature dats

                            // delete existing signature sources to allow re-import
                            Logging.LogKey(Logging.LogType.Information, "process.database", "database.deleting_existing_signature_sources");
                            sql = "DELETE FROM Signatures_Sources;";
                            db.ExecuteNonQuery(sql);

                            _ = MySql_1024_MigrateMetadataVersion();

                            break;

                        case 1027:
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

                                    Logging.LogKey(Logging.LogType.Information, "process.database", "database.updating_rom_path_from_to", null, new[] { existingPath, newPath });

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

                        case 1031:
                            // update Metadata_Platform SourceId to 0
                            sql = "UPDATE Metadata_Platform SET SourceId = 0;";
                            db.ExecuteNonQuery(sql);

                            // update Gmes_Roms to MetadataId = 0
                            sql = "UPDATE Games_Roms SET GameId = 0;";
                            db.ExecuteNonQuery(sql);

                            DatabaseMigration.BackgroundUpgradeTargetSchemaVersions.Add(1031);
                            break;
                    }
                    break;
            }
        }

        public static async Task UpgradeScriptBackgroundTasks()
        {
            foreach (int TargetSchemaVersion in BackgroundUpgradeTargetSchemaVersions)
            {
                try
                {
                    switch (TargetSchemaVersion)
                    {
                        case 1002:
                            MySql_1002_MigrateMetadataVersion();
                            break;

                        case 1031:
                            MySql_1031_MigrateMetadataVersion();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Logging.LogKey(Logging.LogType.Warning, "process.database", "database.error_during_background_upgrade_for_schema_version", null, new[] { TargetSchemaVersion.ToString() }, ex);
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
                Logging.LogKey(Logging.LogType.Information, "process.signature_ingest", "database.update_updating_database_entries_total", null, new[] { data.Rows.Count.ToString() });
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
                        Logging.LogKey(Logging.LogType.Information, "process.signature_ingest", "database.update_updating_database_entries_progress", null, new[] { Counter.ToString(), data.Rows.Count.ToString() });
                    }
                    Counter += 1;
                }
            }
        }

        public static async Task MySql_1024_MigrateMetadataVersion()
        {
            FileSignature fileSignature = new FileSignature();

            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "SELECT * FROM view_Games_Roms WHERE RomDataVersion = 1;";
            DataTable data = await db.ExecuteCMDAsync(sql);
            long count = 1;
            foreach (DataRow row in data.Rows)
            {
                Logging.LogKey(Logging.LogType.Information, "process.database", "database.migration_updating_rom_table_for_rom", null, new[] { count.ToString(), data.Rows.Count.ToString(), (string)row["Name"] });

                GameLibrary.LibraryItem library = await GameLibrary.GetLibrary((int)row["LibraryId"]);
                HashObject hash = new HashObject()
                {
                    md5hash = (string)row["MD5"],
                    sha1hash = (string)row["SHA1"]
                };
                Signatures_Games signature = await fileSignature.GetFileSignatureAsync(
                    library,
                    hash,
                    new FileInfo((string)row["Path"]),
                    (string)row["Path"]
                );

                gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes.Platform platform = await Platforms.GetPlatform((long)row["PlatformId"]);

                await ImportGame.StoreGame(library, hash, signature, platform, (string)row["Path"], (long)row["Id"]);

                count += 1;
            }
        }

        public static void MySql_1031_MigrateMetadataVersion()
        {
            // get the database migration task
            foreach (ProcessQueue.QueueProcessor.QueueItem qi in ProcessQueue.QueueProcessor.QueueItems)
            {
                if (qi.ItemType == ProcessQueue.QueueItemType.BackgroundDatabaseUpgrade)
                {
                    qi.AddSubTask(ProcessQueue.QueueItemSubTasks.MetadataRefresh_Platform, "Platform Metadata", null, false);
                    qi.AddSubTask(ProcessQueue.QueueItemSubTasks.MetadataRefresh_Signatures, "Signature Metadata", null, false);
                    qi.AddSubTask(ProcessQueue.QueueItemSubTasks.MetadataRefresh_Game, "Game Metadata", null, false);
                    qi.AddSubTask(ProcessQueue.QueueItemSubTasks.DatabaseMigration_1031, "Database Migration 1031", null, false);
                }
            }
        }

        public static async Task RunMigration1031()
        {
            // migrate favourites
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "SELECT * FROM Users;";
            DataTable data = db.ExecuteCMD(sql);
            foreach (DataRow row in data.Rows)
            {
                // get the user's favourites
                sql = "SELECT * FROM Favourites WHERE UserId = @userid;";
                Dictionary<string, object> dbDict = new Dictionary<string, object>
                {
                    { "userid", row["Id"] }
                };
                DataTable favouritesData = db.ExecuteCMD(sql, dbDict);

                // copy the users favourites into an array of long
                List<long> favourites = new List<long>();
                foreach (DataRow favouriteRow in favouritesData.Rows)
                {
                    favourites.Add((long)favouriteRow["GameId"]);
                }

                // delete the existing favourites
                sql = "DELETE FROM Favourites WHERE UserId = @userid;";
                dbDict.Clear();
                dbDict.Add("userid", row["Id"]);
                db.ExecuteNonQuery(sql, dbDict);

                // lookup the metadata objects using the GameId, and add the metadataid as a new favourite
                foreach (long gameId in favourites)
                {
                    sql = "SELECT DISTINCT ParentMapId FROM MetadataMapBridge WHERE MetadataSourceType = 1 AND MetadataSourceId = @gameid;";
                    dbDict.Clear();
                    dbDict.Add("gameid", gameId);
                    DataTable metadataData = db.ExecuteCMD(sql, dbDict);
                    if (metadataData.Rows.Count > 0)
                    {
                        Favourites metadataFavourites = new Favourites();
                        metadataFavourites.SetFavourite((string)row["Id"], (long)metadataData.Rows[0]["ParentMapId"], true);
                    }
                }
            }

            // migrate media groups
            sql = "SELECT DISTINCT RomMediaGroup.Id, Games_Roms.MetadataMapId FROM RomMediaGroup_Members JOIN RomMediaGroup ON RomMediaGroup_Members.GroupId = RomMediaGroup.Id JOIN Games_Roms ON RomMediaGroup_Members.RomId = Games_Roms.Id;";
            data = db.ExecuteCMD(sql);
            foreach (DataRow row in data.Rows)
            {
                // set the media group for each media group
                sql = "UPDATE RomMediaGroup SET GameId = @gameid WHERE Id = @id;";
                Dictionary<string, object> dbDict = new Dictionary<string, object>
                {
                    { "gameid", row["MetadataMapId"] },
                    { "id", row["Id"] }
                };
                db.ExecuteNonQuery(sql, dbDict);
            }
        }
    }
}