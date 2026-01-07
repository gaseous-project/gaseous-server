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
            try
            {
                return await LogMessage(logItem, null);
            }
            catch
            {
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> LogMessage(Logging.LogItem logItem, Exception? exception = null)
        {
            try
            {
                var logType = logItem.EventType ?? Logging.LogType.Information;
                Console.ForegroundColor = Logging.LogItem.LogTypeToColor[logType];
                Console.WriteLine(logItem.EventTime.ToString("yyyyMMdd HHmmss") + ": " + Logging.LogItem.LogTypeToString[logType] + ": " + logItem.Process + " - " + logItem.Message);
                Console.ResetColor();

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> RunMaintenance()
        {
            // this is a console log provider, so no maintenance is needed
            return true;
        }

        /// <inheritdoc/>
        public Task<Logging.LogItem> GetLogMessageById(string id)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public Task<List<Logging.LogItem>> GetLogMessages(Logging.LogsViewModel model)
        {
            throw new NotSupportedException();
        }
    }
}