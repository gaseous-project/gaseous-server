

using gaseous_server.ProcessQueue;

namespace GaseousServerHost.Classes.CLI
{
    /// <summary>
    /// Provides methods for displaying command-line help information for the Gaseous Server Host.
    /// </summary>
    public class Help
    {
        /// <summary>
        /// Displays command-line help information for the Hasheous Server Host.
        /// </summary>
        public static void DisplayHelp()
        {
            Console.WriteLine("Gaseous Server - Process Host");
            Console.WriteLine("This program is used to run various background services for the Gaseous server.");
            Console.WriteLine("It is normally called by the server.");
            Console.WriteLine("");
            Console.WriteLine("Usage: gaseous-processhost [options]");
            Console.WriteLine("Options:");
            Console.WriteLine("  --help                     Display this help message");
            Console.WriteLine("  --version                  Display the version of the service");
            Console.WriteLine("  --service <name>           Specify the service name to run");
            Console.WriteLine("  --reportingserver <url>    Specify the reporting server URL");
            Console.WriteLine("  --processid <id>           Specify the process ID for the service instance");
            Console.WriteLine("  --correlationid <id>       Specify a correlation ID for logging");
            Console.WriteLine("");
            Console.WriteLine("Available services:");
            foreach (var service in Enum.GetNames(typeof(QueueItemType)))
            {
                if (service != "All" && service != "NotConfigured")
                {
                    Console.WriteLine($"  {service}");
                }
            }
            Console.WriteLine("");
            Console.WriteLine("Examples:");
            Console.WriteLine("  gaseous-processhost --service SignatureIngestor --reportingserver https://localhost");
            Console.WriteLine("  gaseous-processhost --version");
            Console.WriteLine("  gaseous-processhost --help");
            Environment.Exit(0);
        }
    }
}