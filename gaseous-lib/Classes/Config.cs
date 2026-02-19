using System;
using System.Data;
using Newtonsoft.Json;
using gaseous_server.Classes.Metadata;
using NuGet.Common;

namespace gaseous_server.Classes
{
    /// <summary>
    /// Central static class for accessing configuration throughout the codebase. This class loads the configuration from the config file on initialization and provides static properties for accessing various configuration sections, as well as a method for updating/saving the config file when changes are made.
    /// </summary>
    public static class Config
    {
        static ConfigFile _config;

        /// <summary>
        /// The path where the configuration file and related files (logs, localisation) are stored. By default this is in the user's profile folder under .gaseous-server, but can be overridden by setting the GASEOUS_CONFIG_PATH environment variable (useful for services/containers). The config file itself is stored as config.json within this folder, and a backup of the previous config is stored as config.json.backup when changes are made.
        /// </summary>
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

        /// <summary>
        /// The port the web server listens on (Kestrel). Default 5198. Can be overridden by setting the "webport" environment variable or by changing the value in the config file. When running in a container, the environment variable will take precedence over the config file value to allow for easy configuration without needing to modify the config file.
        /// </summary>
        public static int ServerPort
        {
            get { return _config.ServerPort; }
            set { _config.ServerPort = value; }
        }

        /// <summary>
        /// The port used for local communication between the main server process and the out-of-process task host. This is used for sending commands and receiving status updates from the out-of-process task host when executing long-running tasks. The default value is 5199, but it can be overridden with the "localcommsport" environment variable or by changing the value in the config file. When running in a container, the environment variable will take precedence over the config file value to allow for easy configuration without needing to modify the config file.
        /// </summary>
        public static int LocalCommsPort
        {
            get { return _config.LocalCommsPort; }
            set { _config.LocalCommsPort = value; }
        }

        /// <summary>
        /// The default language/locale the server uses for responses and localisation. This is in the format of a standard locale string, e.g. "en-US" or "fr-FR". The default is "en-US". Can be overridden by setting the "serverlanguage" environment variable or by changing the value in the config file. When running in a container, the environment variable will take precedence over the config file value to allow for easy configuration without needing to modify the config file.
        /// </summary>
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

        static string ConfigurationFilePath_Version2
        {
            get
            {
                return Path.Combine(ConfigurationPath, "configuration.json");
            }
        }

        static string ConfigurationFilePath_Backup_Version2
        {
            get
            {
                return Path.Combine(ConfigurationPath, "configuration.json.backup");
            }
        }

        #region Configuration Accessors
        // These provide easy access to the various configuration sections throughout the codebase without needing to reference the entire config object

        /// <summary>
        /// The database configuration section of the config, containing all relevant settings for connecting to the database. This is used throughout the codebase wherever database access is needed, and is loaded from the config file on initialization of this class.
        /// </summary>
        public static gaseous_server.Classes.Configuration.Models.Database DatabaseConfiguration
        {
            get
            {
                return _config.DatabaseConfiguration;
            }
        }

        /// <summary>
        /// The library configuration section of the config, containing settings related to the game library management such as file paths, naming templates, and other related settings. This is used throughout the codebase wherever library management is needed, and is loaded from the config file on initialization of this class.
        /// </summary>
        public static gaseous_server.Classes.Configuration.Models.Library LibraryConfiguration
        {
            get
            {
                return _config.LibraryConfiguration;
            }
        }

        /// <summary>
        /// The metadata API configuration section of the config, containing settings related to fetching metadata for games such as which metadata source to use, API keys for external services, and other related settings. This is used throughout the codebase wherever metadata fetching is needed, and is loaded from the config file on initialization of this class.
        /// </summary>
        public static gaseous_server.Classes.Configuration.Models.MetadataAPI MetadataConfiguration
        {
            get
            {
                return _config.MetadataConfiguration;
            }
        }

        /// <summary>
        /// The IGDB configuration section of the config, containing settings specific to fetching metadata from IGDB such as API keys and whether to use the Hasheous proxy. This is used throughout the codebase wherever IGDB metadata fetching is needed, and is loaded from the config file on initialization of this class.
        /// </summary>
        public static gaseous_server.Classes.Configuration.Models.Providers.IGDB IGDB
        {
            get
            {
                return _config.IGDBConfiguration;
            }
        }

