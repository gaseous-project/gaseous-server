using System;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
namespace gaseous_server.Classes
{
    public class Logging
    {
        private static DateTime lastDiskRetentionSweep = DateTime.UtcNow;
        public static bool WriteToDiskOnly { get; set; } = false;
        private const string WindowsEventLogSource = "GaseousServer";
        private const string WindowsEventLogName = "Application";

        // Determines if the process is running as a Windows Service
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

        static public void Log(LogType EventType, string ServerProcess, string Message, Exception? ExceptionValue = null, bool LogToDiskOnly = false)
        {
            LogItem logItem = new LogItem
            {
                EventTime = DateTime.UtcNow,
                EventType = EventType,
                Process = ServerProcess,
                Message = Message,
                ExceptionValue = Common.ReturnValueIfNull(ExceptionValue, "").ToString()
            };

            _ = Task.Run(() => WriteLogAsync(logItem, LogToDiskOnly));
        }

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
                string TraceOutput = logItem.EventTime.ToString("yyyyMMdd HHmmss") + ": " + logItem.EventType.ToString() + ": " + logItem.Process + ": " + logItem.Message;
                if (logItem.ExceptionValue != null)
                {
                    TraceOutput += Environment.NewLine + logItem.ExceptionValue.ToString();
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

                    string correlationId;
                    try
                    {
                        if (CallContext.GetData("CorrelationId").ToString() == null)
                        {
                            correlationId = "";
                        }
                        else
                        {
                            correlationId = CallContext.GetData("CorrelationId").ToString();
                        }
                    }
                    catch
                    {
                        correlationId = "";
                    }

                    string callingProcess;
                    try
                    {
                        if (CallContext.GetData("CallingProcess").ToString() == null)
                        {
                            callingProcess = "";
                        }
                        else
                        {
                            callingProcess = CallContext.GetData("CallingProcess").ToString();
                        }
                    }
                    catch
                    {
                        callingProcess = "";
                    }

                    string callingUser;
                    try
                    {
                        if (CallContext.GetData("CallingUser").ToString() == null)
                        {
                            callingUser = "";
                        }
                        else
                        {
                            callingUser = CallContext.GetData("CallingUser").ToString();
                        }
                    }
                    catch
                    {
                        callingUser = "";
                    }

                    Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
                    string sql = "INSERT INTO ServerLogs (EventTime, EventType, Process, Message, Exception, CorrelationId, CallingProcess, CallingUser) VALUES (@EventTime, @EventType, @Process, @Message, @Exception, @correlationid, @callingprocess, @callinguser);";
                    Dictionary<string, object> dbDict = new Dictionary<string, object>();
                    dbDict.Add("EventTime", logItem.EventTime);
                    dbDict.Add("EventType", logItem.EventType);
                    dbDict.Add("Process", logItem.Process);
                    dbDict.Add("Message", logItem.Message);
                    dbDict.Add("Exception", Common.ReturnValueIfNull(logItem.ExceptionValue, "").ToString());
                    dbDict.Add("correlationid", correlationId);
                    dbDict.Add("callingprocess", callingProcess);
                    dbDict.Add("callinguser", callingUser);

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

        static public List<LogItem> GetLogs(LogsViewModel model)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            dbDict.Add("StartIndex", model.StartIndex);
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

                sql = "SELECT ServerLogs.Id, ServerLogs.EventTime, ServerLogs.EventType, ServerLogs.`Process`, ServerLogs.Message, ServerLogs.Exception, ServerLogs.CorrelationId, ServerLogs.CallingProcess, Users.Email FROM ServerLogs LEFT JOIN Users ON ServerLogs.CallingUser = Users.Id " + whereClause + " ORDER BY ServerLogs.Id DESC LIMIT @PageSize OFFSET @PageNumber;";
            }
            else
            {
                if (whereClause.Length > 0)
                {
                    whereClause = "AND " + whereClause;
                }

                sql = "SELECT ServerLogs.Id, ServerLogs.EventTime, ServerLogs.EventType, ServerLogs.`Process`, ServerLogs.Message, ServerLogs.Exception, ServerLogs.CorrelationId, ServerLogs.CallingProcess, Users.Email FROM ServerLogs LEFT JOIN Users ON ServerLogs.CallingUser = Users.Id  WHERE ServerLogs.Id < @StartIndex " + whereClause + " ORDER BY ServerLogs.Id DESC LIMIT @PageSize OFFSET @PageNumber;";
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

                EventLog.WriteEntry(WindowsEventLogSource, traceOutput, entryType);
#pragma warning restore CA1416
            }
            catch (Exception ex)
            {
                // Fall back to disk if writing to Event Log fails (e.g., insufficient rights)
                LogToDisk(logItem, traceOutput, ex);
            }
        }

        public enum LogType
        {
            Information = 0,
            Debug = 1,
            Warning = 2,
            Critical = 3
        }

        public class LogItem
        {
            public long Id { get; set; }
            public DateTime EventTime { get; set; }
            public LogType? EventType { get; set; }
            public string Process { get; set; } = "";
            public string CorrelationId { get; set; } = "";
            public string? CallingProcess { get; set; } = "";
            public string? CallingUser { get; set; } = "";
            private string _Message = "";
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
            public string? ExceptionValue { get; set; }
        }

        public class LogsViewModel
        {
            public long? StartIndex { get; set; }
            public int PageNumber { get; set; } = 1;
            public int PageSize { get; set; } = 100;
            public List<LogType> Status { get; set; } = new List<LogType>();
            public DateTime? StartDateTime { get; set; }
            public DateTime? EndDateTime { get; set; }
            public string? SearchText { get; set; }
            public string? CorrelationId { get; set; }
            public string? CallingProcess { get; set; }
            public string? CallingUser { get; set; }
        }
    }
}

