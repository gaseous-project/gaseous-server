
using System.Data;

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
            var logOutput = Newtonsoft.Json.JsonConvert.SerializeObject(logItem, Newtonsoft.Json.Formatting.Indented);
            await File.AppendAllTextAsync(Config.LogFilePath, logOutput + Environment.NewLine);

            return true;
        }

        /// <inheritdoc/>
        public async Task<bool> RunMaintenance()
        {
            // delete all log files older than the configured log retention period - minimum of 1 day of logs must be kept as the log file may be in use
            try
            {
                var logDirectory = Path.GetDirectoryName(Config.LogPath);

                if (logDirectory == null || !Directory.Exists(logDirectory))
                {
                    throw new DirectoryNotFoundException(logDirectory);
                }

                Logging.LogKey(Logging.LogType.Information, "process.maintenance", "maintenance.removing_files_older_than_days_from_path", null, new string[] { Config.LoggingConfiguration.LogRetention.ToString(), logDirectory });

                var logFiles = Directory.GetFiles(logDirectory);
                var retentionPeriod = TimeSpan.FromDays(Math.Max(Config.LoggingConfiguration.LogRetention, 1));
                foreach (var logFile in logFiles)
                {
                    var fileInfo = new FileInfo(logFile);
                    if (DateTime.UtcNow - fileInfo.LastAccessTime > retentionPeriod)
                    {
                        Logging.LogKey(Logging.LogType.Warning, "process.maintenance", "maintenance.deleting_file", null, new string[] { logFile });
                        File.Delete(logFile);
                    }
                }
            }
            catch (Exception ex)
            {
                // log the exception using the built-in logging system
                Logging.LogKey(Logging.LogType.Warning, "process.maintenance", "Error during TextFileProvider log maintenance: " + ex.Message, null, null, ex, false, null);
            }
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