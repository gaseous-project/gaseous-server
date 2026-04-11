using Newtonsoft.Json;

namespace gaseous_server.Classes.Configuration.Models
{
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

        private static string _DefaultDatabaseEngine
        {
            get
            {
                if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("dbengine")))
                {
                    return Environment.GetEnvironmentVariable("dbengine");
                }
                else
                {
                    return "mysql";
                }
            }
        }

        public string HostName = _DefaultHostName;
        public string UserName = _DefaultUserName;
        public string Password = _DefaultPassword;
        public string DatabaseName = _DefaultDatabaseName;
        public int Port = _DefaultDatabasePort;

        /// <summary>
        /// Selects the database engine family used for migration tooling and backup commands.
        /// </summary>
        public string DatabaseEngine = _DefaultDatabaseEngine;

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
}