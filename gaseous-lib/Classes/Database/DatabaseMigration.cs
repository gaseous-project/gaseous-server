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

                                string resourceName = "gaseous_lib.Support.Database.MySQL.gaseous-fix-1005.sql";
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
                            TableBuilder_1031.BuildTables_1031();
                            sql = "RENAME TABLE AgeGroup TO Metadata_AgeGroup; RENAME TABLE ClearLogo TO Metadata_ClearLogo;";
                            dbDict.Clear();
                            await db.ExecuteCMDAsync(sql, dbDict);
                            break;

                        case 1035:
                            Logging.LogKey(Logging.LogType.Information, "process.database", "database.running_pre_upgrade_for_schema_version", null, new[] { TargetSchemaVersion.ToString() });

                            // ensure that the relation tables for games and platforms are built before we attempt to update the database schema
                            await Storage.CreateRelationsTables<IGDB.Models.Game>();

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

                            string countryResourceName = "gaseous_lib.Support.Country.txt";
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

                            string languageResourceName = "gaseous_lib.Support.Language.txt";
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
            Logging.LogKey(Logging.LogType.Information, "process.database", "database.starting_background_upgrade_tasks");
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
                            await MySql_1031_MigrateMetadataVersion();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Logging.LogKey(Logging.LogType.Warning, "process.database", "database.error_during_background_upgrade_for_schema_version", null, new[] { TargetSchemaVersion.ToString() }, ex);
                }
            }

            // perform any metadata table migrations that are needed
            await gaseous_server.Classes.Metadata.Utility.MetadataTableBuilder.BuildTableFromType("gaseous", "Metadata", typeof(gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes.Game), "", "NameThe, AgeGroup");
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

            // Check if the view exists before proceeding
            string sql = "SELECT table_name FROM information_schema.views WHERE table_schema = @dbname AND table_name = 'view_Games_Roms';";
            Dictionary<string, object> dbDict = new Dictionary<string, object>
            {
                { "dbname", Config.DatabaseConfiguration.DatabaseName }
            };
            DataTable viewCheck = await db.ExecuteCMDAsync(sql, dbDict);
            if (viewCheck.Rows.Count == 0)
            {
                // View doesn't exist, skip migration
                Logging.LogKey(Logging.LogType.Information, "process.database", "database.view_does_not_exist_skipping_migration", null, new[] { "view_Games_Roms" });
                return;
            }

            sql = "SELECT * FROM view_Games_Roms WHERE RomDataVersion = 1;";
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

                FileSignature.FileHash fileHash = new FileSignature.FileHash()
                {
                    Library = library,
                    Hash = hash,
                    FileName = (string)row["RelativePath"]
                };

                var (_, signature) = await fileSignature.GetFileSignatureAsync(
                    library,
                    fileHash
                );

                gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes.Platform platform = await Platforms.GetPlatform((long)row["PlatformId"]);

                await ImportGame.StoreGame(library, hash, signature, platform, (string)row["Path"], (long)row["Id"]);

                count += 1;
            }
        }

        public static async Task MySql_1031_MigrateMetadataVersion()
        {
            // get the database migration task
            foreach (ProcessQueue.QueueProcessor.QueueItem qi in ProcessQueue.QueueProcessor.QueueItems)
            {
                if (qi.ItemType == ProcessQueue.QueueItemType.BackgroundDatabaseUpgrade)
                {
                    await qi.AddSubTask(ProcessQueue.QueueItemSubTasks.MetadataRefresh_Platform, "Platform Metadata", null, false);
                    await qi.AddSubTask(ProcessQueue.QueueItemSubTasks.MetadataRefresh_Signatures, "Signature Metadata", null, false);
                    await qi.AddSubTask(ProcessQueue.QueueItemSubTasks.MetadataRefresh_Game, "Game Metadata", null, false);
                    await qi.AddSubTask(ProcessQueue.QueueItemSubTasks.DatabaseMigration_1031, "Database Migration 1031", null, false);
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

        public static class TableBuilder_1031
        {
            public static void BuildTables_1031()
            {
                BuildTableFromType(typeof(IGDB.Models.AgeRating));
                BuildTableFromType(typeof(IGDB.Models.AgeRatingCategory));
                BuildTableFromType(typeof(IGDB.Models.AgeRatingContentDescriptionV2));
                BuildTableFromType(typeof(IGDB.Models.AgeRatingOrganization));
                BuildTableFromType(typeof(IGDB.Models.AlternativeName));
                BuildTableFromType(typeof(IGDB.Models.Artwork));
                BuildTableFromType(typeof(IGDB.Models.Character));
                BuildTableFromType(typeof(IGDB.Models.CharacterGender));
                BuildTableFromType(typeof(IGDB.Models.CharacterMugShot));
                BuildTableFromType(typeof(IGDB.Models.CharacterSpecies));
                BuildTableFromType(typeof(IGDB.Models.Collection));
                BuildTableFromType(typeof(IGDB.Models.CollectionMembership));
                BuildTableFromType(typeof(IGDB.Models.CollectionMembershipType));
                BuildTableFromType(typeof(IGDB.Models.CollectionRelation));
                BuildTableFromType(typeof(IGDB.Models.CollectionRelationType));
                BuildTableFromType(typeof(IGDB.Models.CollectionType));
                BuildTableFromType(typeof(IGDB.Models.Company));
                BuildTableFromType(typeof(IGDB.Models.CompanyLogo));
                BuildTableFromType(typeof(IGDB.Models.CompanyStatus));
                BuildTableFromType(typeof(IGDB.Models.CompanyWebsite));
                BuildTableFromType(typeof(IGDB.Models.Cover));
                BuildTableFromType(typeof(IGDB.Models.Event));
                BuildTableFromType(typeof(IGDB.Models.EventLogo));
                BuildTableFromType(typeof(IGDB.Models.EventNetwork));
                BuildTableFromType(typeof(IGDB.Models.ExternalGame));
                BuildTableFromType(typeof(IGDB.Models.ExternalGameSource));
                BuildTableFromType(typeof(IGDB.Models.Franchise));
                BuildTableFromType(typeof(IGDB.Models.Game));
                BuildTableFromType(typeof(IGDB.Models.GameEngine));
                BuildTableFromType(typeof(IGDB.Models.GameEngineLogo));
                BuildTableFromType(typeof(IGDB.Models.GameLocalization));
                BuildTableFromType(typeof(IGDB.Models.GameMode));
                BuildTableFromType(typeof(IGDB.Models.GameReleaseFormat));
                BuildTableFromType(typeof(IGDB.Models.GameStatus));
                BuildTableFromType(typeof(IGDB.Models.GameTimeToBeat));
                BuildTableFromType(typeof(IGDB.Models.GameType));
                BuildTableFromType(typeof(IGDB.Models.GameVersion));
                BuildTableFromType(typeof(IGDB.Models.GameVersionFeature));
                BuildTableFromType(typeof(IGDB.Models.GameVersionFeatureValue));
                BuildTableFromType(typeof(IGDB.Models.GameVideo));
                BuildTableFromType(typeof(IGDB.Models.Genre));
                BuildTableFromType(typeof(IGDB.Models.InvolvedCompany));
                BuildTableFromType(typeof(IGDB.Models.Keyword));
                BuildTableFromType(typeof(IGDB.Models.Language));
                BuildTableFromType(typeof(IGDB.Models.LanguageSupport));
                BuildTableFromType(typeof(IGDB.Models.LanguageSupportType));
                BuildTableFromType(typeof(IGDB.Models.MultiplayerMode));
                BuildTableFromType(typeof(IGDB.Models.NetworkType));
                BuildTableFromType(typeof(IGDB.Models.Platform));
                BuildTableFromType(typeof(IGDB.Models.PlatformFamily));
                BuildTableFromType(typeof(IGDB.Models.PlatformLogo));
                BuildTableFromType(typeof(IGDB.Models.PlatformVersion));
                BuildTableFromType(typeof(IGDB.Models.PlatformVersionCompany));
                BuildTableFromType(typeof(IGDB.Models.PlatformVersionReleaseDate));
                BuildTableFromType(typeof(IGDB.Models.PlatformWebsite));
                BuildTableFromType(typeof(IGDB.Models.PlayerPerspective));
                BuildTableFromType(typeof(IGDB.Models.PopularityPrimitive));
                BuildTableFromType(typeof(IGDB.Models.PopularityType));
                BuildTableFromType(typeof(IGDB.Models.Region));
                BuildTableFromType(typeof(IGDB.Models.ReleaseDate));
                BuildTableFromType(typeof(IGDB.Models.ReleaseDateRegion));
                BuildTableFromType(typeof(IGDB.Models.ReleaseDateStatus));
                BuildTableFromType(typeof(IGDB.Models.Screenshot));
                BuildTableFromType(typeof(IGDB.Models.Theme));
                BuildTableFromType(typeof(IGDB.Models.Website));
                BuildTableFromType(typeof(IGDB.Models.WebsiteType));
            }

            /// <summary>
            /// Builds a table from a type definition, or modifies an existing table.
            /// This is used to create or update tables in the database based on the properties of a class.
            /// Updates are limited to adding new columns, as the table structure should not change once created.
            /// If the table already exists, it will only add new columns that are not already present.
            /// This is useful for maintaining a consistent schema across different versions of the application.
            /// The method is generic and can be used with any type that has properties that can be mapped to database columns.
            /// The method does not return any value, but it will throw an exception if there is an error during the table creation or modification process.
            /// </summary>
            /// <param name="type">The type definition of the class for which the table should be built.</param>
            public static void BuildTableFromType(Type type)
            {
                // Get the table name from the class name
                string tableName = type.Name;

                // Start building the SQL command
                Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);

                // check rename migration status
                if (Config.ReadSetting<bool>($"RenameMigration_{tableName}", false) == false)
                {
                    // rename the table if it exists
                    // Check if the table exists
                    string checkTableExistsQuery = $"SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = '{tableName}'";
                    var result = db.ExecuteCMD(checkTableExistsQuery);
                    if (Convert.ToInt32(result.Rows[0][0]) > 0)
                    {
                        // The table exists, so we will rename it
                        Console.WriteLine($"Table '{tableName}' already exists. Renaming to 'Metadata_{tableName}'...");

                        string renameTableQuery = $"ALTER TABLE `{tableName}` RENAME TO `Metadata_{tableName}`";
                        db.ExecuteNonQuery(renameTableQuery);
                    }

                    // mark the rename migration as done
                    Config.SetSetting($"RenameMigration_{tableName}", true);
                }
                // Update the table name to include the Metadata prefix
                tableName = $"Metadata_{tableName}";

                // Get the properties of the class
                PropertyInfo[] properties = type.GetProperties();

                // Create the table with the basic structure if it does not exist
                string createTableQuery = $"CREATE TABLE IF NOT EXISTS `{tableName}` (`Id` BIGINT PRIMARY KEY, `dateAdded` DATETIME DEFAULT CURRENT_TIMESTAMP, `lastUpdated` DATETIME DEFAULT CURRENT_TIMESTAMP )";
                db.ExecuteNonQuery(createTableQuery);

                // Add the sourceId column if it does not exist
                string addSourceIdQuery = $"ALTER TABLE `{tableName}` ADD COLUMN IF NOT EXISTS `SourceId` INT";
                db.ExecuteNonQuery(addSourceIdQuery);

                // Loop through each property to add it as a column in the table
                foreach (PropertyInfo property in properties)
                {
                    // Get the property name and type
                    string columnName = property.Name;
                    string columnType = "VARCHAR(255)"; // Default type, can be changed based on property type

                    // Convert the property type name to a string
                    string propertyTypeName = property.PropertyType.Name;
                    if (propertyTypeName == "Nullable`1")
                    {
                        // If the property is nullable, get the underlying type
                        propertyTypeName = property.PropertyType.GetGenericArguments()[0].Name;
                    }

                    // Determine the SQL type based on the property type
                    switch (propertyTypeName)
                    {
                        case "String":
                            columnType = "VARCHAR(255)";
                            break;
                        case "Int32":
                            columnType = "INT";
                            break;
                        case "Int64":
                            columnType = "BIGINT";
                            break;
                        case "Boolean":
                            columnType = "BOOLEAN";
                            break;
                        case "DateTime":
                        case "DateTimeOffset":
                            columnType = "DATETIME";
                            break;
                        case "Double":
                            columnType = "DOUBLE";
                            break;
                        case "IdentityOrValue`1":
                            columnType = "BIGINT";
                            break;
                        case "IdentitiesOrValues`1":
                            columnType = "LONGTEXT";
                            break;
                    }

                    // check if there is a column with the name of the property
                    string checkColumnQuery = $"SHOW COLUMNS FROM `{tableName}` LIKE '{columnName}'";
                    var result = db.ExecuteCMD(checkColumnQuery);
                    if (result.Rows.Count > 0)
                    {
                        // Column already exists, check if the type matches
                        string existingType = result.Rows[0]["Type"].ToString();
                        if (existingType.ToLower().Split("(")[0] != columnType.ToLower().Split("(")[0] && existingType != "text" && existingType != "longtext")
                        {
                            // If the type does not match, we cannot change the column type in MySQL without dropping it first
                            Console.WriteLine($"Column '{columnName}' in table '{tableName}' already exists with type '{existingType}', but expected type is '{columnType}'.");
                            string alterColumnQuery = $"ALTER TABLE `{tableName}` MODIFY COLUMN `{columnName}` {columnType}";
                            Console.WriteLine($"Executing query: {alterColumnQuery}");
                            try
                            {
                                db.ExecuteNonQuery(alterColumnQuery);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error altering column '{columnName}' in table '{tableName}': {ex.Message}");
                            }
                            continue; // Skip this column as we cannot change its type
                        }
                        continue; // Skip this column as it already exists
                    }

                    // Add the column to the table if it does not already exist
                    string addColumnQuery = $"ALTER TABLE `{tableName}` ADD COLUMN IF NOT EXISTS `{columnName}` {columnType}";
                    Console.WriteLine($"Executing query: {addColumnQuery}");
                    db.ExecuteNonQuery(addColumnQuery);
                }
            }
        }
    }
}