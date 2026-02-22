using System.Diagnostics;

namespace gaseous_server.Classes.Plugins.LogProviders
{
    /// <summary>
    /// Provides logging functionality using the Windows Event Log.
    /// </summary>
    public class WindowsEventLogProvider : ILogProvider
    {
        private const string WindowsEventLogSource = "GaseousServer";
        private const string WindowsEventLogName = "Application";

        private static readonly object _initLock = new();
        private static volatile bool _sourceInitialized;
        private static EventLog? _eventLog;

        private static readonly Newtonsoft.Json.JsonSerializerSettings _jsonSettings = new()
        {
            NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
            Formatting = Newtonsoft.Json.Formatting.None
        };

        /// <inheritdoc/>
        public string Name => "Windows Event Log Provider";

        /// <inheritdoc/>
        public List<gaseous_server.Classes.Plugins.PluginManagement.OperatingSystems> SupportedOperatingSystems { get; } = new List<gaseous_server.Classes.Plugins.PluginManagement.OperatingSystems>
        {
            gaseous_server.Classes.Plugins.PluginManagement.OperatingSystems.Windows
        };

        /// <inheritdoc/>
        public bool SupportsLogFetch => false;

        /// <inheritdoc/>
        public Dictionary<string, object>? Settings { get; set; }

        private static void EnsureEventLogInitialized()
        {
            if (_sourceInitialized)
            {
                return;
            }

            lock (_initLock)
            {
                if (_sourceInitialized)
                {
                    return;
                }

#pragma warning disable CA1416
                try
                {
                    // Try to ensure the source exists. This may require elevation; ignore failures.
                    if (!EventLog.SourceExists(WindowsEventLogSource))
                    {
                        var data = new EventSourceCreationData(WindowsEventLogSource, WindowsEventLogName);
                        EventLog.CreateEventSource(data);
                    }

                    // Create and cache the EventLog instance
                    _eventLog = new EventLog(WindowsEventLogName)
                    {
                        Source = WindowsEventLogSource
                    };
                }
                catch
                {
                    // Ignore initialization errors - WriteEntry will fall back to default behavior
                }
                finally
                {
                    _sourceInitialized = true;
                }
#pragma warning restore CA1416
            }
        }

        /// <inheritdoc/>
        public async Task<bool> LogMessage(Logging.LogItem logItem)
        {
            return await LogMessage(logItem, null);
        }

        /// <inheritdoc/>
        public async Task<bool> LogMessage(Logging.LogItem logItem, Exception? exception)
        {
            // check if we're on Windows - if not fail gracefully
            if (!OperatingSystem.IsWindows())
            {
                return false;
            }

            // Run EventLog operations on thread pool to avoid blocking
            return await Task.Run(() =>
            {
#pragma warning disable CA1416
                try
                {
                    EnsureEventLogInitialized();

                    EventLogEntryType entryType = logItem.EventType switch
                    {
                        Logging.LogType.Warning => EventLogEntryType.Warning,
                        Logging.LogType.Critical => EventLogEntryType.Error,
                        _ => EventLogEntryType.Information
                    };

                    string sanitizedOutput = Newtonsoft.Json.JsonConvert.SerializeObject(logItem, _jsonSettings);

                    // Use cached EventLog instance if available, otherwise use static method
                    if (_eventLog != null)
                    {
                        _eventLog.WriteEntry(sanitizedOutput, entryType);
                    }
                    else
                    {
                        EventLog.WriteEntry(WindowsEventLogSource, sanitizedOutput, entryType);
                    }

                    return true;
                }
                catch
                {
                    // Silently fail if EventLog write fails
                    return false;
                }
#pragma warning restore CA1416
            }).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<bool> RunMaintenance()
        {
            // Windows Event Log does not require maintenance in this context.
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
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public void Shutdown()
        {
#pragma warning disable CA1416
            lock (_initLock)
            {
                if (_eventLog != null)
                {
                    try
                    {
                        _eventLog.Dispose();
                    }
                    catch
                    {
                        // Ignore disposal errors
                    }
                    finally
                    {
                        _eventLog = null;
                        _sourceInitialized = false;
                    }
                }
            }
#pragma warning restore CA1416
        }
    }
}