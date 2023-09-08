using System;
namespace gaseous_tools
{
	public class Logging
	{
        static public void Log(LogType EventType, string Section, string Message, Exception? ExceptionValue = null)
        {
            LogItem logItem = new LogItem
            {
                EventTime = DateTime.UtcNow,
                EventType = EventType,
                Section = Section,
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
                string TraceOutput = logItem.EventTime.ToString("yyyyMMdd HHmmss") + ": " + logItem.EventType.ToString() + ": " + logItem.Section + ": " + logItem.Message;
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

                StreamWriter LogFile = File.AppendText(Config.LogFilePath);
                switch (Config.LoggingConfiguration.LogFormat)
                {
                    case Config.ConfigFile.Logging.LoggingFormat.Text:
                        LogFile.WriteLine(TraceOutput);
                        break;

                    case Config.ConfigFile.Logging.LoggingFormat.Json:
                        Newtonsoft.Json.JsonSerializerSettings serializerSettings = new Newtonsoft.Json.JsonSerializerSettings
                        {
                            NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                            Formatting = Newtonsoft.Json.Formatting.Indented
                        };
                        serializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
                        string JsonOutput = Newtonsoft.Json.JsonConvert.SerializeObject(logItem, serializerSettings);
                        LogFile.WriteLine(JsonOutput);
                        break;
                }
                LogFile.Close();
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
            public LogType EventType { get; set; }
            private string _Section = "";
            public string Section
            {
                get
                {
                    return _Section;
                }
                set
                {
                    _Section = value;
                }
            }
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

