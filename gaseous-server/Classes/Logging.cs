// ----------------------------------------------------------------------------
//  File: Logging.cs
//  Project: gaseous-server
//  Description:
//     Centralized logging utilities for the Gaseous server. Supports writing
//     to console (with ANSI colour when not running as a Windows Service),
//     Windows Event Log (when running as a service on Windows), relational
//     database persistence (MySQL), and disk-based fallback / retention.
//
//     Key features:
//       * Asynchronous log write to prevent blocking caller.
//       * Conditional debug logging based on configuration.
//       * Automatic correlation / context capture (CorrelationId, CallingUser,
//         CallingProcess) via CallContext when available.
//       * Disk retention sweep that purges aged log files based on configured
//         retention period.
//       * Graceful degradation: failures to write to DB or Event Log fall back
//         to disk storage.
//
//     NOTE: This class mixes infrastructure concerns (DB, filesystem, EventLog).
//           Future refactoring could separate providers (e.g., ILogSink) to
//           improve testability and single responsibility.
// ----------------------------------------------------------------------------
using System;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using gaseous_server.Classes.Plugins;
namespace gaseous_server.Classes
{
    /// <summary>
    /// Provides logging utilities for the server including methods to emit log entries
    /// to console, Windows Event Log, a database table, and disk. Supports contextual
    /// enrichment and log retrieval with filtering.
    /// </summary>
    public class Logging
    {
        /// <summary>
        /// When set to true, all logging operations are forced to disk (bypassing DB and Event Log).
        /// </summary>
        public static bool WriteToDiskOnly { get; set; } = false;

        /// <summary>
        /// Adds a log entry using localisation keys for process and message, translating internally.
        /// Prefer this overload for new code instead of performing Localisation.Translate at call sites.
        /// </summary>
        /// <param name="eventType">Severity / classification of the log entry.</param>
        /// <param name="processKey">Localisation key for the logical process/component (server strings).</param>
        /// <param name="messageKey">Localisation key for the log message (server strings).</param>
        /// <param name="processArgs">Optional formatting arguments for process translation.</param>
        /// <param name="messageArgs">Optional formatting arguments for message translation.</param>
        /// <param name="exceptionValue">Optional exception to record.</param>
        /// <param name="logToDiskOnly">If true, bypass non-disk sinks.</param>
        /// <param name="additionalData">Optional structured metadata.</param>
        public static void LogKey(LogType eventType, string processKey, string messageKey, string[]? processArgs = null, string[]? messageArgs = null, Exception? exceptionValue = null, bool logToDiskOnly = false, Dictionary<string, object>? additionalData = null)
        {
            string resolvedProcess = SafeTranslate(processKey, processArgs);
            string resolvedMessage = SafeTranslate(messageKey, messageArgs);

            LogItem logItem = new LogItem
            {
                EventTime = DateTime.UtcNow,
                EventType = eventType,
                Process = resolvedProcess,
                Message = resolvedMessage,
                AdditionalData = additionalData ?? new Dictionary<string, object>(),
                ExceptionValue = Common.ReturnValueIfNull(exceptionValue, "").ToString()
            };

#pragma warning disable CS0618
            _ = Task.Run(() => WriteLogAsync(logItem, logToDiskOnly));
#pragma warning restore CS0618
        }

