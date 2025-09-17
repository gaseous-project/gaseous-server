﻿using System;
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
                // Allow override via environment variable for services/containers
                var overridePath = Environment.GetEnvironmentVariable("GASEOUS_CONFIG_PATH");
                if (!string.IsNullOrWhiteSpace(overridePath))
                {
                    return overridePath;
                }

                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gaseous-server");
            }
        }

        public static int ServerPort
        {
            get { return _config.ServerPort; }
            set { _config.ServerPort = value; }
        }

        static string ConfigurationFilePath
        {
            get
            {
                return Path.Combine(ConfigurationPath, "config.json");
            }
        }

        static string ConfigurationFilePath_Backup
        {
            get
            {
                return Path.Combine(ConfigurationPath, "config.json.backup");
            }
        }

        public static string PlatformMappingFile
        {
            get
            {
                return Path.Combine(ConfigurationPath, "platformmap.json");
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

        public static ConfigFile.SocialAuth SocialAuthConfiguration
        {
            get
            {
                return _config.SocialAuthConfiguration;
            }
        }

        public static ConfigFile.ReverseProxy ReverseProxyConfiguration
        {
            get
            {
                return _config.ReverseProxyConfiguration;
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

        [JsonIgnore]
        public static string FirstRunStatus
        {
            get
            {
                return Config.ReadSetting<string>("FirstRunStatus", "0");
            }
        }

        public static string FirstRunStatusWhenSet
        {
            get
            {
                return "2";
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

                                _config.MetadataConfiguration.DefaultMetadataSource = (FileSignature.MetadataSources)Enum.Parse(typeof(FileSignature.MetadataSources), (string)Common.GetEnvVar("metadatasource", _config.MetadataConfiguration.DefaultMetadataSource.ToString()));
                                _config.IGDBConfiguration.UseHasheousProxy = bool.Parse((string)Common.GetEnvVar("metadatausehasheousproxy", _config.IGDBConfiguration.UseHasheousProxy.ToString()));
                                _config.MetadataConfiguration.SignatureSource = (HasheousClient.Models.MetadataModel.SignatureSources)Enum.Parse(typeof(HasheousClient.Models.MetadataModel.SignatureSources), (string)Common.GetEnvVar("signaturesource", _config.MetadataConfiguration.SignatureSource.ToString())); ;
                                _config.MetadataConfiguration.HasheousHost = (string)Common.GetEnvVar("hasheoushost", _config.MetadataConfiguration.HasheousHost);

                                _config.IGDBConfiguration.ClientId = (string)Common.GetEnvVar("igdbclientid", _config.IGDBConfiguration.ClientId);
                                _config.IGDBConfiguration.Secret = (string)Common.GetEnvVar("igdbclientsecret", _config.IGDBConfiguration.Secret);

                                _config.SocialAuthConfiguration.PasswordLoginEnabled = bool.Parse((string)Common.GetEnvVar("passwordloginenabled", _config.SocialAuthConfiguration.PasswordLoginEnabled.ToString()));
                                _config.SocialAuthConfiguration.GoogleClientId = (string)Common.GetEnvVar("googleclientid", _config.SocialAuthConfiguration.GoogleClientId);
                                _config.SocialAuthConfiguration.GoogleClientSecret = (string)Common.GetEnvVar("googleclientsecret", _config.SocialAuthConfiguration.GoogleClientSecret);
                                _config.SocialAuthConfiguration.MicrosoftClientId = (string)Common.GetEnvVar("microsoftclientid", _config.SocialAuthConfiguration.MicrosoftClientId);
                                _config.SocialAuthConfiguration.MicrosoftClientSecret = (string)Common.GetEnvVar("microsoftclientsecret", _config.SocialAuthConfiguration.MicrosoftClientSecret);
                                _config.SocialAuthConfiguration.OIDCAuthority = (string)Common.GetEnvVar("oidcauthority", _config.SocialAuthConfiguration.OIDCAuthority);
                                _config.SocialAuthConfiguration.OIDCClientId = (string)Common.GetEnvVar("oidcclientid", _config.SocialAuthConfiguration.OIDCClientId);
                                _config.SocialAuthConfiguration.OIDCClientSecret = (string)Common.GetEnvVar("oidcclientsecret", _config.SocialAuthConfiguration.OIDCClientSecret);
                            }
                        }

                        // reverse proxy configuration (known proxies/networks)
                        // Comma-separated IPs/CIDRs via env when running in Docker
                        var knownProxiesEnv = (string)Common.GetEnvVar("knownproxies", string.Join(",", _config.ReverseProxyConfiguration.KnownProxies ?? new List<string>()));
                        if (!string.IsNullOrWhiteSpace(knownProxiesEnv))
                        {
                            _config.ReverseProxyConfiguration.KnownProxies = knownProxiesEnv
                                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                .ToList();
                        }

                        var knownNetworksEnv = (string)Common.GetEnvVar("knownnetworks", string.Join(",", _config.ReverseProxyConfiguration.KnownNetworks ?? new List<string>()));
                        if (!string.IsNullOrWhiteSpace(knownNetworksEnv))
                        {
                            _config.ReverseProxyConfiguration.KnownNetworks = knownNetworksEnv
                                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                .ToList();
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

            public SocialAuth SocialAuthConfiguration = new SocialAuth();

            public Logging LoggingConfiguration = new Logging();

            public ReverseProxy ReverseProxyConfiguration = new ReverseProxy();

            // Port the web server listens on (Kestrel). Default 5198.
            private static int _DefaultServerPort
            {
                get
                {
                    try
                    {
                        var env = Environment.GetEnvironmentVariable("webport");
                        if (!string.IsNullOrWhiteSpace(env) && int.TryParse(env, out var p)) return p;
                    }
                    catch { }
                    return 5198;
                }
            }

            public int ServerPort = _DefaultServerPort;

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

                [JsonIgnore]
                public bool UpgradeInProgress { get; set; } = false;
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

                public string LibraryContentDirectory
                {
                    get
                    {
                        return Path.Combine(LibraryRootDirectory, "Content");
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

                public string LibraryMetadataDirectory_Hasheous()
                {
                    string MetadataPath = Path.Combine(LibraryMetadataDirectory, "Hasheous");
                    if (!Directory.Exists(MetadataPath)) { Directory.CreateDirectory(MetadataPath); }
                    return MetadataPath;
                }

                public string LibraryMetadataDirectory_TheGamesDB()
                {
                    string MetadataPath = Path.Combine(LibraryMetadataDirectory, "TheGamesDB");
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
                    if (!Directory.Exists(LibraryFirmwareDirectory)) { Directory.CreateDirectory(LibraryFirmwareDirectory); }
                    if (!Directory.Exists(LibraryUploadDirectory)) { Directory.CreateDirectory(LibraryUploadDirectory); }
                    if (!Directory.Exists(LibraryMetadataDirectory)) { Directory.CreateDirectory(LibraryMetadataDirectory); }
                    if (!Directory.Exists(LibraryContentDirectory)) { Directory.CreateDirectory(LibraryContentDirectory); }
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

                private static FileSignature.MetadataSources _MetadataSource
                {
                    get
                    {
                        if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("metadatasource")))
                        {
                            return (FileSignature.MetadataSources)Enum.Parse(typeof(FileSignature.MetadataSources), Environment.GetEnvironmentVariable("metadatasource"));
                        }
                        else
                        {
                            return FileSignature.MetadataSources.IGDB;
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

                public FileSignature.MetadataSources DefaultMetadataSource = _MetadataSource;

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

            public class SocialAuth
            {
                private static bool _PasswordLoginEnabled
                {
                    get
                    {
                        bool returnValue = true; // default to enabled
                        if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("passwordloginenabled")))
                        {
                            returnValue = bool.Parse(Environment.GetEnvironmentVariable("passwordloginenabled"));
                        }

                        // password login can only be disabled if at least one other auth method is enabled
                        if (!returnValue)
                        {
                            if (String.IsNullOrEmpty(_GoogleClientId) && String.IsNullOrEmpty(_MicrosoftClientId) && String.IsNullOrEmpty(_OIDCAuthority))
                            {
                                returnValue = true; // force password login to be enabled if no other auth methods are set
                            }
                        }
                        return returnValue;
                    }
                }

                private static string _GoogleClientId
                {
                    get
                    {
                        if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("googleclientid")))
                        {
                            return Environment.GetEnvironmentVariable("googleclientid");
                        }
                        else
                        {
                            return "";
                        }
                    }
                }

                private static string _GoogleClientSecret
                {
                    get
                    {
                        if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("googleclientsecret")))
                        {
                            return Environment.GetEnvironmentVariable("googleclientsecret");
                        }
                        else
                        {
                            return "";
                        }
                    }
                }

                private static string _MicrosoftClientId
                {
                    get
                    {
                        if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("microsoftclientid")))
                        {
                            return Environment.GetEnvironmentVariable("microsoftclientid");
                        }
                        else
                        {
                            return "";
                        }
                    }
                }

                private static string _MicrosoftClientSecret
                {
                    get
                    {
                        if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("microsoftclientsecret")))
                        {
                            return Environment.GetEnvironmentVariable("microsoftclientsecret");
                        }
                        else
                        {
                            return "";
                        }
                    }
                }

                private static string _OIDCAuthority
                {
                    get
                    {
                        if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("oidcauthority")))
                        {
                            return Environment.GetEnvironmentVariable("oidcauthority");
                        }
                        else
                        {
                            return "";
                        }
                    }
                }

                public static string _OIDCClientId
                {
                    get
                    {
                        if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("oidcclientid")))
                        {
                            return Environment.GetEnvironmentVariable("oidcclientid");
                        }
                        else
                        {
                            return "";
                        }
                    }
                }

                public static string _OIDCClientSecret
                {
                    get
                    {
                        if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("oidcclientsecret")))
                        {
                            return Environment.GetEnvironmentVariable("oidcclientsecret");
                        }
                        else
                        {
                            return "";
                        }
                    }
                }

                public bool PasswordLoginEnabled = _PasswordLoginEnabled;

                public string GoogleClientId = _GoogleClientId;
                public string GoogleClientSecret = _GoogleClientSecret;

                public string MicrosoftClientId = _MicrosoftClientId;
                public string MicrosoftClientSecret = _MicrosoftClientSecret;

                public string OIDCAuthority = _OIDCAuthority;
                public string OIDCClientId = _OIDCClientId;
                public string OIDCClientSecret = _OIDCClientSecret;

                [JsonIgnore]
                public bool GoogleAuthEnabled
                {
                    get
                    {
                        return !String.IsNullOrEmpty(GoogleClientId) && !String.IsNullOrEmpty(GoogleClientSecret);
                    }
                }

                [JsonIgnore]
                public bool MicrosoftAuthEnabled
                {
                    get
                    {
                        return !String.IsNullOrEmpty(MicrosoftClientId) && !String.IsNullOrEmpty(MicrosoftClientSecret);
                    }
                }

                [JsonIgnore]
                public bool OIDCAuthEnabled
                {
                    get
                    {
                        return !String.IsNullOrEmpty(OIDCAuthority) && !String.IsNullOrEmpty(OIDCClientId) && !String.IsNullOrEmpty(OIDCClientSecret);
                    }
                }
            }

            public class ReverseProxy
            {
                // If you have an upstream reverse proxy (nginx/traefik/caddy), add its IPs here.
                // Example: [ "127.0.0.1", "10.0.0.2" ]
                public List<string> KnownProxies { get; set; } = new List<string>();

                // Known networks in CIDR notation.
                // Example: [ "10.0.0.0/8", "192.168.0.0/16" ]
                public List<string> KnownNetworks { get; set; } = new List<string>();

                // Aligns with ForwardedHeadersOptions.RequireHeaderSymmetry
                public bool RequireHeaderSymmetry { get; set; } = false;
            }
        }
    }
}
