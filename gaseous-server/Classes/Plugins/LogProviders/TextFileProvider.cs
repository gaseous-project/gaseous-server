
namespace gaseous_server.Classes.Plugins.LogProviders
{
    /// <summary>
    /// Provides logging functionality to text files.
    /// </summary>
    public class TextFileProvider : ILogProvider
    {
        /// <inheritdoc/>
        public string Name => "Text File Log Provider";

        /// <inheritdoc/>
        public bool SupportsLogFetch => false;

        private static DateTime lastDiskFlushTime = DateTime.MinValue;

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
                File.AppendAllText(Config.LogFilePath, logItem.EventTime.ToString("yyyyMMdd HHmmss") + ": " + Logging.LogItem.LogTypeToString[logType] + ": " + logItem.Process + " - " + logItem.Message + Environment.NewLine);

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
            // delete all log files older than the configured log retention period - minimum of 1 day of logs must be kept as the log file may be in use
            try
            {
                var logDirectory = Path.GetDirectoryName(Config.LogPath);
                if (logDirectory != null && Directory.Exists(logDirectory))
                {
                    var logFiles = Directory.GetFiles(logDirectory);
                    var retentionPeriod = TimeSpan.FromDays(Math.Max(Config.LoggingConfiguration.LogRetention, 1));
                    foreach (var logFile in logFiles)
                    {
                        var fileInfo = new FileInfo(logFile);
                        if (DateTime.Now - fileInfo.LastAccessTime > retentionPeriod)
                        {
                            File.Delete(logFile);
                        }
                    }
                }
            }
            catch
            { }
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