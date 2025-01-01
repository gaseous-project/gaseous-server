using System;
using System.Data;
using Newtonsoft.Json;
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
                if (!Directory.Exists(logPath))
                {
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
                    Newtonsoft.Json.JsonSerializerSettings serializerSettings = new Newtonsoft.Json.JsonSerializerSettings
                    {
                        NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                        MissingMemberHandling = Newtonsoft.Json.MissingMemberHandling.Ignore
                    };
                    ConfigFile? _tempConfig = Newtonsoft.Json.JsonConvert.DeserializeObject<ConfigFile>(configRaw, serializerSettings);
                    if (_tempConfig != null)
                    {
                        _config = _tempConfig;

                        // load environment variables if we're in a docker container
                        if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("INDOCKER")))
                        {
                            if (Environment.GetEnvironmentVariable("INDOCKER") == "1")
                            {
                                Console.WriteLine("Running in Docker - setting configuration from variables");
                                _config.DatabaseConfiguration.HostName = (string)Common.GetEnvVar("dbhost", _config.DatabaseConfiguration.HostName);
                                _config.DatabaseConfiguration.UserName = (string)Common.GetEnvVar("dbuser", _config.DatabaseConfiguration.UserName);
                                _config.DatabaseConfiguration.Password = (string)Common.GetEnvVar("dbpass", _config.DatabaseConfiguration.Password);
                                _config.DatabaseConfiguration.DatabaseName = (string)Common.GetEnvVar("dbname", _config.DatabaseConfiguration.DatabaseName);
                                _config.DatabaseConfiguration.Port = int.Parse((string)Common.GetEnvVar("dbport", _config.DatabaseConfiguration.Port.ToString()));
                                _config.MetadataConfiguration.DefaultMetadataSource = (HasheousClient.Models.MetadataSources)Enum.Parse(typeof(HasheousClient.Models.MetadataSources), (string)Common.GetEnvVar("metadatasource", _config.MetadataConfiguration.DefaultMetadataSource.ToString()));
                                _config.IGDBConfiguration.UseHasheousProxy = bool.Parse((string)Common.GetEnvVar("metadatausehasheousproxy", _config.IGDBConfiguration.UseHasheousProxy.ToString()));
                                _config.MetadataConfiguration.SignatureSource = (HasheousClient.Models.MetadataModel.SignatureSources)Enum.Parse(typeof(HasheousClient.Models.MetadataModel.SignatureSources), (string)Common.GetEnvVar("signaturesource", _config.MetadataConfiguration.SignatureSource.ToString())); ;
                                _config.MetadataConfiguration.HasheousHost = (string)Common.GetEnvVar("hasheoushost", _config.MetadataConfiguration.HasheousHost);
                                _config.IGDBConfiguration.ClientId = (string)Common.GetEnvVar("igdbclientid", _config.IGDBConfiguration.ClientId);
                                _config.IGDBConfiguration.Secret = (string)Common.GetEnvVar("igdbclientsecret", _config.IGDBConfiguration.Secret);
                            }
                        }
                    }
                    else
                    {
                        throw new Exception("There was an error reading the config file: Json returned null");
                    }
                }
                else
                {
                    // no config file!
                    // use defaults and save
                    _config = new ConfigFile();
                    UpdateConfig();
                }
            }

            // Console.WriteLine("Using configuration:");
            // Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(_config, Formatting.Indented));
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
                string SettingName = (string)dataRow["Setting"];

                if (AppSettings.ContainsKey(SettingName))
                {
                    AppSettings.Remove(SettingName);
                }

                // Logging.Log(Logging.LogType.Information, "Load Settings", "Loading setting " + SettingName + " from database");

                try
                {
                    if (Database.schema_version >= 1016)
                    {
                        switch ((int)dataRow["ValueType"])
                        {
                            case 0:
                            default:
                                // value is a string
                                AppSettings.Add(SettingName, dataRow["Value"]);
                                break;

                            case 1:
                                // value is a datetime
                                AppSettings.Add(SettingName, dataRow["ValueDate"]);
                                break;
                        }
                    }
                    else
                    {
                        AppSettings.Add(SettingName, dataRow["Value"]);
                    }
                }
                catch (InvalidCastException castEx)
                {
                    Logging.Log(Logging.LogType.Warning, "Settings", "Exception when reading server setting " + SettingName + ". Resetting to default.", castEx);

                    // delete broken setting and return the default
                    // this error is probably generated during an upgrade
                    sql = "DELETE FROM Settings WHERE Setting = @SettingName";
                    Dictionary<string, object> dbDict = new Dictionary<string, object>
                    {
                        { "SettingName", SettingName }
                    };
                }
                catch (Exception ex)
                {
                    Logging.Log(Logging.LogType.Critical, "Settings", "Exception when reading server setting " + SettingName + ".", ex);
                }
            }
        }

        public static T ReadSetting<T>(string SettingName, T DefaultValue)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            try
            {
                if (AppSettings.ContainsKey(SettingName))
                {
                    return (T)AppSettings[SettingName];
                }
                else
                {
                    string sql;
                    Dictionary<string, object> dbDict = new Dictionary<string, object>
                    {
                        { "SettingName", SettingName }
                    };
                    DataTable dbResponse;

                    try
                    {
                        Logging.Log(Logging.LogType.Debug, "Database", "Reading setting '" + SettingName + "'");

                        if (Database.schema_version >= 1016)
                        {
                            sql = "SELECT Value, ValueDate FROM Settings WHERE Setting = @SettingName";

                            dbResponse = db.ExecuteCMD(sql, dbDict);
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
                                    AppSettings.Add(SettingName, (T)dbResponse.Rows[0]["ValueDate"]);
                                    return (T)dbResponse.Rows[0]["ValueDate"];
                                }
                                else
                                {
                                    AppSettings.Add(SettingName, (T)dbResponse.Rows[0]["Value"]);
                                    return (T)dbResponse.Rows[0]["Value"];
                                }
                            }
                        }
                        else
                        {
                            sql = "SELECT Value FROM Settings WHERE Setting = @SettingName";

                            dbResponse = db.ExecuteCMD(sql, dbDict);
                            Type type = typeof(T);
                            if (dbResponse.Rows.Count == 0)
                            {
                                // no value with that name stored - respond with the default value
                                SetSetting<T>(SettingName, DefaultValue);
                                return DefaultValue;
                            }
                            else
                            {
                                AppSettings.Add(SettingName, (T)dbResponse.Rows[0]["Value"]);
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
            catch (InvalidCastException castEx)
            {
                Logging.Log(Logging.LogType.Warning, "Settings", "Exception when reading server setting " + SettingName + ". Resetting to default.", castEx);

                // delete broken setting and return the default
                // this error is probably generated during an upgrade
                if (AppSettings.ContainsKey(SettingName))
                {
                    AppSettings.Remove(SettingName);
                }

                string sql = "DELETE FROM Settings WHERE Setting = @SettingName";
                Dictionary<string, object> dbDict = new Dictionary<string, object>
                {
                    { "SettingName", SettingName }
                };

                return DefaultValue;
            }
            catch (Exception ex)
            {
                Logging.Log(Logging.LogType.Critical, "Settings", "Exception when reading server setting " + SettingName + ".", ex);
                throw;
            }
        }

        public static void SetSetting<T>(string SettingName, T Value)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql;
            Dictionary<string, object> dbDict;

            if (Database.schema_version >= 1016)
            {
                sql = "REPLACE INTO Settings (Setting, ValueType, Value, ValueDate) VALUES (@SettingName, @ValueType, @Value, @ValueDate)";
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
            }
            else
            {
                sql = "REPLACE INTO Settings (Setting, Value) VALUES (@SettingName, @Value)";
                dbDict = new Dictionary<string, object>
                {
                    { "SettingName", SettingName },
                    { "Value", Value }
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
                private static string _DefaultHostName
                {
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

                private static string _DefaultDatabaseName
                {
                    get
                    {
                        if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("dbname")))
                        {
                            return Environment.GetEnvironmentVariable("dbname");
                        }
                        else
                        {
                            return "gaseous";
                        }
                    }
                }

                private static int _DefaultDatabasePort
                {
                    get
                    {
                        if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("dbport")))
                        {
                            return int.Parse(Environment.GetEnvironmentVariable("dbport"));
                        }
                        else
                        {
                            return 3306;
                        }
                    }
                }

                public string HostName = _DefaultHostName;
                public string UserName = _DefaultUserName;
                public string Password = _DefaultPassword;
                public string DatabaseName = _DefaultDatabaseName;
                public int Port = _DefaultDatabasePort;

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
                        return Path.Combine(Config.ConfigurationPath, "Data");
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

                public string LibraryFirmwareDirectory
                {
                    get
                    {
                        return Path.Combine(LibraryRootDirectory, "Firmware");
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

                public string LibraryMetadataDirectory_Platform(HasheousClient.Models.Metadata.IGDB.Platform platform)
                {
                    string MetadataPath = Path.Combine(LibraryMetadataDirectory, "Platforms", platform.Slug);
                    if (!Directory.Exists(MetadataPath)) { Directory.CreateDirectory(MetadataPath); }
                    return MetadataPath;
                }

                public string LibraryMetadataDirectory_Game(gaseous_server.Models.Game game)
                {
                    string MetadataPath = Path.Combine(LibraryMetadataDirectory, "Games", game.Slug);
                    if (!Directory.Exists(MetadataPath)) { Directory.CreateDirectory(MetadataPath); }
                    return MetadataPath;
                }

                public string LibraryMetadataDirectory_Company(HasheousClient.Models.Metadata.IGDB.Company company)
                {
                    string MetadataPath = Path.Combine(LibraryMetadataDirectory, "Companies", company.Slug);
                    if (!Directory.Exists(MetadataPath)) { Directory.CreateDirectory(MetadataPath); }
                    return MetadataPath;
                }

                public string LibrarySignaturesDirectory
                {
                    get
                    {
                        return Path.Combine(LibraryRootDirectory, "Signatures");
                    }
                }

                public string LibrarySignaturesProcessedDirectory
                {
                    get
                    {
                        return Path.Combine(LibraryRootDirectory, "Signatures - Processed");
                    }
                }

                public void InitLibrary()
                {
                    if (!Directory.Exists(LibraryRootDirectory)) { Directory.CreateDirectory(LibraryRootDirectory); }
                    if (!Directory.Exists(LibraryImportDirectory)) { Directory.CreateDirectory(LibraryImportDirectory); }
                    // if (!Directory.Exists(LibraryBIOSDirectory)) { Directory.CreateDirectory(LibraryBIOSDirectory); }
                    if (!Directory.Exists(LibraryFirmwareDirectory)) { Directory.CreateDirectory(LibraryFirmwareDirectory); }
                    if (!Directory.Exists(LibraryUploadDirectory)) { Directory.CreateDirectory(LibraryUploadDirectory); }
                    if (!Directory.Exists(LibraryMetadataDirectory)) { Directory.CreateDirectory(LibraryMetadataDirectory); }
                    if (!Directory.Exists(LibraryTempDirectory)) { Directory.CreateDirectory(LibraryTempDirectory); }
                    if (!Directory.Exists(LibraryCollectionsDirectory)) { Directory.CreateDirectory(LibraryCollectionsDirectory); }
                    if (!Directory.Exists(LibrarySignaturesDirectory)) { Directory.CreateDirectory(LibrarySignaturesDirectory); }
                    if (!Directory.Exists(LibrarySignaturesProcessedDirectory)) { Directory.CreateDirectory(LibrarySignaturesProcessedDirectory); }
                }
            }

            public class MetadataAPI
            {
                public static string _HasheousClientAPIKey
                {
                    get
                    {
                        return "Pna5SRcbJ6R8aasytab_6vZD0aBKDGNZKRz4WY4xArpfZ-3mdZq0hXIGyy0AD43b";
                    }
                }

                private static HasheousClient.Models.MetadataSources _MetadataSource
                {
                    get
                    {
                        if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("metadatasource")))
                        {
                            return (HasheousClient.Models.MetadataSources)Enum.Parse(typeof(HasheousClient.Models.MetadataSources), Environment.GetEnvironmentVariable("metadatasource"));
                        }
                        else
                        {
                            return HasheousClient.Models.MetadataSources.IGDB;
                        }
                    }
                }

                private static bool _MetadataUseHasheousProxy
                {
                    get
                    {
                        if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("metadatausehasheousproxy")))
                        {
                            return bool.Parse(Environment.GetEnvironmentVariable("metadatausehasheousproxy"));
                        }
                        else
                        {
                            return true;
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

                private static bool _HasheousSubmitFixes { get; set; } = false;

                private static string _HasheousAPIKey { get; set; } = "";

                private static string _HasheousHost
                {
                    get
                    {
                        if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("hasheoushost")))
                        {
                            return Environment.GetEnvironmentVariable("hasheoushost");
                        }
                        else
                        {
                            return "https://hasheous.org/";
                        }
                    }
                }

                public HasheousClient.Models.MetadataSources DefaultMetadataSource = _MetadataSource;

                public HasheousClient.Models.MetadataModel.SignatureSources SignatureSource = _SignatureSource;

                public bool HasheousSubmitFixes = _HasheousSubmitFixes;

                public string HasheousAPIKey = _HasheousAPIKey;

                [JsonIgnore]
                public string HasheousClientAPIKey = _HasheousClientAPIKey;

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

                private static bool _MetadataUseHasheousProxy
                {
                    get
                    {
                        if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("igdbusehasheousproxy")))
                        {
                            return bool.Parse(Environment.GetEnvironmentVariable("igdbusehasheousproxy"));
                        }
                        else
                        {
                            return false;
                        }
                    }
                }

                public string ClientId = _DefaultIGDBClientId;
                public string Secret = _DefaultIGDBSecret;
                public bool UseHasheousProxy = _MetadataUseHasheousProxy;
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