        /// <summary>
        /// The social authentication configuration section of the config, containing settings related to enabling social login options for users such as Google, Microsoft, and OIDC providers, as well as whether to allow password login. This is used throughout the codebase wherever authentication is needed, and is loaded from the config file on initialization of this class.
        /// </summary>
        public static gaseous_server.Classes.Configuration.Models.Security.SocialAuth SocialAuthConfiguration
        {
            get
            {
                return _config.SocialAuthConfiguration;
            }
        }

        /// <summary>
        /// The reverse proxy configuration section of the config, containing settings related to running the server behind a reverse proxy such as known proxy IPs/networks and whether to trust forwarded headers. This is used in the server setup to configure ASP.NET Core's Forwarded Headers middleware when the server is running in an environment where a reverse proxy is likely (e.g. in Docker or behind Nginx), and is loaded from the config file on initialization of this class.
        /// </summary>
        public static gaseous_server.Classes.Configuration.Models.Security.ReverseProxy ReverseProxyConfiguration
        {
            get
            {
                return _config.ReverseProxyConfiguration;
            }
        }

        /// <summary>
        /// The logging configuration section of the config, containing settings related to logging such as log levels, whether to log to file, and other related settings. This is used throughout the codebase wherever logging is needed, and is loaded from the config file on initialization of this class.
        /// </summary>
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

        /// <summary>
        /// The localisation configuration section of the config, containing settings related to localisation such as the default server language/locale and other related settings. This is used throughout the codebase wherever localisation is needed, and is loaded from the config file on initialization of this class.
        /// </summary>
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

        /// <summary>
        /// The file path for the server log file. The log file is stored in the Logs subfolder of the configuration path, and is named "Server Log [UTC Date].txt" where [UTC Date] is the current date in UTC in the format YYYYMMDD. For example, "Server Log 20240101.txt". This allows for easy organization of log files by date. The log file path is used by the logging system when writing logs to file, and is determined based on the current date to ensure logs are separated by day.
        /// </summary>
        public static string LogFilePath
        {
            get
            {
                string logFileExtension = "txt";

                string logPathName = Path.Combine(LogPath, "Server Log " + DateTime.Now.ToUniversalTime().ToString("yyyyMMdd") + "." + logFileExtension);
                return logPathName;
            }
        }

        /// <summary>
        /// The logging configuration section of the config, containing settings related to logging such as log levels, whether to log to file, and other related settings. This is used throughout the codebase wherever logging is needed, and is loaded from the config file on initialization of this class.
        /// </summary>
        public static gaseous_server.Classes.Configuration.Models.Logging LoggingConfiguration
        {
            get
            {
                return _config.LoggingConfiguration;
            }
        }

        /// <summary>
        /// The first run status setting, stored in the database, which indicates whether the server is being run for the first time (e.g. for initial setup) or not. This is used to determine whether to show the initial setup page in the web interface and to perform any necessary first-run initialization tasks. The value is stored as a string in the database and can be "0" for not first run, "1" for first run in progress, and "2" for first run completed. This is accessed via the ReadSetting method which reads from the database, and defaults to "0" if not set.
        /// </summary>
        [JsonIgnore]
        public static string FirstRunStatus
        {
            get
            {
                return Config.ReadSetting<string>("FirstRunStatus", "0");
            }
        }

        /// <summary>
        /// The value to set for the first run status when the initial setup is completed. This is used to update the first run status in the database to indicate that the initial setup has been completed. This value is "2" and is used in the code after the initial setup process is finished to mark that the server has completed its first run initialization.
        /// </summary>
        public static string FirstRunStatusWhenSet
        {
            get
            {
                return "2";
            }
        }
        #endregion Configuration Accessors

