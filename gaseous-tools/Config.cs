using System;
using System.Data;
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
                    string configRaw = Newtonsoft.Json.JsonConvert.SerializeObject(_config, Newtonsoft.Json.Formatting.Indented);
                    File.WriteAllText(ConfigurationFilePath, configRaw);
                }
            }

            Console.WriteLine("Using configuration:");
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(_config, Formatting.Indented));
        }

        public static void UpdateConfig()
        {
            // save any updates to the configuration
            string configRaw = Newtonsoft.Json.JsonConvert.SerializeObject(_config, Newtonsoft.Json.Formatting.Indented);
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

        private static string ReadSetting(string SettingName, string DefaultValue)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "SELECT * FROM settings WHERE setting = @settingname";
            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            dbDict.Add("settingname", SettingName);
            dbDict.Add("value", DefaultValue);

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

        private static void SetSetting(string SettingName, string Value)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "REPLACE INTO settings (setting, value) VALUES (@settingname, @value)";
            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            dbDict.Add("settingname", SettingName);
            dbDict.Add("value", Value);

            db.ExecuteCMD(sql, dbDict);
        }

        public class ConfigFile
        {
            public Database DatabaseConfiguration = new Database();

            [JsonIgnore]
            public Library LibraryConfiguration = new Library();

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
            }
        }
    }
}

