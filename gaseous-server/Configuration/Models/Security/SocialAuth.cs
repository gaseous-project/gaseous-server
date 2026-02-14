using Newtonsoft.Json;

namespace gaseous_server.Classes.Configuration.Models.Security
{
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
}