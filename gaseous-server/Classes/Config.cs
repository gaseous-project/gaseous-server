using System;
using System.Data;
using Newtonsoft.Json;
using IGDB.Models;
using gaseous_server.Classes.Metadata;
using NuGet.Common;

namespace gaseous_server.Classes
{
    public static class Config
    {
        static ConfigFile _config;

        public static string ConfigurationPath
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gaseous-server");
            }
        }

        static string ConfigurationFilePath
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gaseous-server", "config.json");
            }
        }

        static string ConfigurationFilePath_Backup
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gaseous-server", "config.json.backup");
            }
        }

        public static string PlatformMappingFile
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gaseous-server", "platformmap.json");
            }
        }

        public static ConfigFile.Database DatabaseConfiguration
        {
            get
            {
                return _config.DatabaseConfiguration;
            }
        }

        public static ConfigFile.Library LibraryConfiguration
        {
            get
            {
                return _config.LibraryConfiguration;
            }
        }

        public static ConfigFile.MetadataAPI MetadataConfiguration
        {
            get
            {
                return _config.MetadataConfiguration;
            }
        }

        public static ConfigFile.IGDB IGDB
        {
            get
            {
                return _config.IGDBConfiguration;
            }
        }

        public static string LogPath
        {
            get
            {
                string logPath = Path.Combine(ConfigurationPath, "Logs");
                if (!Directory.Exists(logPath)) {
                    Directory.CreateDirectory(logPath);
                }
                return logPath;
            }
        }

        public static string LogFilePath
        {
            get
            {
                string logFileExtension = "txt";
                
                string logPathName = Path.Combine(LogPath, "Server Log " + DateTime.Now.ToUniversalTime().ToString("yyyyMMdd") + "." + logFileExtension);
                return logPathName;
            }
        }

        public static ConfigFile.Logging LoggingConfiguration
        {
            get
            {
                return _config.LoggingConfiguration;
            }
        }

        static Config()
        {
            if (_config == null)
            {
                // load the config file
                if (File.Exists(ConfigurationFilePath))
                {
                    string configRaw = File.ReadAllText(ConfigurationFilePath);
                    ConfigFile? _tempConfig = Newtonsoft.Json.JsonConvert.DeserializeObject<ConfigFile>(configRaw);
                    if (_tempConfig != null)
                    {
                        _config = _tempConfig;
                    } else
                    {
                        throw new Exception("There was an error reading the config file: Json returned null");
                    }
                } else
                {
                    // no config file!
                    // use defaults and save
                    _config = new ConfigFile();
                    UpdateConfig();
                }
            }

            Console.WriteLine("Using configuration:");
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(_config, Formatting.Indented));
        }

        public static void UpdateConfig()
        {
            // save any updates to the configuration
            Newtonsoft.Json.JsonSerializerSettings serializerSettings = new Newtonsoft.Json.JsonSerializerSettings
            {
                NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                Formatting = Newtonsoft.Json.Formatting.Indented
            };
            serializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
            string configRaw = Newtonsoft.Json.JsonConvert.SerializeObject(_config, serializerSettings);

            if (!Directory.Exists(ConfigurationPath))
            {
                Directory.CreateDirectory(ConfigurationPath);
            }

            if (File.Exists(ConfigurationFilePath_Backup))
            {
                File.Delete(ConfigurationFilePath_Backup);
            }
            if (File.Exists(ConfigurationFilePath))
            {
                File.Move(ConfigurationFilePath, ConfigurationFilePath_Backup);
            }
            File.WriteAllText(ConfigurationFilePath, configRaw);
        }

        private static Dictionary<string, object> AppSettings = new Dictionary<string, object>();

        public static void InitSettings()
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "SELECT * FROM Settings";

            DataTable dbResponse = db.ExecuteCMD(sql);
            foreach (DataRow dataRow in dbResponse.Rows)
            {
                if (AppSettings.ContainsKey((string)dataRow["Setting"]))
                {
                    if ((int)dataRow["ValueType"] == 0)
                    {
                        AppSettings[(string)dataRow["Setting"]] = (string)dataRow["Value"];
                    }
                    else
                    {
                        AppSettings[(string)dataRow["Setting"]] = (DateTime)dataRow["ValueDate"];
                    }
                }
                else
                {
                    if ((int)dataRow["ValueType"] == 0)
                    {
                        AppSettings.Add((string)dataRow["Setting"], (string)dataRow["Value"]);
                    }
                    else
                    {
                        AppSettings.Add((string)dataRow["Setting"], (DateTime)dataRow["ValueDate"]);
                    }
                }
            }
        }

        public static T ReadSetting<T>(string SettingName, T DefaultValue)
        {
            if (AppSettings.ContainsKey(SettingName))
            {
                return (T)AppSettings[SettingName];
            }
            else
            {
                Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
                string sql = "SELECT Value, ValueDate FROM Settings WHERE Setting = @SettingName";
                Dictionary<string, object> dbDict = new Dictionary<string, object>
                {
                    { "SettingName", SettingName }
                };

                try
                {
                    Logging.Log(Logging.LogType.Debug, "Database", "Reading setting '" + SettingName + "'");
                    DataTable dbResponse = db.ExecuteCMD(sql, dbDict);
                    Type type = typeof(T);
                    if (dbResponse.Rows.Count == 0)
                    {
                        // no value with that name stored - respond with the default value
                        SetSetting<T>(SettingName, DefaultValue);
                        return DefaultValue;
                    }
                    else
                    {
                        if (type.ToString() == "System.DateTime")
                        {
                            AppSettings.Add(SettingName, dbResponse.Rows[0]["ValueDate"]);
                            return (T)dbResponse.Rows[0]["ValueDate"];
                        }
                        else
                        {
                            AppSettings.Add(SettingName, dbResponse.Rows[0]["Value"]);
                            return (T)dbResponse.Rows[0]["Value"];
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logging.Log(Logging.LogType.Critical, "Database", "Failed reading setting " + SettingName, ex);
                    throw;
                }
            }
        }

        public static void SetSetting<T>(string SettingName, T Value)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "REPLACE INTO Settings (Setting, ValueType, Value, ValueDate) VALUES (@SettingName, @ValueType, @Value, @ValueDate)";
            Dictionary<string, object> dbDict;
            Type type = typeof(T);
            if (type.ToString() == "System.DateTime")
            {
                dbDict = new Dictionary<string, object>
                {
                    { "SettingName", SettingName },
                    { "ValueType", 1 },
                    { "Value", null },
                    { "ValueDate", Value }
                };
            }
            else
            {
                dbDict = new Dictionary<string, object>
                {
                    { "SettingName", SettingName },
                    { "ValueType", 0 },
                    { "Value", Value },
                    { "ValueDate", null }
                };
            }

            Logging.Log(Logging.LogType.Debug, "Database", "Storing setting '" + SettingName + "' to value: '" + Value + "'");
            try
            {
                db.ExecuteCMD(sql, dbDict);

                if (AppSettings.ContainsKey(SettingName))
                {
                    AppSettings[SettingName] = Value;
                }
                else
                {
                    AppSettings.Add(SettingName, Value);
                }
            }
            catch (Exception ex)
            {
                Logging.Log(Logging.LogType.Critical, "Database", "Failed storing setting" + SettingName, ex);
                throw;
            }
        }

        public class ConfigFile
        {
            public Database DatabaseConfiguration = new Database();

            [JsonIgnore]
            public Library LibraryConfiguration = new Library();

            public MetadataAPI MetadataConfiguration = new MetadataAPI();

            public IGDB IGDBConfiguration = new IGDB();

            public Logging LoggingConfiguration = new Logging();

            public class Database
            {
                private static string _DefaultHostName {
                    get
                    {
                        if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("dbhost")))
                        {
                            return Environment.GetEnvironmentVariable("dbhost");
                        }
                        else
                        {
                            return "localhost";
                        }
                    }
                }

                private static string _DefaultUserName
                {
                    get
                    {
                        if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("dbuser")))
                        {
                            return Environment.GetEnvironmentVariable("dbuser");
                        }
                        else
                        {
                            return "gaseous";
                        }
                    }
                }

                private static string _DefaultPassword
                {
                    get
                    {
                        if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("dbpass")))
                        {
                            return Environment.GetEnvironmentVariable("dbpass");
                        }
                        else
                        {
                            return "gaseous";
                        }
                    }
                }

                public string HostName = _DefaultHostName;
                public string UserName = _DefaultUserName;
                public string Password = _DefaultPassword;
                public string DatabaseName = "gaseous";
                public int Port = 3306;

                [JsonIgnore]
                public string ConnectionString
                {
                    get
                    {
                        string dbConnString = "server=" + HostName + ";port=" + Port + ";userid=" + UserName + ";password=" + Password + ";database=" + DatabaseName + "";
                        return dbConnString;
                    }
                }

                [JsonIgnore]
                public string ConnectionStringNoDatabase
                {
                    get
                    {
                        string dbConnString = "server=" + HostName + ";port=" + Port + ";userid=" + UserName + ";password=" + Password + ";";
                        return dbConnString;
                    }
                }
            }

            public class Library
            {
                public string LibraryRootDirectory
                {
                    get
                    {
                        return ReadSetting<string>("LibraryRootDirectory", Path.Combine(Config.ConfigurationPath, "Data"));
                    }
                    set
                    {
                        SetSetting<string>("LibraryRootDirectory", value);
                    }
                }

                public string LibraryImportDirectory
                {
                    get
                    {
                        return Path.Combine(LibraryRootDirectory, "Import");
                    }
                }

                public string LibraryImportErrorDirectory
                {
                    get
                    {
                        return Path.Combine(LibraryRootDirectory, "Import Errors");
                    }
                }

                public string LibraryImportDuplicatesDirectory
                {
                    get
                    {
                        return Path.Combine(LibraryImportErrorDirectory, "Duplicates");
                    }
                }

                public string LibraryImportGeneralErrorDirectory
                {
                    get
                    {
                        return Path.Combine(LibraryImportErrorDirectory, "Error");
                    }
                }

                public string LibraryBIOSDirectory
                {
                    get
                    {
                        return Path.Combine(LibraryRootDirectory, "BIOS");
                    }
                }

                public string LibraryUploadDirectory
                {
                    get
                    {
                        return Path.Combine(LibraryRootDirectory, "Upload");
                    }
                }

                public string LibraryMetadataDirectory
                {
                    get
                    {
                        return Path.Combine(LibraryRootDirectory, "Metadata");
                    }
                }

                public string LibraryTempDirectory
                {
                    get
                    {
                        return Path.Combine(LibraryRootDirectory, "Temp");
                    }
                }

                public string LibraryCollectionsDirectory
                {
                    get
                    {
                        return Path.Combine(LibraryRootDirectory, "Collections");
                    }
                }

                public string LibraryMediaGroupDirectory
                {
                    get
                    {
                        return Path.Combine(LibraryRootDirectory, "Media Groups");
                    }
                }

                public string LibraryMetadataDirectory_Platform(Platform platform)
                {
                    string MetadataPath = Path.Combine(LibraryMetadataDirectory, "Platforms", platform.Slug);
                    if (!Directory.Exists(MetadataPath)) { Directory.CreateDirectory(MetadataPath); }
                    return MetadataPath;
                }

                public string LibraryMetadataDirectory_Game(Game game)
                {
                    string MetadataPath = Path.Combine(LibraryMetadataDirectory, "Games", game.Slug);
                    if (!Directory.Exists(MetadataPath)) { Directory.CreateDirectory(MetadataPath); }
                    return MetadataPath;
                }

                public string LibraryMetadataDirectory_Company(Company company)
                {
                    string MetadataPath = Path.Combine(LibraryMetadataDirectory, "Companies", company.Slug);
                    if (!Directory.Exists(MetadataPath)) { Directory.CreateDirectory(MetadataPath); }
                    return MetadataPath;
                }

                public string LibrarySignatureImportDirectory
                {
                    get
                    {
                        return Path.Combine(LibraryRootDirectory, "Signatures");
                    }
                }

                public void InitLibrary()
                {
                    if (!Directory.Exists(LibraryRootDirectory)) { Directory.CreateDirectory(LibraryRootDirectory); }
                    if (!Directory.Exists(LibraryImportDirectory)) { Directory.CreateDirectory(LibraryImportDirectory); }
                    if (!Directory.Exists(LibraryBIOSDirectory)) { Directory.CreateDirectory(LibraryBIOSDirectory); }
                    if (!Directory.Exists(LibraryUploadDirectory)) { Directory.CreateDirectory(LibraryUploadDirectory); }
                    if (!Directory.Exists(LibraryMetadataDirectory)) { Directory.CreateDirectory(LibraryMetadataDirectory); }
                    if (!Directory.Exists(LibraryTempDirectory)) { Directory.CreateDirectory(LibraryTempDirectory); }
                    if (!Directory.Exists(LibraryCollectionsDirectory)) { Directory.CreateDirectory(LibraryCollectionsDirectory); }
                    if (!Directory.Exists(LibrarySignatureImportDirectory)) { Directory.CreateDirectory(LibrarySignatureImportDirectory); }
                }
            }

            public class MetadataAPI
            {
                private static HasheousClient.Models.MetadataModel.MetadataSources _MetadataSource
                {
                    get
                    {
                        if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("metadatasource")))
                        {
                            return (HasheousClient.Models.MetadataModel.MetadataSources)Enum.Parse(typeof(HasheousClient.Models.MetadataModel.MetadataSources), Environment.GetEnvironmentVariable("metadatasource"));
                        }
                        else
                        {
                            return HasheousClient.Models.MetadataModel.MetadataSources.IGDB;
                        }
                    }
                }

                private static HasheousClient.Models.MetadataModel.SignatureSources _SignatureSource
                {
                    get
                    {
                        if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("signaturesource")))
                        {
                            return (HasheousClient.Models.MetadataModel.SignatureSources)Enum.Parse(typeof(HasheousClient.Models.MetadataModel.SignatureSources), Environment.GetEnvironmentVariable("signaturesource"));
                        }
                        else
                        {
                            return HasheousClient.Models.MetadataModel.SignatureSources.LocalOnly;
                        }
                    }
                }

                private static int _MaxLibraryScanWorkers
                {
                    get
                    {
                        return 4;
                    }
                }

                private static string _HasheousHost
                {
                    get
                    {
                        if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("hasheoushoust")))
                        {
                            return Environment.GetEnvironmentVariable("hasheoushoust");
                        }
                        else
                        {
                            return "https://hasheous.org/";
                        }
                    }
                }

                public HasheousClient.Models.MetadataModel.MetadataSources MetadataSource = _MetadataSource;

                public HasheousClient.Models.MetadataModel.SignatureSources SignatureSource = _SignatureSource;

                public int MaxLibraryScanWorkers = _MaxLibraryScanWorkers;

                public string HasheousHost = _HasheousHost;
            }

            public class IGDB
            {
                private static string _DefaultIGDBClientId
                {
                    get
                    {
                        if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("igdbclientid")))
                        {
                            return Environment.GetEnvironmentVariable("igdbclientid");
                        }
                        else
                        {
                            return "";
                        }
                    }
                }

                private static string _DefaultIGDBSecret
                {
                    get
                    {
                        if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("igdbclientsecret")))
                        {
                            return Environment.GetEnvironmentVariable("igdbclientsecret");
                        }
                        else
                        {
                            return "";
                        }
                    }
                }

                public string ClientId = _DefaultIGDBClientId;
                public string Secret = _DefaultIGDBSecret;
            }

            public class Logging
            {
                public bool DebugLogging = false;

                // log retention in days
                public int LogRetention = 7;

                public bool AlwaysLogToDisk = false;
            }
        }
    }
}
