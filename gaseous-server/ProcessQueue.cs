using System;
using System.ComponentModel.Design.Serialization;
using System.Data;
using gaseous_server.Classes;
using gaseous_server.Controllers;
using Microsoft.AspNetCore.Identity.UI.V4.Pages.Account.Manage.Internal;
using NuGet.Common;
using NuGet.Packaging;

namespace gaseous_server
{
    public static class ProcessQueue
    {
        public static List<QueueItem> QueueItems = new List<QueueItem>();

        public class QueueItem
        {
            public QueueItem(QueueItemType ItemType, bool AllowManualStart = true, bool RemoveWhenStopped = false)
            {
                _ItemType = ItemType;
                _ItemState = QueueItemState.NeverStarted;
                _LastRunTime = Config.ReadSetting<DateTime>("LastRun_" + _ItemType.ToString(), DateTime.UtcNow.AddMinutes(-5));
                _AllowManualStart = AllowManualStart;
                _RemoveWhenStopped = RemoveWhenStopped;

                // load queueitem configuration
                BackgroundTaskItem defaultItem = new BackgroundTaskItem(ItemType);
                Enabled(defaultItem.Enabled);
                _Interval = defaultItem.Interval;
                _AllowedDays = defaultItem.AllowedDays;
                AllowedStartHours = defaultItem.AllowedStartHours;
                AllowedStartMinutes = defaultItem.AllowedStartMinutes;
                AllowedEndHours = defaultItem.AllowedEndHours;
                AllowedEndMinutes = defaultItem.AllowedEndMinutes;
                _Blocks = defaultItem.Blocks;
            }

            public QueueItem(QueueItemType ItemType, int ExecutionInterval, bool AllowManualStart = true, bool RemoveWhenStopped = false)
            {
                _ItemType = ItemType;
                _ItemState = QueueItemState.NeverStarted;
                _LastRunTime = Config.ReadSetting<DateTime>("LastRun_" + _ItemType.ToString(), DateTime.UtcNow.AddMinutes(-5));
                _Interval = ExecutionInterval;
                _AllowManualStart = AllowManualStart;
                _RemoveWhenStopped = RemoveWhenStopped;

                // load timing defaults
                BackgroundTaskItem defaultItem = new BackgroundTaskItem(ItemType);
                Enabled(defaultItem.Enabled);
                _AllowedDays = defaultItem.AllowedDays;
                AllowedStartHours = defaultItem.AllowedStartHours;
                AllowedStartMinutes = defaultItem.AllowedStartMinutes;
                AllowedEndHours = defaultItem.AllowedEndHours;
                AllowedEndMinutes = defaultItem.AllowedEndMinutes;
            }

            public QueueItem(QueueItemType ItemType, int ExecutionInterval, List<QueueItemType> Blocks, bool AllowManualStart = true, bool RemoveWhenStopped = false)
            {
                _ItemType = ItemType;
                _ItemState = QueueItemState.NeverStarted;
                _LastRunTime = Config.ReadSetting<DateTime>("LastRun_" + _ItemType.ToString(), DateTime.UtcNow.AddMinutes(-5));
                _Interval = ExecutionInterval;
                _AllowManualStart = AllowManualStart;
                _RemoveWhenStopped = RemoveWhenStopped;
                _Blocks = Blocks;

                // load timing defaults
                BackgroundTaskItem defaultItem = new BackgroundTaskItem(ItemType);
                Enabled(defaultItem.Enabled);
                _AllowedDays = defaultItem.AllowedDays;
                AllowedStartHours = defaultItem.AllowedStartHours;
                AllowedStartMinutes = defaultItem.AllowedStartMinutes;
                AllowedEndHours = defaultItem.AllowedEndHours;
                AllowedEndMinutes = defaultItem.AllowedEndMinutes;
            }

