using Newtonsoft.Json;

namespace gaseous_server.Classes.Configuration.Models
{
    public class Logging
    {
        public bool DebugLogging = false;

        // log retention in days
        public int LogRetention = 7;

        public bool AlwaysLogToDisk = false;
    }
}