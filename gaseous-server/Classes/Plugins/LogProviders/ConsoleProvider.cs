namespace gaseous_server.Classes.Plugins.LogProviders
{
    /// <summary>
    /// Provides logging functionality to the console output.
    /// </summary>
    public class ConsoleProvider : ILogProvider
    {
        /// <inheritdoc/>
        public string Name => "Console Log Provider";

        /// <inheritdoc/>
        public List<gaseous_server.Classes.Plugins.PluginManagement.OperatingSystems> SupportedOperatingSystems
        {
            get
            {
                List<gaseous_server.Classes.Plugins.PluginManagement.OperatingSystems> operatingSystems = new List<gaseous_server.Classes.Plugins.PluginManagement.OperatingSystems>
                {
                    gaseous_server.Classes.Plugins.PluginManagement.OperatingSystems.Linux,
                    gaseous_server.Classes.Plugins.PluginManagement.OperatingSystems.macOS
                };

                if (!PluginManagement.IsRunningAsWindowsService())
                {
                    operatingSystems.Add(gaseous_server.Classes.Plugins.PluginManagement.OperatingSystems.Windows);
                }

                return operatingSystems;
            }
        }

        /// <inheritdoc/>
        public bool SupportsLogFetch => false;

        /// <inheritdoc/>
        public Dictionary<string, object>? Settings { get; set; }

        /// <inheritdoc/>
        public async Task<bool> LogMessage(Logging.LogItem logItem)
        {
            return await LogMessage(logItem, null);
        }

        /// <inheritdoc/>
        public async Task<bool> LogMessage(Logging.LogItem logItem, Exception? exception)
        {
            string eventTypeString = logItem.EventTypeInfo.ColourEscape + logItem.EventTypeInfo.TypeString + logItem.EventTypeInfo.DefaultConsoleColourEscape;
            Console.WriteLine(logItem.EventTime.ToString("HH:mm:ss") + ": " + eventTypeString + ": " + logItem.Process + " - " + logItem.Message);

            return true;
        }

        /// <inheritdoc/>
        public async Task<bool> RunMaintenance()
        {
            // this is a console log provider, so no maintenance is needed
            return true;
        }

        /// <inheritdoc/>
        public async Task<Logging.LogItem?> GetLogMessageById(string id)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public async Task<List<Logging.LogItem>> GetLogMessages(Logging.LogsViewModel model)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public void Shutdown()
        {
            // No resources to clean up for console logging
        }
    }
}