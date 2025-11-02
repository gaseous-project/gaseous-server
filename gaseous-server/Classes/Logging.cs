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
namespace gaseous_server.Classes
{
    /// <summary>
    /// Provides logging utilities for the server including methods to emit log entries
    /// to console, Windows Event Log, a database table, and disk. Supports contextual
    /// enrichment and log retrieval with filtering.
    /// </summary>
    public class Logging
    {
        private static DateTime lastDiskRetentionSweep = DateTime.UtcNow;
        /// <summary>
        /// When set to true, all logging operations are forced to disk (bypassing DB and Event Log).
        /// </summary>
        public static bool WriteToDiskOnly { get; set; } = false;
        private const string WindowsEventLogSource = "GaseousServer";
        private const string WindowsEventLogName = "Application";

        /// <summary>
        /// Determines if the current process is running as a Windows Service. Returns false on non-Windows platforms
        /// or when detection fails. Used to decide between console output and Windows Event Log writes.
        /// </summary>
        private static bool IsRunningAsWindowsService()
        {
            if (!OperatingSystem.IsWindows())
                return false;

            try
            {
                // Only available on Windows; analyzer suppression as we guard at runtime
#pragma warning disable CA1416
                return Microsoft.Extensions.Hosting.WindowsServices.WindowsServiceHelpers.IsWindowsService();
#pragma warning restore CA1416
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Primary logging entry point. Queues a log item for asynchronous processing.
        /// </summary>
        /// <param name="EventType">Severity / classification of the log entry.</param>
        /// <param name="ServerProcess">Logical component or process emitting the log.</param>
        /// <param name="Message">Human readable message text.</param>
        /// <param name="ExceptionValue">Optional exception whose details are recorded.</param>
        /// <param name="LogToDiskOnly">If true, bypass non-disk sinks even if enabled globally.</param>
        /// <param name="AdditionalData">Optional structured key/value metadata captured with the log.</param>
        static public void Log(LogType EventType, string ServerProcess, string Message, Exception? ExceptionValue = null, bool LogToDiskOnly = false, Dictionary<string, object>? AdditionalData = null)
        {
            LogItem logItem = new LogItem
            {
                EventTime = DateTime.UtcNow,
                EventType = EventType,
                Process = ServerProcess,
                Message = Message,
                AdditionalData = AdditionalData ?? new Dictionary<string, object>(),
                ExceptionValue = Common.ReturnValueIfNull(ExceptionValue, "").ToString()
            };

            _ = Task.Run(() => WriteLogAsync(logItem, LogToDiskOnly));
        }

        /// <summary>
        /// Handles the actual persistence / output of the log item to the configured sinks.
        /// Applies filtering (e.g., debug on/off), formats console output, writes to DB, Windows Event Log,
        /// and disk as needed. Performs periodic disk retention cleanup.
        /// </summary>
        /// <param name="logItem">The populated log item to write.</param>
        /// <param name="LogToDiskOnly">Overrides sink selection to force disk-only behavior.</param>
        static async Task WriteLogAsync(LogItem logItem, bool LogToDiskOnly)
        {
            bool AllowWrite = false;
            if (logItem.EventType == LogType.Debug)
            {
                if (Config.LoggingConfiguration.DebugLogging == true)
                {
                    AllowWrite = true;
                }
            }
            else
            {
                AllowWrite = true;
            }

            if (AllowWrite == true)
            {
                // console output
                // Base trace line includes timestamp, type, process, and message.
                string TraceOutput = logItem.EventTime.ToString("yyyyMMdd HHmmss") + ": " + logItem.EventType.ToString() + ": " + logItem.Process + ": " + logItem.Message;
                if (logItem.AdditionalData.Count > 0)
                {
                    TraceOutput += Environment.NewLine + "Additional Data:";
                    foreach (var kvp in logItem.AdditionalData)
                    {
                        TraceOutput += Environment.NewLine + " - " + kvp.Key + ": " + kvp.Value.ToString();
                    }
                }
                if (logItem.ExceptionValue != null)
                {
                    TraceOutput += Environment.NewLine + "Exception: " + logItem.ExceptionValue.ToString();
                }
                if (IsRunningAsWindowsService())
                {
                    WriteToWindowsEventLog(logItem, TraceOutput);
                }
                else
                {
                    // Colorize only for console output
                    string consoleColour = "";
                    switch (logItem.EventType)
                    {
                        case LogType.Information:
                            consoleColour = "\u001b[1;34m]";
                            break;
                        case LogType.Warning:
                            consoleColour = "\u001b[1;33m]";
                            break;
                        case LogType.Critical:
                            consoleColour = "\u001b[1;31m]";
                            break;
                        case LogType.Debug:
                            consoleColour = "\u001b[1;36m]";
                            break;
                    }
                    Console.WriteLine(consoleColour + TraceOutput);
                }
                // Console.ResetColor();

                if (WriteToDiskOnly == true)
                {
                    LogToDiskOnly = true;
                }

                if (LogToDiskOnly == false)
                {
                    if (Config.LoggingConfiguration.AlwaysLogToDisk == true)
                    {
                        LogToDisk(logItem, TraceOutput, null);
                    }

                    // Pull ambient context values if they have been set for correlation / tracing.
                    string correlationId = "";
                    try
                    {
                        var ctxCorrelation = CallContext.GetData("CorrelationId");
                        if (ctxCorrelation == null)
                        {
                            correlationId = "";
                        }
                        else
                        {
                            correlationId = ctxCorrelation.ToString() ?? "";
                        }
                    }
                    catch
                    {
                        correlationId = "";
                    }

                    string callingProcess = "";
                    try
                    {
                        var ctxCallingProcess = CallContext.GetData("CallingProcess");
                        if (ctxCallingProcess == null)
                        {
                            callingProcess = "";
                        }
                        else
                        {
                            callingProcess = ctxCallingProcess.ToString() ?? "";
                        }
                    }
                    catch
                    {
                        callingProcess = "";
                    }

                    string callingUser = "";
                    try
                    {
                        var ctxCallingUser = CallContext.GetData("CallingUser");
                        if (ctxCallingUser == null)
                        {
                            callingUser = "";
                        }
                        else
                        {
                            callingUser = ctxCallingUser.ToString() ?? "";
                        }
                    }
                    catch
                    {
                        callingUser = "";
                    }

                    // Persist to database
                    Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
                    string sql = "INSERT INTO ServerLogs (EventTime, EventType, Process, Message, AdditionalData, Exception, CorrelationId, CallingProcess, CallingUser) VALUES (@EventTime, @EventType, @Process, @Message, @AdditionalData, @Exception, @correlationid, @callingprocess, @callinguser);";
                    Dictionary<string, object> dbDict = new Dictionary<string, object>();
                    dbDict.Add("EventTime", logItem.EventTime);
                    dbDict.Add("EventType", (int)(logItem.EventType ?? LogType.Information));
                    dbDict.Add("Process", logItem.Process);
                    dbDict.Add("Message", logItem.Message);
                    dbDict.Add("AdditionalData", Newtonsoft.Json.JsonConvert.SerializeObject(logItem.AdditionalData));
                    dbDict.Add("Exception", Common.ReturnValueIfNull(logItem.ExceptionValue, "").ToString() ?? "");
                    dbDict.Add("correlationid", correlationId ?? "");
                    dbDict.Add("callingprocess", callingProcess ?? "");
                    dbDict.Add("callinguser", callingUser ?? "");

                    try
                    {
                        await db.ExecuteCMDAsync(sql, dbDict);
                    }
                    catch (Exception ex)
                    {
                        LogToDisk(logItem, TraceOutput, ex);
                    }
                }
                else
                {
                    LogToDisk(logItem, TraceOutput, null);
                }
            }

            // Periodic retention sweep (once per hour) removes old log files beyond configured retention days.
            if (lastDiskRetentionSweep.AddMinutes(60) < DateTime.UtcNow)
            {
                // time to delete any old logs
                lastDiskRetentionSweep = DateTime.UtcNow;
                string[] files = Directory.GetFiles(Config.LogPath);

                foreach (string file in files)
                {
                    FileInfo fi = new FileInfo(file);
                    if (fi.LastAccessTime < DateTime.Now.AddDays(Config.LoggingConfiguration.LogRetention * -1))
                    {
                        fi.Delete();
                    }
                }
            }
        }

        /// <summary>
        /// Appends the log entry (and optionally exception details) to the configured log file path.
        /// Used either as a primary sink (disk-only mode) or fallback when other sinks fail.
        /// </summary>
        /// <param name="logItem">The log item being recorded.</param>
        /// <param name="TraceOutput">Preformatted string representation of the log.</param>
        /// <param name="exception">Optional exception that triggered fallback or accompanies the log.</param>
        static void LogToDisk(LogItem logItem, string TraceOutput, Exception? exception)
        {
            if (exception != null)
            {
                // dump the error
                File.AppendAllText(Config.LogFilePath, logItem.EventTime.ToString("yyyyMMdd HHmmss") + ": " + logItem.EventType.ToString() + ": " + logItem.Process + ": " + logItem.Message + Environment.NewLine + exception.ToString());


                // something went wrong writing to the db
                File.AppendAllText(Config.LogFilePath, logItem.EventTime.ToString("yyyyMMdd HHmmss") + ": The following event was unable to be written to the log database:");
            }

            File.AppendAllText(Config.LogFilePath, TraceOutput);
        }

        /// <summary>
        /// Retrieves log entries from the database applying pagination and optional filtering criteria
        /// (event types, time range, full-text search on message, correlation/user/process constraints).
        /// </summary>
        /// <param name="model">Query model specifying filters and paging.</param>
        /// <returns>List of matching <see cref="LogItem"/> instances.</returns>
        static public List<LogItem> GetLogs(LogsViewModel model)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            if (model.StartIndex.HasValue)
            {
                dbDict.Add("StartIndex", model.StartIndex.Value);
            }
            dbDict.Add("PageNumber", (model.PageNumber - 1) * model.PageSize);
            dbDict.Add("PageSize", model.PageSize);
            string sql = "";

            List<string> whereClauses = new List<string>();

            // handle status criteria
            if (model.Status != null)
            {
                if (model.Status.Count > 0)
                {
                    List<string> statusWhere = new List<string>();
                    for (int i = 0; i < model.Status.Count; i++)
                    {
                        string valueName = "@eventtype" + i;
                        statusWhere.Add(valueName);
                        dbDict.Add(valueName, (int)model.Status[i]);
                    }

                    whereClauses.Add("EventType IN (" + string.Join(",", statusWhere) + ")");
                }
            }

            // handle start date criteria
            if (model.StartDateTime != null)
            {
                dbDict.Add("startdate", model.StartDateTime);
                whereClauses.Add("EventTime >= @startdate");
            }

            // handle end date criteria
            if (model.EndDateTime != null)
            {
                dbDict.Add("enddate", model.EndDateTime);
                whereClauses.Add("EventTime <= @enddate");
            }

            // handle search text criteria
            if (model.SearchText != null)
            {
                if (model.SearchText.Length > 0)
                {
                    dbDict.Add("messageSearch", model.SearchText);
                    whereClauses.Add("MATCH(Message) AGAINST (@messageSearch)");
                }
            }

            if (model.CorrelationId != null)
            {
                if (model.CorrelationId.Length > 0)
                {
                    dbDict.Add("correlationId", model.CorrelationId);
                    whereClauses.Add("CorrelationId = @correlationId");
                }
            }

            if (model.CallingProcess != null)
            {
                if (model.CallingProcess.Length > 0)
                {
                    dbDict.Add("callingProcess", model.CallingProcess);
                    whereClauses.Add("CallingProcess = @callingProcess");
                }
            }

            if (model.CallingUser != null)
            {
                if (model.CallingUser.Length > 0)
                {
                    dbDict.Add("callingUser", model.CallingUser);
                    whereClauses.Add("CallingUser = @callingUser");
                }
            }

            // compile WHERE clause
            string whereClause = "";
            if (whereClauses.Count > 0)
            {
                whereClause = "(" + String.Join(" AND ", whereClauses) + ")";
            }

            // execute query
            if (model.StartIndex == null)
            {
                if (whereClause.Length > 0)
                {
                    whereClause = "WHERE " + whereClause;
                }

                sql = "SELECT ServerLogs.Id, ServerLogs.EventTime, ServerLogs.EventType, ServerLogs.`Process`, ServerLogs.Message, ServerLogs.AdditionalData, ServerLogs.Exception, ServerLogs.CorrelationId, ServerLogs.CallingProcess, Users.Email FROM ServerLogs LEFT JOIN Users ON ServerLogs.CallingUser = Users.Id " + whereClause + " ORDER BY ServerLogs.Id DESC LIMIT @PageSize OFFSET @PageNumber;";
            }
            else
            {
                if (whereClause.Length > 0)
                {
                    whereClause = "AND " + whereClause;
                }

                sql = "SELECT ServerLogs.Id, ServerLogs.EventTime, ServerLogs.EventType, ServerLogs.`Process`, ServerLogs.Message, ServerLogs.AdditionalData, ServerLogs.Exception, ServerLogs.CorrelationId, ServerLogs.CallingProcess, Users.Email FROM ServerLogs LEFT JOIN Users ON ServerLogs.CallingUser = Users.Id  WHERE ServerLogs.Id < @StartIndex " + whereClause + " ORDER BY ServerLogs.Id DESC LIMIT @PageSize OFFSET @PageNumber;";
            }
            DataTable dataTable = db.ExecuteCMD(sql, dbDict);

            List<LogItem> logs = new List<LogItem>();
            foreach (DataRow row in dataTable.Rows)
            {
                LogItem log = new LogItem
                {
                    Id = (long)row["Id"],
                    EventTime = DateTime.Parse(((DateTime)row["EventTime"]).ToString("yyyy-MM-ddThh:mm:ss") + 'Z'),
                    EventType = (LogType)row["EventType"],
                    Process = (string)row["Process"],
                    Message = (string)row["Message"],
                    AdditionalData = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>((string)Common.ReturnValueIfNull(row["AdditionalData"], "{}")) ?? new Dictionary<string, object>(),
                    ExceptionValue = (string)row["Exception"],
                    CorrelationId = (string)Common.ReturnValueIfNull(row["CorrelationId"], ""),
                    CallingProcess = (string)Common.ReturnValueIfNull(row["CallingProcess"], ""),
                    CallingUser = (string)Common.ReturnValueIfNull(row["Email"], "")
                };

                logs.Add(log);
            }

            return logs;
        }

        // Writes to Windows Event Log when running as a Windows Service. No-ops on other platforms.
        /// <summary>
        /// Writes the log entry to the Windows Event Log when running as a service on Windows OS. Falls back to disk on failure.
        /// On non-Windows platforms, simply writes the trace output to the console.
        /// </summary>
        /// <param name="logItem">The log item to serialize and record.</param>
        /// <param name="traceOutput">Console-friendly representation of the log (used for non-Windows fallback).</param>
        private static void WriteToWindowsEventLog(LogItem logItem, string traceOutput)
        {
            if (!OperatingSystem.IsWindows())
            {
                Console.WriteLine(traceOutput);
                return;
            }

            try
            {
#pragma warning disable CA1416
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

                var entryType = EventLogEntryType.Information;
                switch (logItem.EventType)
                {
                    case LogType.Warning:
                        entryType = EventLogEntryType.Warning;
                        break;
                    case LogType.Critical:
                        entryType = EventLogEntryType.Error;
                        break;
                    case LogType.Debug:
                        entryType = EventLogEntryType.Information;
                        break;
                    case LogType.Information:
                    default:
                        entryType = EventLogEntryType.Information;
                        break;
                }

                string sanitizedOutput = Newtonsoft.Json.JsonConvert.SerializeObject(logItem);

                EventLog.WriteEntry(WindowsEventLogSource, sanitizedOutput, entryType);
#pragma warning restore CA1416
            }
            catch (Exception ex)
            {
                // Fall back to disk if writing to Event Log fails (e.g., insufficient rights)
                LogToDisk(logItem, traceOutput, ex);
            }
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
            private string _Message = "";
            /// <summary>
            /// Message body of the log entry. Set-only wrapper allows future validation / transformation.
            /// </summary>
            public string Message
            {
                get
                {
                    return _Message;
                }
                set
                {
                    _Message = value;
                }
            }
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

