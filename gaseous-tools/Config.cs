using System;
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

        public class ConfigFile
        {
            public Database DatabaseConfiguration = new Database();

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
        }
    }
}

