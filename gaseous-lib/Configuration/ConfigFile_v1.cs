using Newtonsoft.Json;

namespace gaseous_server.Classes
{
    /// <summary>
    /// The ConfigFile class represents the structure of the configuration file used by the server. It contains properties for each section of the configuration, such as database settings, library settings, metadata API settings, IGDB settings, social authentication settings, logging settings, and reverse proxy settings. This class is used to deserialize the JSON config file into a strongly-typed object that can be easily accessed throughout the codebase. Each property corresponds to a section in the config file and is initialized with default values. When the config file is loaded, it populates an instance of this class with the values from the file, allowing for easy access to configuration settings through the static properties in the Config class.
    /// This is the version 1 structure of the config file. If breaking changes are made to the config structure in the future, a new version of this class should be created (e.g. ConfigFileV2) to represent the new structure, and the static constructor of the Config class should be updated to handle loading both versions of the config file and migrating from the old version to the new version if necessary. This allows for backward compatibility and smooth upgrades without breaking existing configurations.
    /// </summary>
    public class ConfigFile
    {
        /// <summary>
        /// The database configuration section of the config, containing settings related to the database connection such as host, port, username, password, and database name. This is used throughout the codebase wherever a database connection is needed, and is loaded from the config file on initialization of this class.
        /// </summary>
        public gaseous_server.Classes.Configuration.Models.Database DatabaseConfiguration = new gaseous_server.Classes.Configuration.Models.Database();

        /// <summary>
        /// The library configuration section of the config, containing settings related to the game library such as the library path, whether to include subfolders, and other related settings. This is used throughout the codebase wherever access to the game library is needed, and is loaded from the config file on initialization of this class.
        /// </summary>
        [JsonIgnore]
        public gaseous_server.Classes.Configuration.Models.Library LibraryConfiguration = new gaseous_server.Classes.Configuration.Models.Library();

        /// <summary>
        /// The metadata API configuration section of the config, containing settings related to fetching game metadata such as the default metadata source, Hasheous proxy settings, and other related settings. This is used throughout the codebase wherever game metadata fetching is needed, and is loaded from the config file on initialization of this class.
        /// </summary>
        public gaseous_server.Classes.Configuration.Models.MetadataAPI MetadataConfiguration = new gaseous_server.Classes.Configuration.Models.MetadataAPI();

        /// <summary>
        /// The IGDB configuration section of the config, containing settings related to fetching game metadata from IGDB such as API credentials and whether to use a proxy. This is used throughout the codebase wherever IGDB metadata fetching is needed, and is loaded from the config file on initialization of this class.
        /// </summary>
        public gaseous_server.Classes.Configuration.Models.Providers.IGDB IGDBConfiguration = new gaseous_server.Classes.Configuration.Models.Providers.IGDB();

        /// <summary>
        /// The social authentication configuration section of the config, containing settings related to social authentication such as whether password login is enabled and API credentials for various social auth providers. This is used throughout the codebase wherever authentication is needed, and is loaded from the config file on initialization of this class.
        /// </summary>
        public gaseous_server.Classes.Configuration.Models.Security.SocialAuth SocialAuthConfiguration = new gaseous_server.Classes.Configuration.Models.Security.SocialAuth();

        /// <summary>
        /// The logging configuration section of the config, containing settings related to logging such as log levels, whether to log to file, and other related settings. This is used throughout the codebase wherever logging is needed, and is loaded from the config file on initialization of this class.
        /// </summary>
        public gaseous_server.Classes.Configuration.Models.Logging LoggingConfiguration = new gaseous_server.Classes.Configuration.Models.Logging();

        /// <summary>
        /// The reverse proxy configuration section of the config, containing settings related to reverse proxy setup such as known proxies and networks. This is used in the web server setup to configure trusted proxies and networks for scenarios where the server is running behind a reverse proxy. It is loaded from the config file on initialization of this class, and can be overridden with environment variables when running in Docker for easy configuration without modifying the config file.
        /// </summary>
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
                catch
                {
                    // errors can be ignored as we'll just return the default port if there's an issue with the environment variable
                }
                return 5198;
            }
        }

        /// <summary>
        /// The port that the web server listens on (Kestrel). This is used in the web server setup to determine which port to bind to for incoming HTTP requests. The default value is 5198, but it can be overridden with the "webport" environment variable when running in Docker for easy configuration without modifying the config file. This allows users running in Docker to specify the desired port through environment variables, while still providing a sensible default for users running outside of Docker.
        /// </summary>
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

        /// <summary>
        /// The language that the server uses for its responses and localization. This is used throughout the codebase wherever localized strings are needed, and is loaded from the config file on initialization of this class. The default value is determined by the "serverlanguage" environment variable when running in Docker, allowing for easy configuration of the server language without modifying the config file. If the environment variable is not set, it falls back to the default locale defined in the Localisation class. This allows users to specify their preferred server language through environment variables when running in Docker, while still providing a sensible default for users running outside of Docker.
        /// </summary>
        public string ServerLanguage = _ServerLanguage;
    }
}