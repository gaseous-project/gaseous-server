using Newtonsoft.Json;

namespace gaseous_server.Classes.Configuration.Models
{
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
                    return "https://hasheous.org";
                }
            }
        }

        public FileSignature.MetadataSources DefaultMetadataSource = _MetadataSource;

        public HasheousClient.Models.MetadataModel.SignatureSources SignatureSource = _SignatureSource;

        public bool HasheousSubmitFixes = _HasheousSubmitFixes;

        public string HasheousAPIKey = _HasheousAPIKey;

        [JsonIgnore]
        public string HasheousClientAPIKey = _HasheousClientAPIKey;

        private string _HasheousHostValue = _HasheousHost;
        public string HasheousHost
        {
            get
            {
                // strip trailing slash if exists
                if (_HasheousHostValue.EndsWith("/"))
                {
                    return _HasheousHostValue.Substring(0, _HasheousHostValue.Length - 1);
                }
                return _HasheousHostValue;
            }
            set
            {
                _HasheousHostValue = value;
            }
        }
    }
}