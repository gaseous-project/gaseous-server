using System;
using System.Data;

namespace gaseous_server.Classes
{
	public static class DatabaseMigration
	{
        public static List<int> BackgroundUpgradeTargetSchemaVersions = new List<int>();

        public static void PreUpgradeScript(int TargetSchemaVersion, Database.databaseType? DatabaseType) 
        {

        }

        public static void PostUpgradeScript(int TargetSchemaVersion, Database.databaseType? DatabaseType) 
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            Dictionary<string, object> dbDict = new Dictionary<string, object>();

            switch(DatabaseType)
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
                            string sql = "INSERT INTO GameLibraries (Name, Path, DefaultLibrary, DefaultPlatform) VALUES (@name, @path, @defaultlibrary, @defaultplatform); SELECT CAST(LAST_INSERT_ID() AS SIGNED);";
                            dbDict.Add("name", "Default");
                            dbDict.Add("path", oldRoot);
                            dbDict.Add("defaultlibrary", 1);
                            dbDict.Add("defaultplatform", 0);
                            DataTable data = db.ExecuteCMD(sql, dbDict);

                            // apply the new library id to the existing roms
                            sql = "UPDATE Games_Roms SET LibraryId=@libraryid;";
                            dbDict.Clear();
                            dbDict.Add("libraryid", data.Rows[0][0]);
                            db.ExecuteCMD(sql, dbDict);
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
                }
            }
        }

        public static void MySql_1002_MigrateMetadataVersion() {
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
    }
}