            private QueueItemType _ItemType = QueueItemType.NotConfigured;
            private QueueItemState _ItemState = QueueItemState.NeverStarted;
            private DateTime _LastRunTime = DateTime.UtcNow;
            private double _LastRunDuration = 0;
            private DateTime _LastFinishTime
            {
                get
                {
                    // return DateTime.Parse(Config.ReadSetting("LastRun_" + _ItemType.ToString(), DateTime.UtcNow.ToString("yyyy-MM-ddThh:mm:ssZ")));
                    return Config.ReadSetting<DateTime>("LastRun_" + _ItemType.ToString(), DateTime.UtcNow);
                }
                set
                {
                    if (_SaveLastRunTime == true)
                    {
                        //Config.SetSetting("LastRun_" + _ItemType.ToString(), value.ToString("yyyy-MM-ddThh:mm:ssZ"));
                        Config.SetSetting<DateTime>("LastRun_" + _ItemType.ToString(), value);
                    }
                }
            }
            private bool _SaveLastRunTime = false;
            private int _Interval = 0;
            private string _LastResult = "";
            private string? _LastError = null;
            private bool _ForceExecute = false;
            private bool _AllowManualStart = true;
            private bool _RemoveWhenStopped = false;
            private bool _IsBlocked = false;
            private string _CorrelationId = "";
            private List<DayOfWeek> _AllowedDays = new List<DayOfWeek>
            {
                DayOfWeek.Sunday,
                DayOfWeek.Monday,
                DayOfWeek.Tuesday,
                DayOfWeek.Wednesday,
                DayOfWeek.Thursday,
                DayOfWeek.Friday,
                DayOfWeek.Saturday
            };
            private List<QueueItemType> _Blocks = new List<QueueItemType>();

            public List<DayOfWeek> AllowedDays
            {
                get
                {
                    return _AllowedDays;
                }
                set
                {
                    _AllowedDays = value;
                }
            }
            public int AllowedStartHours { get; set; } = 0;
            public int AllowedStartMinutes { get; set; } = 0;
            public int AllowedEndHours { get; set; } = 23;
            public int AllowedEndMinutes { get; set; } = 59;
            public QueueItemType ItemType => _ItemType;
            public QueueItemState ItemState => _ItemState;
            public DateTime LastRunTime => _LastRunTime;
            public DateTime LastFinishTime => _LastFinishTime;
            public double LastRunDuration => _LastRunDuration;
            public DateTime NextRunTime
            {
                get
                {
                    // next run time
                    DateTime tempNextRun = LastRunTime.ToLocalTime().AddMinutes(Interval);
                    // if (tempNextRun < DateTime.Now)
                    // {
                    //     tempNextRun = DateTime.Now;
                    // }
                    DayOfWeek nextWeekDay = tempNextRun.DayOfWeek;

                    // create local start and end times
                    DateTime tempStartTime = new DateTime(tempNextRun.Year, tempNextRun.Month, tempNextRun.Day, AllowedStartHours, AllowedStartMinutes, 0, DateTimeKind.Local);
                    DateTime tempEndTime = new DateTime(tempNextRun.Year, tempNextRun.Month, tempNextRun.Day, AllowedEndHours, AllowedEndMinutes, 0, DateTimeKind.Local);

                    // bump the next run time to the next allowed day and hour range
                    if (AllowedDays.Contains(nextWeekDay))
                    {
                        // next run day is allowed, nothing to do
                    }
                    else
                    {
                        // keep bumping the day forward until the a weekday is found
                        do
                        {
                            tempNextRun = tempNextRun.AddDays(1);
                            nextWeekDay = tempNextRun.DayOfWeek;
                        }
                        while (!AllowedDays.Contains(nextWeekDay));

                        // update windows
                        tempStartTime = new DateTime(tempNextRun.Year, tempNextRun.Month, tempNextRun.Day, AllowedStartHours, AllowedStartMinutes, 0, DateTimeKind.Local);
                        tempEndTime = new DateTime(tempNextRun.Year, tempNextRun.Month, tempNextRun.Day, AllowedEndHours, AllowedEndMinutes, 0, DateTimeKind.Local);
                    }

                    // are the hours in the right range
                    TimeSpan spanNextRun = tempNextRun.TimeOfDay;
                    if (LastRunTime.ToLocalTime().AddMinutes(Interval) < tempStartTime)
                    {
                        return tempStartTime.ToUniversalTime();
                    }
                    else if (spanNextRun >= tempStartTime.TimeOfDay && spanNextRun <= tempEndTime.TimeOfDay)
                    {
                        // all good - return nextRun
                        return tempNextRun.ToUniversalTime();
                    }
                    else
                    {
                        return tempStartTime.ToUniversalTime();
                    }
                }
            }

