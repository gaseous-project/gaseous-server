using System;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
namespace gaseous_server.Classes
{
	public class Logging
	{
        public static bool WriteToDiskOnly { get; set; } = false;

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

            bool AllowWrite = false;
            if (EventType == LogType.Debug)
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
                switch(logItem.EventType) {
                    case LogType.Information:
                        Console.ForegroundColor = ConsoleColor.Blue;
                        break;

                    case LogType.Warning:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;

                    case LogType.Critical:
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;

                    case LogType.Debug:
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        break;

                }
                Console.WriteLine(TraceOutput);
                Console.ResetColor();

                if (WriteToDiskOnly == true)
                {
                    LogToDiskOnly = true;
                }

                if (LogToDiskOnly == false)
                {
                    Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
                    string sql = "DELETE FROM ServerLogs WHERE EventTime < @EventRententionDate; INSERT INTO ServerLogs (EventTime, EventType, Process, Message, Exception) VALUES (@EventTime, @EventType, @Process, @Message, @Exception);";
                    Dictionary<string, object> dbDict = new Dictionary<string, object>();
                    dbDict.Add("EventRententionDate", DateTime.UtcNow.AddDays(Config.LoggingConfiguration.LogRetention * -1));
                    dbDict.Add("EventTime", logItem.EventTime);
                    dbDict.Add("EventType", logItem.EventType);
                    dbDict.Add("Process", logItem.Process);
                    dbDict.Add("Message", logItem.Message);
                    dbDict.Add("Exception", Common.ReturnValueIfNull(logItem.ExceptionValue, "").ToString());

                    try
                    {
                        db.ExecuteCMD(sql, dbDict);
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

        static public List<LogItem> GetLogs(long? StartIndex, int PageNumber = 1, int PageSize = 100) 
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "";
            if (StartIndex == null)
            {
                sql = "SELECT * FROM ServerLogs ORDER BY Id DESC LIMIT @PageSize OFFSET @PageNumber;";
            }
            else
            {
                sql = "SELECT * FROM ServerLogs WHERE Id < @StartIndex ORDER BY Id DESC LIMIT @PageSize OFFSET @PageNumber;";
            }
            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            dbDict.Add("StartIndex", StartIndex);
            dbDict.Add("PageNumber", (PageNumber - 1) * PageSize);
            dbDict.Add("PageSize", PageSize);
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
                    ExceptionValue = (string)row["Exception"]
                };

                logs.Add(log);
            }

            return logs;
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
    }
}