        /// <summary>
        /// Safely translates a localisation key (server strings) with optional formatting arguments.
        /// Returns the key itself if translation fails or key not found.
        /// </summary>
        private static string SafeTranslate(string key, string[]? args)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(key)) return "";
                string value = Localisation.Translate(key, args, true);
                return value ?? key;
            }
            catch
            {
                return key; // fallback to key literal
            }
        }

        static List<gaseous_server.Classes.Plugins.LogProviders.ILogProvider> _logProviders = new List<Plugins.LogProviders.ILogProvider>();
        static List<gaseous_server.Classes.Plugins.LogProviders.ILogProvider> _logReaderProviders = new List<Plugins.LogProviders.ILogProvider>();

        /// <summary>
        /// Handles the actual persistence / output of the log item to the configured sinks.
        /// Applies filtering (e.g., debug on/off), formats console output, writes to DB, Windows Event Log,
        /// and disk as needed.
        /// </summary>
        /// <param name="logItem">The populated log item to write.</param>
        /// <param name="LogToDiskOnly">Overrides sink selection to force disk-only behavior.</param>
        static async Task WriteLogAsync(LogItem logItem, bool LogToDiskOnly)
        {
            if (logItem.EventType == LogType.Debug && !Config.LoggingConfiguration.DebugLogging)
            {
                return;
            }

            if (WriteToDiskOnly || LogToDiskOnly)
            {
                var diskLogProvider = new Plugins.LogProviders.TextFileProvider();
                _ = diskLogProvider.LogMessage(logItem, null);
            }
            else
            {
                // load all log providers into a static array
                // - classes that implement gaseous_server.Classes.Plugins.LogProviders.ILogProvider
                // - filter by supported operating systems
                if (_logProviders.Count == 0)
                {
                    // load all log providers
                    var assembly = Assembly.GetExecutingAssembly();
                    var pluginType = typeof(Plugins.LogProviders.ILogProvider);

                    var pluginTypes = assembly.GetTypes()
                        .Where(t => t.IsClass && !t.IsAbstract && pluginType.IsAssignableFrom(t))
                        .ToList();

                    foreach (var type in pluginTypes)
                    {
                        try
                        {
                            var plugin = Activator.CreateInstance(type) as Plugins.LogProviders.ILogProvider;
                            if (plugin != null && plugin.SupportedOperatingSystems.Contains(Plugins.PluginManagement.GetCurrentOperatingSystem()))
                            {
                                _logProviders.Add(plugin);

                                if (plugin.SupportsLogFetch)
                                {
                                    _logReaderProviders.Add(plugin);
                                }
                            }
                        }
                        catch
                        {
                            // log provider failed to load - write to disk log as fallback
                        }
                    }
                }

                // Pull ambient context values if they have been set for correlation / tracing.
                try
                {
                    var ctxCorrelation = CallContext.GetData("CorrelationId");
                    if (ctxCorrelation == null)
                    {
                        logItem.CorrelationId = "";
                    }
                    else
                    {
                        logItem.CorrelationId = ctxCorrelation.ToString() ?? "";
                    }
                }
                catch
                {
                    logItem.CorrelationId = "";
                }

                try
                {
                    var ctxCallingProcess = CallContext.GetData("CallingProcess");
                    if (ctxCallingProcess == null)
                    {
                        logItem.CallingProcess = "";
                    }
                    else
                    {
                        logItem.CallingProcess = ctxCallingProcess.ToString() ?? "";
                    }
                }
                catch
                {
                    logItem.CallingProcess = "";
                }

                try
                {
                    var ctxCallingUser = CallContext.GetData("CallingUser");
                    if (ctxCallingUser == null)
                    {
                        logItem.CallingUser = "";
                    }
                    else
                    {
                        logItem.CallingUser = ctxCallingUser.ToString() ?? "";
                    }
                }
                catch
                {
                    logItem.CallingUser = "";
                }

                // send log to each provider
                foreach (var provider in _logProviders)
                {
                    try
                    {
                        _ = provider.LogMessage(logItem, null);
                    }
                    catch
                    {
                        // log provider failed to log - write to disk log as fallback
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves log entries from the database applying pagination and optional filtering criteria
        /// (event types, time range, full-text search on message, correlation/user/process constraints).
        /// </summary>
        /// <param name="model">Query model specifying filters and paging.</param>
        /// <returns>List of matching <see cref="LogItem"/> instances.</returns>
        public static async Task<List<LogItem>> GetLogs(LogsViewModel model)
        {
            if (_logReaderProviders.Count == 0)
            {
                return new List<LogItem>();
            }

            var logItems = new List<LogItem>();
            foreach (var provider in _logProviders)
            {
                try
                {
                    var providerLogs = await provider.GetLogMessages(model);
                    logItems.AddRange(providerLogs);
                }
                catch
                {
                    // log provider failed to fetch logs - skip
                }
            }
            return logItems;
        }

        /// <summary>
        /// Classification / severity of a log entry.
        /// </summary>
        public enum LogType
        {
            /// <summary>
            /// Standard informational event representing normal application flow.
            /// </summary>
            Information = 0,
            /// <summary>
            /// Verbose diagnostic message intended to aid debugging; may be filtered.
            /// </summary>
            Debug = 1,
            /// <summary>
            /// Non-critical issue that may need attention but does not stop execution.
            /// </summary>
            Warning = 2,
            /// <summary>
            /// Critical failure or exception requiring immediate attention.
            /// </summary>
            Critical = 3
        }

        /// <summary>
        /// Represents a single log entry including metadata, message content, optional exception, and contextual identifiers.
        /// </summary>
        public class LogItem
        {
            /// <summary>
            /// Database identity / primary key (if sourced from persistence layer).
            /// </summary>
            public long Id { get; set; }

            /// <summary>
            /// UTC timestamp indicating when the event occurred / was captured.
            /// </summary>
            public DateTime EventTime { get; set; }

            /// <summary>
            /// Severity / classification of the log. Nullable to handle transitional deserialization scenarios.
            /// </summary>
            public LogType? EventType { get; set; }

            /// <summary>
            /// Provides detailed information about the event type, including its string representation and console color.
            /// </summary>
            [Newtonsoft.Json.JsonIgnore]
            [System.Text.Json.Serialization.JsonIgnore]
            public LogTypeInfo EventTypeInfo
            {
                get
                {
                    return new LogTypeInfo(EventType ?? LogType.Information);
                }
            }

            /// <summary>
            /// Contains information about the log type, including its severity, string representation, and console color.
            /// </summary>
            public class LogTypeInfo
            {
                /// <summary>
                /// Initializes a new instance of the <see cref="LogTypeInfo"/> class with the specified log type.
                /// </summary>
                /// <param name="type">The log type to associate with this info instance.</param>
                public LogTypeInfo(LogType type)
                {
                    this._type = type;
                }

                private LogType _type { get; set; }

                /// <summary>
                /// The type of log entry.
                /// </summary>
                public LogType Type { get { return this._type; } }

                /// <summary>
                /// String representation of the log type for easy logging/display.
                /// </summary>
                public string? TypeString
                {
                    get
                    {
                        switch (this._type)
                        {
                            case LogType.Information:
                                return "INFO";
                            case LogType.Warning:
                                return "WARN";
                            case LogType.Critical:
                                return "CRIT";
                            case LogType.Debug:
                                return "DBUG";
                            default:
                                return "INFO";
                        }
                    }
                }

                /// <summary>
                /// The console color associated with the log type for colorized output.
                /// </summary>
                public ConsoleColor Colour
                {
                    get
                    {
                        switch (this._type)
                        {
                            case LogType.Information:
                                return ConsoleColor.Blue;
                            case LogType.Warning:
                                return ConsoleColor.Yellow;
                            case LogType.Critical:
                                return ConsoleColor.Red;
                            case LogType.Debug:
                                return ConsoleColor.Gray;
                            default:
                                return ConsoleColor.Blue;
                        }
                    }
                }
            }

            /// <summary>
            /// Logical process or component emitting the log (e.g., controller name, worker identifier).
            /// </summary>
            public string Process { get; set; } = "";

            /// <summary>
            /// Ambient correlation identifier used to link related operations across components.
            /// </summary>
            public string CorrelationId { get; set; } = "";

            /// <summary>
            /// Name of the calling process (if supplied via context) for cross-system traceability.
            /// </summary>
            public string? CallingProcess { get; set; } = "";

            /// <summary>
            /// Identifier / email of the user associated with the log (if available).
            /// </summary>
            public string? CallingUser { get; set; } = "";

            /// <summary>
            /// Message body of the log entry. Set-only wrapper allows future validation / transformation.
            /// </summary>
            public string Message { get; set; } = "";

            /// <summary>
            /// Raw exception details captured for the event, usually stack trace and message.
            /// </summary>
            public string? ExceptionValue { get; set; } = "";

            /// <summary>
            /// Arbitrary additional structured data associated with the log (serialized when persisted).
            /// </summary>
            public Dictionary<string, object> AdditionalData { get; set; } = new Dictionary<string, object>();
        }

        /// <summary>
        /// Query model for retrieving logs with paging and filter criteria.
        /// </summary>
        public class LogsViewModel
        {
            /// <summary>
            /// Upper bound (exclusive) on log Id for incremental pagination (e.g., infinite scroll).
            /// </summary>
            public long? StartIndex { get; set; }
            /// <summary>
            /// 1-based page index.
            /// </summary>
            public int PageNumber { get; set; } = 1;
            /// <summary>
            /// Number of records to return per page.
            /// </summary>
            public int PageSize { get; set; } = 100;
            /// <summary>
            /// Set of log types to include; empty set means all types.
            /// </summary>
            public List<LogType> Status { get; set; } = new List<LogType>();
            /// <summary>
            /// Optional inclusive start of time range filter.
            /// </summary>
            public DateTime? StartDateTime { get; set; }
            /// <summary>
            /// Optional inclusive end of time range filter.
            /// </summary>
            public DateTime? EndDateTime { get; set; }
            /// <summary>
            /// Full-text search term applied to message field.
            /// </summary>
            public string? SearchText { get; set; }
            /// <summary>
            /// Filter by exact correlation identifier.
            /// </summary>
            public string? CorrelationId { get; set; }
            /// <summary>
            /// Filter by calling process.
            /// </summary>
            public string? CallingProcess { get; set; }
            /// <summary>
            /// Filter by calling user (email/identifier).
            /// </summary>
            public string? CallingUser { get; set; }
        }
    }
}