        /// <summary>
        /// Static constructor for the Config class. This is called automatically when any member of the Config class is accessed for the first time. It loads the configuration from the config file, and if running in a Docker container, it overrides certain configuration values with environment variables to allow for easy configuration without needing to modify the config file. If the config file does not exist, it creates a new config with default values and saves it. This ensures that the configuration is loaded and available for use throughout the codebase whenever any Config property is accessed.
        /// </summary>
        /// <exception cref="Exception">
        /// This can throw exceptions if there are issues reading the config file (e.g. invalid JSON format) or if required configuration values are missing. It can also throw exceptions if there are issues with the environment variables when running in Docker (e.g. invalid values that cannot be parsed). These exceptions should be handled by the caller to ensure that the server can provide appropriate error messages and fallback behavior if the configuration cannot be loaded successfully.
        /// </exception>
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
                        if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("INDOCKER")) && Environment.GetEnvironmentVariable("INDOCKER") == "1")
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
                    }
                    else
                    {
                        throw new InvalidOperationException("There was an error reading the config file: Json returned null");
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
        }

        /// <summary>
        /// Saves any updates made to the configuration back to the config file. This should be called whenever changes are made to the configuration properties that need to be persisted. It serializes the current state of the _config object to JSON and writes it to the config file path. Before writing, it creates a backup of the existing config file by renaming it with a .backup extension. This ensures that if there are any issues with the new config (e.g. invalid JSON format), the previous config can be restored from the backup file. This method should be used whenever changes are made to the configuration that need to be saved, such as when updating settings through an admin interface or when making changes programmatically.
        /// </summary>
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

        /// <summary>
        /// Initializes the application settings by loading them from the database. This method reads all settings from the Settings table in the database and stores them in the AppSettings dictionary for quick access. It handles different value types (string and datetime) based on the database schema version, and logs any exceptions that occur during the reading of settings. If a setting cannot be read due to an invalid cast (e.g. due to a schema change during an upgrade), it deletes the broken setting from the database and logs a warning, allowing the application to reset it to a default value when accessed again. This method should be called during application startup after the database connection is established to ensure that all settings are loaded and available for use throughout the application.
        /// </summary>
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

                try
                {
                    if (Database.schema_version >= 1016)
                    {
                        switch ((int)dataRow["ValueType"])
                        {
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
                    db.ExecuteCMD(sql, dbDict);
                }
                catch (Exception ex)
                {
                    Logging.LogKey(Logging.LogType.Critical, "process.settings", "settings.exception_when_reading_server_setting", null, new string[] { SettingName }, ex);
                }
            }
        }

        /// <summary>
        /// Reads a setting value from the AppSettings dictionary, which is loaded from the database on application startup. If the setting is not found in the AppSettings dictionary, it attempts to read it from the database. If the setting is not found in the database, it returns the provided default value and saves that default value to the database for future access. This method also handles caching of settings in the AppSettings dictionary for quick access, and logs any exceptions that occur during the reading of settings. If an invalid cast exception occurs (e.g. due to a schema change during an upgrade), it deletes the broken setting from the database and logs a warning, allowing the application to reset it to a default value when accessed again. This method should be used throughout the codebase whenever access to a setting value is needed, as it ensures that settings are read from the database if not already cached, and that default values are used and saved when settings are missing.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the setting value.
        /// </typeparam>
        /// <param name="SettingName">The name of the setting to read.</param>
        /// <param name="DefaultValue">The default value to return if the setting is not found.</param>
        /// <returns>The value of the setting, or the default value if the setting is not found.</returns>
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
                            if (typeof(T) == typeof(DateTime) || typeof(T) == typeof(Nullable<DateTime>))
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
                db.ExecuteCMD(sql, dbDict);

                return DefaultValue;
            }
            catch (Exception ex)
            {
                Logging.LogKey(Logging.LogType.Critical, "process.settings", "settings.exception_when_reading_server_setting", null, new string[] { SettingName }, ex);
                throw;
            }
        }

        /// <summary>
        /// Writes a setting value to the database and updates the AppSettings dictionary. This method takes a setting name and value, and saves it to the Settings table in the database. It handles different value types (string and datetime) based on the database schema version, and logs any exceptions that occur during the saving of settings. If an exception occurs, it is logged and rethrown to be handled by the caller. This method should be used whenever a setting value needs to be updated or saved, as it ensures that the new value is persisted to the database and that the AppSettings cache is updated accordingly for quick access.
        /// </summary>
        /// <typeparam name="T">The type of the setting value.</typeparam>
        /// <param name="SettingName">The name of the setting to write.</param>
        /// <param name="Value">The value to write to the setting.</param>
        public static void SetSetting<T>(string SettingName, T Value)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql;
            Dictionary<string, object> dbDict;

            if (Database.schema_version >= 1016)
            {
                sql = "REPLACE INTO Settings (Setting, ValueType, Value, ValueDate) VALUES (@SettingName, @ValueType, @Value, @ValueDate)";
                if (typeof(T) == typeof(DateTime) || typeof(T) == typeof(Nullable<DateTime>))
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
    }
}
