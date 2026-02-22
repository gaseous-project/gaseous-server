using Newtonsoft.Json;

namespace gaseous_server.Classes.Configuration.Models.Providers
{
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
}