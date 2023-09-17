using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using Org.BouncyCastle.Utilities;
namespace gaseous_tools
{
	public class Logging
	{
        // when was the last clean
        static DateTime LastRetentionClean = DateTime.UtcNow;
        // how often to clean in hours
        const int RetentionCleanInterval = 1;

        static public void Log(LogType EventType, string ServerProcess, string Message, Exception? ExceptionValue = null)
        {
            LogItem logItem = new LogItem
            {
                EventTime = DateTime.UtcNow,
                EventType = EventType,
                Process = ServerProcess,
                Message = Message,
                ExceptionValue = ExceptionValue
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

                Newtonsoft.Json.JsonSerializerSettings serializerSettings = new Newtonsoft.Json.JsonSerializerSettings
                {
                    NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                    Formatting = Newtonsoft.Json.Formatting.None
                };
                serializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());

                // write log file
                string JsonOutput = Newtonsoft.Json.JsonConvert.SerializeObject(logItem, serializerSettings);
                File.AppendAllText(Config.LogFilePath, JsonOutput);
            }

            // quick clean before we go
            if (LastRetentionClean.AddHours(RetentionCleanInterval) < DateTime.UtcNow)
            {
                LogCleanup();
            }
        }

        static public List<LogItem> GetLogs() {
            string logData = File.ReadAllText(Config.LogFilePath);

            List<LogItem> logs = new List<LogItem>();
            if (File.Exists(Config.LogFilePath))
            {
                StreamReader sr = new StreamReader(Config.LogFilePath);
                while (!sr.EndOfStream)
                {
                    LogItem logItem = Newtonsoft.Json.JsonConvert.DeserializeObject<LogItem>(sr.ReadLine());
                    logs.Add(logItem);
                }
                logs.Reverse();
            }

            return logs;
        }

        static public void LogCleanup()
        {
            Log(LogType.Information, "Log Cleanup", "Purging log files older than " + Config.LoggingConfiguration.LogRetention + " days");
            LastRetentionClean = DateTime.UtcNow;

            string[] files = Directory.GetFiles(Config.LogPath, "Server Log *.json");

            foreach (string file in files)
            {
                FileInfo fi = new FileInfo(file);
                if (fi.LastAccessTime.AddDays(Config.LoggingConfiguration.LogRetention) < DateTime.Now)
                {
                    try
                    {
                        fi.Delete();
                    }
                    catch
                    {
                        Log(LogType.Warning, "Log Cleanup", "Failed purging log " + fi.FullName);
                    }
                }
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
            public Exception? ExceptionValue { get; set; }
        }
    }
}

