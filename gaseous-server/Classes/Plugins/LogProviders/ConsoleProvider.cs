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

            Console.ForegroundColor = logItem.EventTypeInfo.Colour;
            Console.WriteLine(logItem.EventTime.ToString("yyyyMMdd HHmmss") + ": " + logItem.EventTypeInfo.TypeString + ": " + logItem.Process + " - " + logItem.Message);
            Console.ResetColor();

            return true;
        }

        /// <inheritdoc/>
        public async Task<bool> RunMaintenance()
        {
            // this is a console log provider, so no maintenance is needed
            return true;
        }

        /// <inheritdoc/>
        public async Task<Logging.LogItem> GetLogMessageById(string id)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public async Task<List<Logging.LogItem>> GetLogMessages(Logging.LogsViewModel model)
        {
            throw new NotSupportedException();
        }
    }
}