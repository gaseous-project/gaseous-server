
using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace gaseous_server.Classes.Plugins.LogProviders
{
    /// <summary>
    /// Provides logging functionality to text files.
    /// </summary>
    public class TextFileProvider : ILogProvider
    {
        private static readonly SemaphoreSlim _writeLock = new(1, 1);
        private static StreamWriter? _writer;
        private static PeriodicTimer? _flushTimer;
        private static Task? _flushLoop;
        private static CancellationTokenSource? _flushCts;
        private static volatile bool _flushStarted;

        private static void EnsureWriterInitialized()
        {
            if (_writer != null)
            {
                return;
            }

            var logDir = Path.GetDirectoryName(Config.LogFilePath);
            if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }

            var fs = new FileStream(
                Config.LogFilePath,
                FileMode.Append,
                FileAccess.Write,
                FileShare.Read,
                bufferSize: 64 * 1024,
                options: FileOptions.Asynchronous);

            _writer = new StreamWriter(fs)
            {
                AutoFlush = false
            };

            // Start periodic flush loop once
            if (!_flushStarted)
            {
                _flushStarted = true;
                _flushTimer = new PeriodicTimer(TimeSpan.FromSeconds(5));
                _flushCts = new CancellationTokenSource();
                _flushLoop = Task.Run(async () =>
                {
                    try
                    {
                        while (await _flushTimer!.WaitForNextTickAsync(_flushCts.Token).ConfigureAwait(false))
                        {
                            await _writeLock.WaitAsync().ConfigureAwait(false);
                            try
                            {
                                if (_writer != null)
                                {
                                    await _writer.FlushAsync().ConfigureAwait(false);
                                }
                            }
                            finally
                            {
                                _writeLock.Release();
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // expected when shutting down
                    }
                }, _flushCts.Token);
            }
        }
        /// <inheritdoc/>
        public string Name => "Text File Log Provider";

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
            Newtonsoft.Json.JsonSerializerSettings jsonSettings = new()
            {
                NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                Formatting = Newtonsoft.Json.Formatting.None
            };
            jsonSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());

            var logOutput = Newtonsoft.Json.JsonConvert.SerializeObject(logItem, jsonSettings);

            await _writeLock.WaitAsync().ConfigureAwait(false);
            try
            {
                EnsureWriterInitialized();
                await _writer!.WriteLineAsync(logOutput).ConfigureAwait(false);
            }
            finally
            {
                _writeLock.Release();
            }

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
            // Cancel the periodic flush loop
            _flushCts?.Cancel();

            // Wait for flush loop to complete (with timeout)
            try
            {
                _flushLoop?.Wait(TimeSpan.FromSeconds(5));
            }
            catch (AggregateException)
            {
                // Ignore cancellation exceptions during shutdown
            }

            // Acquire lock and flush/dispose writer
            _writeLock.Wait();
            try
            {
                if (_writer != null)
                {
                    _writer.Flush();
                    _writer.Dispose();
                    _writer = null;
                }
            }
            finally
            {
                _writeLock.Release();
            }

            // Dispose timer and cancellation token source
            _flushTimer?.Dispose();
            _flushTimer = null;
            _flushCts?.Dispose();
            _flushCts = null;
            _flushStarted = false;
        }
    }
}