            public int Interval
            {
                get
                {
                    return _Interval;
                }
                set
                {
                    _Interval = value;
                }
            }
            public string LastResult => _LastResult;
            public string? LastError => _LastError;
            public bool Force => _ForceExecute;
            public bool AllowManualStart => _AllowManualStart;
            public bool RemoveWhenStopped => _RemoveWhenStopped;
            public bool IsBlocked => _IsBlocked;
            public object? Options { get; set; } = null;
            public string CurrentState { get; set; } = "";
            public string CurrentStateProgress { get; set; } = "";
            public string CorrelationId => _CorrelationId;
            public List<QueueItemType> Blocks => _Blocks;

            public void Execute()
            {
                if (_ItemState != QueueItemState.Disabled)
                {
                    if ((DateTime.UtcNow > NextRunTime || _ForceExecute == true) && _ItemState != QueueItemState.Running)
                    {
                        // we can run - do some setup before we start processing
                        _LastRunTime = DateTime.UtcNow;
                        _ItemState = QueueItemState.Running;
                        _LastResult = "";
                        _LastError = null;

                        // set the correlation id
                        Guid correlationId = Guid.NewGuid();
                        _CorrelationId = correlationId.ToString();
                        CallContext.SetData("CorrelationId", correlationId);
                        CallContext.SetData("CallingProcess", _ItemType.ToString());
                        CallContext.SetData("CallingUser", "System");

                        // log the start
                        Logging.Log(Logging.LogType.Debug, "Timered Event", "Executing " + _ItemType + " with correlation id " + _CorrelationId);

                        try
                        {
                            switch (_ItemType)
                            {
                                case QueueItemType.SignatureIngestor:
                                    Logging.Log(Logging.LogType.Debug, "Timered Event", "Starting Signature Ingestor");
                                    SignatureIngestors.XML.XMLIngestor tIngest = new SignatureIngestors.XML.XMLIngestor
                                    {
                                        CallingQueueItem = this
                                    };

                                    foreach (int i in Enum.GetValues(typeof(gaseous_signature_parser.parser.SignatureParser)))
                                    {
                                        gaseous_signature_parser.parser.SignatureParser parserType = (gaseous_signature_parser.parser.SignatureParser)i;
                                        if (
                                            parserType != gaseous_signature_parser.parser.SignatureParser.Auto &&
                                            parserType != gaseous_signature_parser.parser.SignatureParser.Unknown
                                        )
                                        {
                                            Logging.Log(Logging.LogType.Debug, "Signature Import", "Processing " + parserType + " files");

                                            string SignaturePath = Path.Combine(Config.LibraryConfiguration.LibrarySignaturesDirectory, parserType.ToString());
                                            string SignatureProcessedPath = Path.Combine(Config.LibraryConfiguration.LibrarySignaturesProcessedDirectory, parserType.ToString());

                                            if (!Directory.Exists(SignaturePath))
                                            {
                                                Directory.CreateDirectory(SignaturePath);
                                            }

                                            if (!Directory.Exists(SignatureProcessedPath))
                                            {
                                                Directory.CreateDirectory(SignatureProcessedPath);
                                            }

                                            tIngest.Import(SignaturePath, SignatureProcessedPath, parserType);
                                        }
                                    }

                                    _SaveLastRunTime = true;

                                    break;

                                case QueueItemType.TitleIngestor:
                                    Logging.Log(Logging.LogType.Debug, "Timered Event", "Starting Title Ingestor");
                                    Classes.ImportGame import = new ImportGame
                                    {
                                        CallingQueueItem = this
                                    };
                                    import.ProcessDirectory(Config.LibraryConfiguration.LibraryImportDirectory);

                                    // clean up
                                    Classes.ImportGame.DeleteOrphanedDirectories(Config.LibraryConfiguration.LibraryImportDirectory);

                                    _SaveLastRunTime = true;

                                    break;

                                case QueueItemType.MetadataRefresh:
                                    Logging.Log(Logging.LogType.Debug, "Timered Event", "Starting Metadata Refresher");
                                    Classes.MetadataManagement metadataManagement = new MetadataManagement
                                    {
                                        CallingQueueItem = this
                                    };
                                    metadataManagement.RefreshMetadata(_ForceExecute);

                                    _SaveLastRunTime = true;

                                    break;

                                case QueueItemType.OrganiseLibrary:
                                    Logging.Log(Logging.LogType.Debug, "Timered Event", "Starting Library Organiser");
                                    Classes.ImportGame importLibraryOrg = new ImportGame
                                    {
                                        CallingQueueItem = this
                                    };
                                    importLibraryOrg.OrganiseLibrary();

                                    _SaveLastRunTime = true;

                                    break;

                                case QueueItemType.LibraryScan:
                                    Logging.Log(Logging.LogType.Debug, "Timered Event", "Starting Library Scanners");
                                    Classes.ImportGame libScan = new ImportGame
                                    {
                                        CallingQueueItem = this
                                    };
                                    libScan.LibraryScan();

                                    _SaveLastRunTime = true;

                                    break;

                                case QueueItemType.LibraryScanWorker:
                                    GameLibrary.LibraryItem library = (GameLibrary.LibraryItem)Options;
                                    Logging.Log(Logging.LogType.Debug, "Timered Event", "Starting Library Scanner worker for library " + library.Name);
                                    Classes.ImportGame importLibraryScan = new ImportGame
                                    {
                                        CallingQueueItem = this
                                    };
                                    importLibraryScan.LibrarySpecificScan(library);

                                    break;

                                case QueueItemType.CollectionCompiler:
                                    Logging.Log(Logging.LogType.Debug, "Timered Event", "Starting Collection Compiler");
                                    Dictionary<string, object> collectionOptions = (Dictionary<string, object>)Options;
                                    Classes.Collections.CompileCollections((long)collectionOptions["Id"], (string)collectionOptions["UserId"]);
                                    break;

                                case QueueItemType.MediaGroupCompiler:
                                    Logging.Log(Logging.LogType.Debug, "Timered Event", "Starting Media Group Compiler");
                                    Classes.RomMediaGroup.CompileMediaGroup((long)Options);
                                    break;

                                case QueueItemType.BackgroundDatabaseUpgrade:
                                    Logging.Log(Logging.LogType.Debug, "Timered Event", "Starting Background Upgrade");
                                    DatabaseMigration.UpgradeScriptBackgroundTasks();
                                    break;

                                case QueueItemType.DailyMaintainer:
                                    Logging.Log(Logging.LogType.Debug, "Timered Event", "Starting Daily Maintenance");
                                    Classes.Maintenance maintenance = new Maintenance
                                    {
                                        CallingQueueItem = this
                                    };
                                    maintenance.RunDailyMaintenance();

                                    _SaveLastRunTime = true;

                                    break;

                                case QueueItemType.WeeklyMaintainer:
                                    Logging.Log(Logging.LogType.Debug, "Timered Event", "Starting Weekly Maintenance");
                                    Classes.Maintenance weeklyMaintenance = new Maintenance
                                    {
                                        CallingQueueItem = this
                                    };
                                    weeklyMaintenance.RunWeeklyMaintenance();

                                    _SaveLastRunTime = true;
                                    break;

                                case QueueItemType.TempCleanup:
                                    try
                                    {
                                        foreach (GameLibrary.LibraryItem libraryItem in GameLibrary.GetLibraries)
                                        {
                                            string rootPath = Path.Combine(Config.LibraryConfiguration.LibraryTempDirectory, libraryItem.Id.ToString());
                                            if (Directory.Exists(rootPath))
                                            {
                                                foreach (string directory in Directory.GetDirectories(rootPath))
                                                {
                                                    DirectoryInfo info = new DirectoryInfo(directory);
                                                    if (info.LastWriteTimeUtc.AddMinutes(5) < DateTime.UtcNow)
                                                    {
                                                        Logging.Log(Logging.LogType.Information, "Get Signature", "Deleting temporary decompress folder: " + directory);
                                                        Directory.Delete(directory, true);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception tcEx)
                                    {
                                        Logging.Log(Logging.LogType.Warning, "Get Signature", "An error occurred while cleaning temporary files", tcEx);
                                    }
                                    break;

                            }
                        }
                        catch (Exception ex)
                        {
                            Logging.Log(Logging.LogType.Warning, "Timered Event", "An error occurred", ex);
                            _LastResult = "";
                            _LastError = ex.ToString();
                        }

                        _ForceExecute = false;
                        if (_DisableWhenComplete == false)
                        {
                            _ItemState = QueueItemState.Stopped;
                        }
                        else
                        {
                            _ItemState = QueueItemState.Disabled;
                        }
                        _LastFinishTime = DateTime.UtcNow;
                        _LastRunDuration = Math.Round((DateTime.UtcNow - _LastRunTime).TotalSeconds, 2);

                        Logging.Log(Logging.LogType.Information, "Timered Event", "Total " + _ItemType + " run time = " + _LastRunDuration);
                    }
                }
            }

            public void ForceExecute()
            {
                _ForceExecute = true;
            }

            public void BlockedState(bool BlockState)
            {
                _IsBlocked = BlockState;
            }

            private bool _DisableWhenComplete = false;
            public void Enabled(bool Enabled)
            {
                _DisableWhenComplete = !Enabled;
                if (Enabled == true)
                {
                    if (_ItemState == QueueItemState.Disabled)
                    {
                        _ItemState = QueueItemState.Stopped;
                    }
                }
                else
                {
                    if (_ItemState == QueueItemState.Stopped || _ItemState == QueueItemState.NeverStarted)
                    {
                        _ItemState = QueueItemState.Disabled;
                    }
                }
            }

            public HasErrorsItem HasErrors
            {
                get
                {
                    return new HasErrorsItem(_CorrelationId);
                }
            }

            public class HasErrorsItem
            {
                public HasErrorsItem(string? CorrelationId)
                {
                    if (CorrelationId != null)
                    {
                        if (CorrelationId.Length > 0)
                        {
                            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
                            string sql = "SELECT EventType, COUNT(EventType) AS EventTypes FROM gaseous.ServerLogs WHERE CorrelationId = @correlationid GROUP BY EventType ORDER BY EventType DESC LIMIT 1;";
                            Dictionary<string, object> dbDict = new Dictionary<string, object>();
                            dbDict.Add("correlationid", CorrelationId);

                            DataTable data = db.ExecuteCMD(sql, dbDict);

                            if (data.Rows.Count == 0)
                            {
                                ErrorType = null;
                                ErrorCount = 0;
                            }
                            else
                            {
                                Logging.LogType errorType = (Logging.LogType)data.Rows[0]["EventType"];
                                if (errorType != Logging.LogType.Information)
                                {
                                    ErrorType = errorType;
                                    ErrorCount = (int)(long)data.Rows[0]["EventTypes"];
                                }
                                else
                                {
                                    ErrorType = null;
                                    ErrorCount = 0;
                                }
                            }
                        }
                        else
                        {
                            ErrorType = null;
                            ErrorCount = 0;
                        }
                    }
                    else
                    {
                        ErrorType = null;
                        ErrorCount = 0;
                    }
                }

                public Logging.LogType? ErrorType { get; set; }
                public int ErrorCount { get; set; }
            }
        }

        public enum QueueItemType
        {
            /// <summary>
            /// Reserved for blocking all services - no actual background service is tied to this type
            /// </summary>
            All,

            /// <summary>
            /// Default type - no background service is tied to this type
            /// </summary>
            NotConfigured,

            /// <summary>
            /// Ingests signature DAT files into the database
            /// </summary>
            SignatureIngestor,

            /// <summary>
            /// Imports game files into the database and moves them to the required location on disk
            /// </summary>
            TitleIngestor,

            /// <summary>
            /// Forces stored metadata to be refreshed
            /// </summary>
            MetadataRefresh,

            /// <summary>
            /// Ensures all managed files are where they are supposed to be
            /// </summary>
            OrganiseLibrary,

            /// <summary>
            /// Looks for orphaned files in the library and re-adds them to the database
            /// </summary>
            LibraryScan,

            /// <summary>
            /// Performs the work for the LibraryScan task
            /// </summary>
            LibraryScanWorker,

            /// <summary>
            /// Builds collections - set the options attribute to the id of the collection to build
            /// </summary>
            CollectionCompiler,

            /// <summary>
            /// Builds media groups - set the options attribute to the id of the media group to build
            /// </summary>
            MediaGroupCompiler,

            /// <summary>
            /// Performs and post database upgrade scripts that can be processed as a background task
            /// </summary>
            BackgroundDatabaseUpgrade,

            /// <summary>
            /// Performs a clean up of old files, and purge old logs
            /// </summary>
            DailyMaintainer,

            /// <summary>
            /// Performs more intensive cleanups and optimises the database
            /// </summary>
            WeeklyMaintainer,

            /// <summary>
            /// Cleans up marked paths in the temporary directory
            /// </summary>
            TempCleanup
        }

        public enum QueueItemState
        {
            NeverStarted,
            Running,
            Stopped,
            Disabled
        }
    }
}

