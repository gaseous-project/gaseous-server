using System.Diagnostics;

namespace gaseous_server.Classes.Plugins.LogProviders
{
    /// <summary>
    /// Provides logging functionality using the Windows Event Log.
    /// </summary>
    public class WindowsEventLogProvider : ILogProvider
    {
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

#pragma warning disable CA1416

            const string WindowsEventLogSource = "GaseousServer";
            const string WindowsEventLogName = "Application";

            // Try to ensure the source exists. This may require elevation; ignore failures.
            try
            {
                if (!EventLog.SourceExists(WindowsEventLogSource))
                {
                    var data = new EventSourceCreationData(WindowsEventLogSource, WindowsEventLogName);
                    EventLog.CreateEventSource(data);
                }
            }
            catch { /* ignore source creation errors */ }

            EventLogEntryType entryType;
            switch (logItem.EventType)
            {
                case Logging.LogType.Warning:
                    entryType = EventLogEntryType.Warning;
                    break;
                case Logging.LogType.Critical:
                    entryType = EventLogEntryType.Error;
                    break;
                case Logging.LogType.Debug:
                    entryType = EventLogEntryType.Information;
                    break;
                default:
                    entryType = EventLogEntryType.Information;
                    break;
            }

            string sanitizedOutput = Newtonsoft.Json.JsonConvert.SerializeObject(logItem);

            EventLog.WriteEntry(WindowsEventLogSource, sanitizedOutput, entryType);
#pragma warning restore CA1416

            return true;
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
    }
}