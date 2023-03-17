using System;
using System.Data;
using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json;

namespace gaseous_tools
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
                string logPathName = Path.Combine(LogPath, "Log " + DateTime.Now.ToUniversalTime().ToString("yyyyMMdd") + ".txt");
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

        public static string ReadSetting(string SettingName, string DefaultValue)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "SELECT * FROM settings WHERE setting = @settingname";
            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            dbDict.Add("settingname", SettingName);
            dbDict.Add("value", DefaultValue);

            try
            {
                Logging.Log(Logging.LogType.Debug, "Database", "Reading setting '" + SettingName + "'");
                DataTable dbResponse = db.ExecuteCMD(sql, dbDict);
                if (dbResponse.Rows.Count == 0)
                {
                    // no value with that name stored - respond with the default value
                    return DefaultValue;
                }
                else
                {
                    return (string)dbResponse.Rows[0][0];
                }
            }
            catch (Exception ex)
            {
                Logging.Log(Logging.LogType.Critical, "Database", "Failed reading setting " + SettingName, ex);
                throw;
            }
        }

        public static void SetSetting(string SettingName, string Value)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "REPLACE INTO settings (setting, value) VALUES (@settingname, @value)";
            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            dbDict.Add("settingname", SettingName);
            dbDict.Add("value", Value);

            Logging.Log(Logging.LogType.Debug, "Database", "Storing setting '" + SettingName + "' to value: '" + Value + "'");
            try
            {
                db.ExecuteCMD(sql, dbDict);
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

            public Logging LoggingConfiguration = new Logging();

            public class Database
            {
                public string HostName = "localhost";
                public string UserName = "gaseous";
                public string Password = "gaseous";
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
            }

            public class Library
            {
                public string LibraryRootDirectory
                {
                    get
                    {
                        return ReadSetting("LibraryRootDirectory", Path.Combine(Config.ConfigurationPath, "Data"));
                    }
                    set
                    {
                        SetSetting("LibraryRootDirectory", value);
                    }
                }

                public string LibraryUploadDirectory
                {
                    get
                    {
                        return Path.Combine(LibraryRootDirectory, "Upload");
                    }
                }

                public string LibraryImportDirectory
                {
                    get
                    {
                        return Path.Combine(LibraryRootDirectory, "Import");
                    }
                }

                public string LibraryDataDirectory
                {
                    get
                    {
                        return Path.Combine(LibraryRootDirectory, "Library");
                    }
                }

                public string LibrarySignatureImportDirectory
                {
                    get
                    {
                        return Path.Combine(LibraryRootDirectory, "Signatures");
                    }
                }

                public string LibrarySignatureImportDirectory_TOSEC
                {
                    get
                    {
                        return Path.Combine(LibrarySignatureImportDirectory, "TOSEC");
                    }
                }

                public void InitLibrary()
                {
                    if (!Directory.Exists(LibraryRootDirectory)) { Directory.CreateDirectory(LibraryRootDirectory); }
                    if (!Directory.Exists(LibraryUploadDirectory)) { Directory.CreateDirectory(LibraryUploadDirectory); }
                    if (!Directory.Exists(LibraryImportDirectory)) { Directory.CreateDirectory(LibraryImportDirectory); }
                    if (!Directory.Exists(LibraryDataDirectory)) { Directory.CreateDirectory(LibraryDataDirectory); }
                    if (!Directory.Exists(LibrarySignatureImportDirectory)) { Directory.CreateDirectory(LibrarySignatureImportDirectory); }
                    if (!Directory.Exists(LibrarySignatureImportDirectory_TOSEC)) { Directory.CreateDirectory(LibrarySignatureImportDirectory_TOSEC); }
                }
            }

            public class Logging
            {
                public bool DebugLogging = false;

                public LoggingFormat LogFormat = Logging.LoggingFormat.Json;

                public enum LoggingFormat
                {
                    Json,
                    Text
                }
            }
        }
    }
}
