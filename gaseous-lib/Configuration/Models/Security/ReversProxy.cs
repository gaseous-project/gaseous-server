using Newtonsoft.Json;

namespace gaseous_server.Classes.Configuration.Models.Security
{
    public class ReverseProxy
    {
        // If you have an upstream reverse proxy (nginx/traefik/caddy), add its IPs here.
        // Example: [ "127.0.0.1", "10.0.0.2" ]
        public List<string> KnownProxies { get; set; } = new List<string>();

        // Known networks in CIDR notation.
        // Example: [ "10.0.0.0/8", "192.168.0.0/16" ]
        public List<string> KnownNetworks { get; set; } = new List<string>();

        // Aligns with ForwardedHeadersOptions.RequireHeaderSymmetry
        public bool RequireHeaderSymmetry { get; set; } = false;
    }
}