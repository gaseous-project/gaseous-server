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

        public static string ServerLanguage
        {
            get { return _config.ServerLanguage; }
            set { _config.ServerLanguage = value; }
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

        public static gaseous_server.Classes.Configuration.Models.Database DatabaseConfiguration
        {
            get
            {
                return _config.DatabaseConfiguration;
            }
        }

        public static gaseous_server.Classes.Configuration.Models.Library LibraryConfiguration
        {
            get
            {
                return _config.LibraryConfiguration;
            }
        }

        public static gaseous_server.Classes.Configuration.Models.MetadataAPI MetadataConfiguration
        {
            get
            {
                return _config.MetadataConfiguration;
            }
        }

        public static gaseous_server.Classes.Configuration.Models.Providers.IGDB IGDB
        {
            get
            {
                return _config.IGDBConfiguration;
            }
        }

        public static gaseous_server.Classes.Configuration.Models.Security.SocialAuth SocialAuthConfiguration
        {
            get
            {
                return _config.SocialAuthConfiguration;
            }
        }

        public static gaseous_server.Classes.Configuration.Models.Security.ReverseProxy ReverseProxyConfiguration
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

        public static string LocalisationPath
        {
            get
            {
                string localisationPath = Path.Combine(ConfigurationPath, "Localisation");
                if (!Directory.Exists(localisationPath))
                {
                    Directory.CreateDirectory(localisationPath);
                }
                return localisationPath;
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

        public static gaseous_server.Classes.Configuration.Models.Logging LoggingConfiguration
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
                    Logging.LogKey(Logging.LogType.Warning, "process.settings", "settings.exception_when_reading_server_setting_resetting_to_default", null, new string[] { SettingName }, castEx);

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
                    Logging.LogKey(Logging.LogType.Critical, "process.settings", "settings.exception_when_reading_server_setting", null, new string[] { SettingName }, ex);
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
                        Logging.LogKey(Logging.LogType.Debug, "process.database", "database.reading_setting", null, new string[] { SettingName });

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
                        Logging.LogKey(Logging.LogType.Critical, "process.database", "database.failed_reading_setting", null, new string[] { SettingName }, ex);
                        throw;
                    }
                }
            }
            catch (InvalidCastException castEx)
            {
                Logging.LogKey(Logging.LogType.Warning, "process.settings", "settings.exception_when_reading_server_setting_resetting_to_default", null, new string[] { SettingName }, castEx);

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
                Logging.LogKey(Logging.LogType.Critical, "process.settings", "settings.exception_when_reading_server_setting", null, new string[] { SettingName }, ex);
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

            Logging.LogKey(Logging.LogType.Debug, "process.database", "database.storing_setting_to_value", null, new string[] { SettingName, Value?.ToString() ?? "" });
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
                Logging.LogKey(Logging.LogType.Critical, "process.database", "database.failed_storing_setting", null, new string[] { SettingName }, ex);
                throw;
            }
        }

        public class ConfigFile
        {
            public gaseous_server.Classes.Configuration.Models.Database DatabaseConfiguration = new gaseous_server.Classes.Configuration.Models.Database();

            [JsonIgnore]
            public gaseous_server.Classes.Configuration.Models.Library LibraryConfiguration = new gaseous_server.Classes.Configuration.Models.Library();

            public gaseous_server.Classes.Configuration.Models.MetadataAPI MetadataConfiguration = new gaseous_server.Classes.Configuration.Models.MetadataAPI();

            public gaseous_server.Classes.Configuration.Models.Providers.IGDB IGDBConfiguration = new gaseous_server.Classes.Configuration.Models.Providers.IGDB();

            public gaseous_server.Classes.Configuration.Models.Security.SocialAuth SocialAuthConfiguration = new gaseous_server.Classes.Configuration.Models.Security.SocialAuth();

            public gaseous_server.Classes.Configuration.Models.Logging LoggingConfiguration = new gaseous_server.Classes.Configuration.Models.Logging();

            public gaseous_server.Classes.Configuration.Models.Security.ReverseProxy ReverseProxyConfiguration = new gaseous_server.Classes.Configuration.Models.Security.ReverseProxy();

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

            private static string _ServerLanguage
            {
                get
                {
                    var env = Environment.GetEnvironmentVariable("serverlanguage");
                    if (!string.IsNullOrWhiteSpace(env)) return env;
                    return Localisation.DefaultLocale;
                }
            }

            public string ServerLanguage = _ServerLanguage;
        }
    }
}
