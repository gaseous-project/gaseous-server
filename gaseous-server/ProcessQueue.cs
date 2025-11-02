using System;
using System.ComponentModel.Design.Serialization;
using System.Data;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using gaseous_server.Classes;
using gaseous_server.Classes.Metadata;
using gaseous_server.Controllers;
using gaseous_server.Models;
using HasheousClient.Models.Metadata.IGDB;
using Microsoft.AspNetCore.Identity.UI.V4.Pages.Account.Manage.Internal;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Newtonsoft.Json;
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
            [Newtonsoft.Json.JsonIgnore]
            [System.Text.Json.Serialization.JsonIgnore]
            public List<SubTask> SubTasks { get; set; } = new List<SubTask>();
            public Guid AddSubTask(SubTask.TaskTypes TaskType, string TaskName, object Settings, bool RemoveWhenCompleted)
            {
                // check if the task already exists
                SubTask? existingTask = SubTasks.FirstOrDefault(x => x.TaskType == TaskType && x.TaskName == TaskName);
                if (existingTask == null)
                {
                    // generate a new correlation id
                    Guid correlationId = Guid.NewGuid();

                    // add the task to the list
                    SubTask subTask = new SubTask(this, TaskType, TaskName, Settings, correlationId);
                    subTask.RemoveWhenStopped = RemoveWhenCompleted;
                    SubTasks.Add(subTask);

                    Logging.Log(Logging.LogType.Information, "Queue Item", "Added subtask " + TaskName + " of type " + TaskType.ToString() + " with correlation id " + correlationId.ToString(), null, false, new Dictionary<string, object>
                    {
                        { "ParentQueueItem", _ItemType.ToString() },
                        { "SubTaskType", TaskType.ToString() },
                        { "SubTaskName", TaskName },
                        { "SubTaskCorrelationId", correlationId.ToString() }
                    });

                    return correlationId;
                }
                else
                {
                    return Guid.Parse(existingTask.CorrelationId);
                }
            }
            public List<SubTask> ChildTasks
            {
                get
                {
                    // return only the first 10 tasks
                    List<SubTask> childTasks = new List<SubTask>();
                    int maxTasks = 10;
                    foreach (SubTask task in SubTasks)
                    {
                        if (childTasks.Count >= maxTasks)
                        {
                            break;
                        }
                        childTasks.Add(task);
                    }
                    return childTasks;
                }
            }

            [Newtonsoft.Json.JsonIgnore]
            [System.Text.Json.Serialization.JsonIgnore]
            Dictionary<string, Thread> BackgroundThreads = null;
            public class SubTask
            {
                public TaskTypes TaskType
                {
                    get
                    {
                        return _TaskType;
                    }
                }
                private TaskTypes _TaskType;
                public enum TaskTypes
                {
                    ImportQueueProcessor,
                    MetadataRefresh_Platform,
                    MetadataRefresh_Signatures,
                    MetadataRefresh_Game,
                    DatabaseMigration_1031,
                    LibraryScanWorker
                }
                private string _CorrelationId;
                public string CorrelationId
                {
                    get
                    {
                        return _CorrelationId;
                    }
                }
                public QueueItemState State
                {
                    get
                    {
                        return _State;
                    }
                }
                private QueueItemState _State = QueueItemState.NeverStarted;
                public bool AllowConcurrentExecution
                {
                    get
                    {
                        return _AllowConcurrentExecution;
                    }
                }
                private bool _AllowConcurrentExecution = false;
                public string Status
                {
                    get
                    {
                        return _Status;
                    }
                }
                private string _Status = "";
                public string TaskName
                {
                    get
                    {
                        return _TaskName;
                    }
                }
                private string _TaskName = "";
                public object Settings
                {
                    get
                    {
                        return _Settings;
                    }
                }
                private object _Settings = new object();
                public bool RemoveWhenStopped { get; set; } = false;
                public string CurrentState { get; set; } = "";
                public string CurrentStateProgress { get; set; } = "";
                [Newtonsoft.Json.JsonIgnore]
                [System.Text.Json.Serialization.JsonIgnore]
                public object? ParentObject
                {
                    get
                    {
                        return _ParentObject;
                    }
                }
                private object? _ParentObject = null;
                public SubTask(object? ParentObject, TaskTypes TaskType, string TaskName, object Settings, Guid CorrelationId = default)
                {
                    _ParentObject = ParentObject;
                    _TaskType = TaskType;
                    _TaskName = TaskName;
                    _Settings = Settings;

                    _CorrelationId = CorrelationId.ToString();

                    switch (TaskType)
                    {
                        case TaskTypes.ImportQueueProcessor:
                            ImportGame.UpdateImportState((Guid)_Settings, ImportStateItem.ImportState.Queued, ImportStateItem.ImportType.Unknown, null);
                            break;
                    }
                }
                public async Task Execute()
                {
                    CallContext.SetData("CorrelationId", _CorrelationId.ToString());
                    CallContext.SetData("CallingProcess", _TaskType.ToString());
                    CallContext.SetData("CallingUser", "System");

                    if (_State == QueueItemState.NeverStarted)
                    {
                        _State = QueueItemState.Running;
                        // do some work
                        switch (_TaskType)
                        {
                            case TaskTypes.ImportQueueProcessor:
                                Logging.Log(Logging.LogType.Information, "Import Queue Processor", "Processing import " + _TaskName);

                                // update the import state
                                ImportGame.UpdateImportState((Guid)_Settings, ImportStateItem.ImportState.Processing, ImportStateItem.ImportType.Unknown, null);

                                ImportStateItem importState = ImportGame.GetImportState((Guid)_Settings);
                                if (importState != null)
                                {
                                    Dictionary<string, object>? ProcessData = new Dictionary<string, object>();
                                    ProcessData.Add("path", Path.GetFileName(importState.FileName));
                                    ProcessData.Add("sessionid", importState.SessionId.ToString());

                                    // get the hash of the file
                                    HashObject hash = new HashObject(importState.FileName);
                                    ProcessData.Add("md5hash", hash.md5hash);
                                    ProcessData.Add("sha1hash", hash.sha1hash);
                                    ProcessData.Add("crc32hash", hash.crc32hash);

                                    // check if the file is a bios file first
                                    Models.PlatformMapping.PlatformMapItem? IsBios = Classes.Bios.BiosHashSignatureLookup(hash.md5hash);

                                    if (IsBios != null)
                                    {
                                        // file is a bios
                                        Bios.ImportBiosFile(importState.FileName, hash, ref ProcessData);

                                        ImportGame.UpdateImportState((Guid)_Settings, ImportStateItem.ImportState.Completed, ImportStateItem.ImportType.BIOS, ProcessData);
                                    }
                                    else if (
                                        Common.SkippableFiles.Contains<string>(Path.GetFileName(importState.FileName), StringComparer.OrdinalIgnoreCase) ||
                                        !PlatformMapping.SupportedFileExtensions.Contains(Path.GetExtension(importState.FileName), StringComparer.OrdinalIgnoreCase)
                                    )
                                    {
                                        Logging.Log(Logging.LogType.Debug, "Import Game", "Skipping item " + importState.FileName + " - not a supported file type");
                                        ImportGame.UpdateImportState((Guid)_Settings, ImportStateItem.ImportState.Skipped, ImportStateItem.ImportType.Unknown, ProcessData);
                                    }
                                    else
                                    {
                                        // file is a rom
                                        Platform? platformOverride = null;
                                        if (importState.PlatformOverride != null)
                                        {
                                            platformOverride = await Platforms.GetPlatform((long)importState.PlatformOverride);
                                        }
                                        ImportGame.ImportGameFile(importState.FileName, hash, ref ProcessData, platformOverride);

                                        ImportGame.UpdateImportState((Guid)_Settings, ImportStateItem.ImportState.Processing, ImportStateItem.ImportType.Rom, ProcessData);

                                        // refresh the metadata for the game - this is a task that can run in the background
                                        _AllowConcurrentExecution = true;
                                        if (ProcessData.ContainsKey("metadatamapid"))
                                        {
                                            long? metadataMapId = (long?)ProcessData["metadatamapid"];
                                            if (metadataMapId != null)
                                            {
                                                MetadataManagement metadataManagement = new MetadataManagement();
                                                await metadataManagement.RefreshSpecificGameAsync((long)metadataMapId);
                                            }
                                        }
                                        ImportGame.UpdateImportState((Guid)_Settings, ImportStateItem.ImportState.Completed, ImportStateItem.ImportType.Rom, ProcessData);
                                    }
                                }
                                else
                                {
                                    Logging.Log(Logging.LogType.Warning, "Import Queue Processor", "Import " + _TaskName + " not found");
                                }

                                break;

                            case TaskTypes.MetadataRefresh_Platform:
                                Logging.Log(Logging.LogType.Information, "Metadata Refresh", "Refreshing platform metadata for " + _TaskName);
                                MetadataManagement metadataPlatform = new MetadataManagement(this);
                                await metadataPlatform.RefreshPlatforms(true);
                                break;

                            case TaskTypes.MetadataRefresh_Signatures:
                                Logging.Log(Logging.LogType.Information, "Metadata Refresh", "Refreshing signature metadata for " + _TaskName);
                                MetadataManagement metadataSignatures = new MetadataManagement(this);
                                await metadataSignatures.RefreshSignatures(true);
                                break;

                            case TaskTypes.MetadataRefresh_Game:
                                Logging.Log(Logging.LogType.Information, "Metadata Refresh", "Refreshing game metadata for " + _TaskName);
                                MetadataManagement metadataGame = new MetadataManagement(this);
                                metadataGame.UpdateRomCounts();
                                await metadataGame.RefreshGames(true);
                                break;

                            case TaskTypes.DatabaseMigration_1031:
                                Logging.Log(Logging.LogType.Information, "Database Migration", "Running database migration 1031 for " + _TaskName);
                                await DatabaseMigration.RunMigration1031();
                                break;

                            case TaskTypes.LibraryScanWorker:
                                CallContext.SetData("CallingProcess", _TaskType.ToString() + " - " + ((GameLibrary.LibraryItem)_Settings).Name);
                                Logging.Log(Logging.LogType.Information, "Library Scan", "Scanning library " + _TaskName);
                                ImportGame importLibraryScan = new ImportGame(this);
                                await importLibraryScan.LibrarySpecificScan((GameLibrary.LibraryItem)_Settings);
                                break;
                        }
                        _State = QueueItemState.Stopped;
                        _Status = "Stopped";

                        if (RemoveWhenStopped == true)
                        {
                            // remove the task from the parent object
                            if (_ParentObject is QueueItem parent)
                            {
                                parent.SubTasks.Remove(this);
                            }
                        }
                    }
                }
            }
            private QueueItemState _ItemState = QueueItemState.NeverStarted;
            private DateTime _LastRunTime = DateTime.UtcNow;
            private double _LastRunDuration = 0;
            private DateTime _LastFinishTime
            {
                get
                {
                    return Config.ReadSetting<DateTime>("LastRun_" + _ItemType.ToString(), DateTime.UtcNow);
                }
                set
                {
                    if (_SaveLastRunTime == true)
                    {
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
            public QueueItemState ItemState
            {
                get
                {
                    // if any subtasks are running or never started, set the state to running
                    if (SubTasks != null)
                    {
                        foreach (SubTask task in SubTasks)
                        {
                            if (task.State == QueueItemState.Running || task.State == QueueItemState.NeverStarted)
                            {
                                return QueueItemState.Running;
                            }
                        }
                    }

                    return _ItemState;
                }
            }
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

            public async Task Execute()
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

                                    _SaveLastRunTime = true;

                                    break;

                                case QueueItemType.ImportQueueProcessor:
                                    Logging.Log(Logging.LogType.Debug, "Timered Event", "Starting Import Queue Processor");

                                    if (ImportGame.ImportStates != null)
                                    {
                                        // get all pending imports
                                        List<ImportStateItem> pendingImports = ImportGame.ImportStates.Where(x => x.State == ImportStateItem.ImportState.Pending).ToList();

                                        // process each import
                                        foreach (ImportStateItem importState in pendingImports)
                                        {
                                            // check the subtask list for any tasks with the same session id
                                            SubTask? subTask = SubTasks.FirstOrDefault(x => x.Settings is Guid && (Guid)x.Settings == importState.SessionId);
                                            if (subTask == null)
                                            {
                                                // process the import
                                                Logging.Log(Logging.LogType.Information, "Import Queue Processor", "Processing import " + importState.FileName);
                                                AddSubTask(SubTask.TaskTypes.ImportQueueProcessor, Path.GetFileName(importState.FileName), importState.SessionId, true);
                                                // update the import state
                                                ImportGame.UpdateImportState(importState.SessionId, ImportStateItem.ImportState.Queued, ImportStateItem.ImportType.Unknown, null);
                                            }
                                        }
                                    }

                                    // clean up
                                    Classes.ImportGame.DeleteOrphanedDirectories(Config.LibraryConfiguration.LibraryImportDirectory);
                                    Classes.ImportGame.RemoveOldImportStates();

                                    // force execute this task again so the user doesn't have to wait for imports to be processed
                                    _LastRunTime = DateTime.UtcNow.AddMinutes(-_Interval);

                                    break;

                                case QueueItemType.MetadataRefresh:
                                    Logging.Log(Logging.LogType.Debug, "Timered Event", "Starting Metadata Refresher");

                                    // clear the sub tasks
                                    if (SubTasks != null)
                                    {
                                        SubTasks.Clear();
                                    }
                                    else
                                    {
                                        SubTasks = new List<SubTask>();
                                    }

                                    // set up metadata refresh subtasks
                                    AddSubTask(SubTask.TaskTypes.MetadataRefresh_Platform, "Platform Metadata", null, true);
                                    AddSubTask(SubTask.TaskTypes.MetadataRefresh_Signatures, "Signature Metadata", null, true);
                                    AddSubTask(SubTask.TaskTypes.MetadataRefresh_Game, "Game Metadata", null, true);

                                    _SaveLastRunTime = true;

                                    break;

                                case QueueItemType.OrganiseLibrary:
                                    Logging.Log(Logging.LogType.Debug, "Timered Event", "Starting Library Organiser");
                                    Classes.ImportGame importLibraryOrg = new ImportGame
                                    {
                                        CallingQueueItem = this
                                    };
                                    await importLibraryOrg.OrganiseLibrary();

                                    _SaveLastRunTime = true;

                                    break;

                                case QueueItemType.LibraryScan:
                                    Logging.Log(Logging.LogType.Debug, "Timered Event", "Starting Library Scanners");
                                    Classes.ImportGame libScan = new ImportGame
                                    {
                                        CallingQueueItem = this
                                    };

                                    // get all libraries
                                    if (SubTasks == null || SubTasks.Count == 0)
                                    {
                                        List<GameLibrary.LibraryItem> libraries = await GameLibrary.GetLibraries();

                                        // process each library
                                        foreach (GameLibrary.LibraryItem library in libraries)
                                        {
                                            Guid childCorrelationId = AddSubTask(SubTask.TaskTypes.LibraryScanWorker, library.Name, library, true);
                                            Logging.Log(Logging.LogType.Information, "Library Scan", "Queuing library " + library.Name + " for scanning with correlation id: " + childCorrelationId);
                                        }
                                    }

                                    _SaveLastRunTime = true;

                                    break;

                                case QueueItemType.CollectionCompiler:
                                    Logging.Log(Logging.LogType.Debug, "Timered Event", "Starting Collection Compiler");
                                    Dictionary<string, object> collectionOptions = (Dictionary<string, object>)Options;
                                    Classes.Collections.CompileCollections((long)collectionOptions["Id"], (string)collectionOptions["UserId"]);
                                    break;

                                case QueueItemType.MediaGroupCompiler:
                                    Logging.Log(Logging.LogType.Debug, "Timered Event", "Starting Media Group Compiler");
                                    await Classes.RomMediaGroup.CompileMediaGroup((long)Options);
                                    break;

                                case QueueItemType.BackgroundDatabaseUpgrade:
                                    Logging.Log(Logging.LogType.Debug, "Timered Event", "Starting Background Upgrade");
                                    await DatabaseMigration.UpgradeScriptBackgroundTasks();
                                    break;

                                case QueueItemType.DailyMaintainer:
                                    Logging.Log(Logging.LogType.Debug, "Timered Event", "Starting Daily Maintenance");
                                    Classes.Maintenance maintenance = new Maintenance
                                    {
                                        CallingQueueItem = this
                                    };
                                    await maintenance.RunDailyMaintenance();

                                    _SaveLastRunTime = true;

                                    break;

                                case QueueItemType.WeeklyMaintainer:
                                    Logging.Log(Logging.LogType.Debug, "Timered Event", "Starting Weekly Maintenance");
                                    Classes.Maintenance weeklyMaintenance = new Maintenance
                                    {
                                        CallingQueueItem = this
                                    };
                                    await weeklyMaintenance.RunWeeklyMaintenance();

                                    _SaveLastRunTime = true;
                                    break;

                                case QueueItemType.TempCleanup:
                                    try
                                    {
                                        foreach (GameLibrary.LibraryItem libraryItem in await GameLibrary.GetLibraries())
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

                            // execute sub tasks
                            if (SubTasks != null)
                            {
                                SubTaskExecute();
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

            void SubTaskExecute()
            {
                // execute any subtasks
                if (SubTasks != null)
                {
                    if (BackgroundThreads == null)
                    {
                        BackgroundThreads = new Dictionary<string, Thread>();
                    }

                    // execute all subtasks in order, only move to the next one is the previous one is complete, or if the task is allowed to run concurrently
                    int subTaskProgressCount = 0;
                    do
                    {
                        // get the next eligible task
                        SubTask? nextTask = SubTasks.FirstOrDefault(x => x.State == QueueItemState.NeverStarted);

                        int maxThreads = 4;

                        // if we have too many threads running, wait for one to finish
                        while (BackgroundThreads.Count >= maxThreads)
                        {
                            // remove any completed threads
                            List<string> completedThreads = new List<string>();
                            foreach (KeyValuePair<string, Thread> thread in BackgroundThreads)
                            {
                                if (thread.Value.ThreadState == ThreadState.Stopped)
                                {
                                    completedThreads.Add(thread.Key);
                                }
                            }
                            foreach (string threadKey in completedThreads)
                            {
                                BackgroundThreads.Remove(threadKey);
                            }

                            // jobs can only be added to the thread pool if there is space
                            if (BackgroundThreads.Count < maxThreads)
                            {
                                break;
                            }

                            // wait for a second
                            Thread.Sleep(1000);
                        }

                        // add the next task to the thread pool
                        if (nextTask != null)
                        {
                            // execute the task
                            Thread thread = new Thread(() => nextTask.Execute().GetAwaiter().GetResult());
                            thread.Name = nextTask.TaskName;
                            thread.Start();
                            BackgroundThreads.Add(nextTask.TaskName, thread);

                            subTaskProgressCount += 1;
                            CurrentState = "Running " + nextTask.TaskName;
                            if (nextTask.RemoveWhenStopped == true)
                            {
                                CurrentStateProgress = subTaskProgressCount.ToString();
                            }
                            else
                            {
                                CurrentStateProgress = subTaskProgressCount + " of " + SubTasks.Count;
                            }

                            // wait for the thread to finish
                            while (thread.IsAlive)
                            {
                                // check if the thread is still running
                                if (thread.ThreadState == ThreadState.Stopped || (nextTask.AllowConcurrentExecution == true && SubTasks.Count == 1))
                                {
                                    break;
                                }
                                else
                                {
                                    // wait for a second
                                    Thread.Sleep(1000);
                                }
                            }
                        }
                        else
                        {
                            break;
                        }

                    } while (SubTasks.Count > 0);

                    // wait for all threads to finish
                    bool stillRunning = true;
                    while (stillRunning)
                    {
                        // remove any completed threads
                        List<string> completedThreads = new List<string>();
                        foreach (KeyValuePair<string, Thread> thread in BackgroundThreads)
                        {
                            if (thread.Value.ThreadState == ThreadState.Stopped)
                            {
                                completedThreads.Add(thread.Key);
                            }
                        }
                        foreach (string threadKey in completedThreads)
                        {
                            BackgroundThreads.Remove(threadKey);
                        }

                        // check if all threads are complete
                        if (BackgroundThreads.Count == 0)
                        {
                            stillRunning = false;
                        }
                        else
                        {
                            // wait for a second
                            Thread.Sleep(1000);
                        }
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
            /// Processes the import queue and imports files into the database
            /// </summary>
            ImportQueueProcessor,

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

