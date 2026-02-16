using System;
using System.ComponentModel.Design.Serialization;
using System.Data;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using gaseous_server.Classes;
using gaseous_server.Classes.Metadata;
using gaseous_server.Models;
using gaseous_server.ProcessQueue.Plugins;
using gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes;
using Microsoft.AspNetCore.Identity.UI.V4.Pages.Account.Manage.Internal;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Newtonsoft.Json;
using NuGet.Common;
using NuGet.Packaging;

namespace gaseous_server.ProcessQueue
{
    public static class QueueProcessor
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
                _Blocks = defaultItem.Blocks;
                _SaveLastRunTime = defaultItem.SaveLastRunTime;
                _Interval = defaultItem.Interval;
                _AllowedDays = defaultItem.AllowedDays;
                AllowedStartHours = defaultItem.AllowedStartHours;
                AllowedStartMinutes = defaultItem.AllowedStartMinutes;
                AllowedEndHours = defaultItem.AllowedEndHours;
                AllowedEndMinutes = defaultItem.AllowedEndMinutes;
                _RunInProcess = defaultItem.RunInProcess;

                AttachPlugin();
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
                _Blocks = defaultItem.Blocks;
                _SaveLastRunTime = defaultItem.SaveLastRunTime;
                _AllowedDays = defaultItem.AllowedDays;
                AllowedStartHours = defaultItem.AllowedStartHours;
                AllowedStartMinutes = defaultItem.AllowedStartMinutes;
                AllowedEndHours = defaultItem.AllowedEndHours;
                AllowedEndMinutes = defaultItem.AllowedEndMinutes;
                _RunInProcess = defaultItem.RunInProcess;

                AttachPlugin();
            }

            private void AttachPlugin()
            {
                // attach the plugin based on the item type
                switch (_ItemType)
                {
                    case QueueItemType.SignatureIngestor:
                        this.Task = new SignatureIngestor { ParentQueueItem = this };
                        break;

                    case QueueItemType.TitleIngestor:
                        this.Task = new TitleIngestor { ParentQueueItem = this };
                        break;

                    case QueueItemType.ImportQueueProcessor:
                        this.Task = new ImportQueueProcessor { ParentQueueItem = this };
                        break;

                    case QueueItemType.MetadataRefresh:
                        this.Task = new MetadataRefresh { ParentQueueItem = this };
                        break;

                    case QueueItemType.OrganiseLibrary:
                        this.Task = new OrganiseLibrary { ParentQueueItem = this };
                        break;

                    case QueueItemType.LibraryScan:
                        this.Task = new LibraryScan { ParentQueueItem = this };
                        break;

                    case QueueItemType.CollectionCompiler:
                        this.Task = new CollectionCompiler { ParentQueueItem = this };
                        break;

                    case QueueItemType.MediaGroupCompiler:
                        this.Task = new MediaGroupCompiler { ParentQueueItem = this };
                        break;

                    case QueueItemType.BackgroundDatabaseUpgrade:
                        this.Task = new BackgroundDatabaseUpgrade { ParentQueueItem = this };
                        break;

                    case QueueItemType.DailyMaintainer:
                        this.Task = new DailyMaintainer { ParentQueueItem = this };
                        break;

                    case QueueItemType.WeeklyMaintainer:
                        this.Task = new WeeklyMaintainer { ParentQueueItem = this };
                        break;

                    case QueueItemType.TempCleanup:
                        this.Task = new TempCleanup { ParentQueueItem = this };
                        break;
                }
            }

            private QueueItemType _ItemType = QueueItemType.NotConfigured;
            [Newtonsoft.Json.JsonIgnore]
            [System.Text.Json.Serialization.JsonIgnore]
            public List<SubTask> SubTasks { get; set; } = new List<SubTask>();
            public Guid AddSubTask(QueueItemSubTasks TaskType, string TaskName, object? Settings, bool RemoveWhenCompleted)
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

                    Logging.LogKey(
                        Logging.LogType.Information,
                        "process.queue_item",
                        "queue.added_subtask",
                        null,
                        new[] { TaskName, TaskType.ToString(), correlationId.ToString() },
                        null,
                        false,
                        new Dictionary<string, object>
                        {
                            { "ParentQueueItem", _ItemType.ToString() },
                            { "SubTaskType", TaskType.ToString() },
                            { "SubTaskName", TaskName },
                            { "SubTaskCorrelationId", correlationId.ToString() }
                        }
                    );

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
                public QueueItemSubTasks TaskType
                {
                    get
                    {
                        return _TaskType;
                    }
                }
                private QueueItemSubTasks _TaskType;

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
                    set
                    {
                        _AllowConcurrentExecution = value;
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
                private object? _Settings = new object();
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
                public SubTask(object? ParentObject, QueueItemSubTasks TaskType, string TaskName, object? Settings, Guid CorrelationId = default)
                {
                    _ParentObject = ParentObject;
                    _TaskType = TaskType;
                    _TaskName = TaskName;
                    _Settings = Settings;

                    _CorrelationId = CorrelationId.ToString();

                    switch (TaskType)
                    {
                        case QueueItemSubTasks.ImportQueueProcessor:
                            ImportGame.UpdateImportState((Guid)_Settings, ImportStateItem.ImportState.Queued, ImportStateItem.ImportType.Unknown, null);
                            this.subTask = new ImportQueueProcessor.SubTaskItem
                            {
                                ParentSubTaskItem = this
                            };
                            break;

                        case QueueItemSubTasks.MetadataRefresh_Signatures:
                            this.subTask = new MetadataRefresh.SubTaskSignatureRefresh
                            {
                                ParentSubTaskItem = this
                            };
                            break;

                        case QueueItemSubTasks.MetadataRefresh_Platform:
                            this.subTask = new MetadataRefresh.SubTaskPlatformRefresh
                            {
                                ParentSubTaskItem = this
                            };
                            break;

                        case QueueItemSubTasks.MetadataRefresh_Game:
                            this.subTask = new MetadataRefresh.SubTaskGameRefresh
                            {
                                ParentSubTaskItem = this
                            };
                            break;

                        case QueueItemSubTasks.LibraryScanWorker:
                            this.subTask = new LibraryScan.SubTaskLibraryScanWorker
                            {
                                ParentSubTaskItem = this
                            };
                            break;
                    }
                }
                private gaseous_server.ProcessQueue.Plugins.ITaskPlugin.ISubTaskItem subTask { get; set; }
                public async Task Execute()
                {
                    CallContext.SetData("CorrelationId", _CorrelationId.ToString());
                    CallContext.SetData("CallingProcess", _TaskType.ToString());
                    CallContext.SetData("CallingUser", "System");

                    if (_State == QueueItemState.NeverStarted)
                    {
                        _State = QueueItemState.Running;

                        if (subTask != null)
                        {
                            await subTask.Execute();
                        }
                        else
                        {
                            // do some work
                            switch (_TaskType)
                            {
                                case QueueItemSubTasks.DatabaseMigration_1031:
                                    Logging.LogKey(Logging.LogType.Information, "process.database", "database.running_migration_for", null, new[] { _TaskName, "1031" });
                                    await DatabaseMigration.RunMigration1031();
                                    break;
                            }
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
            public bool ForceStartRequested
            {
                get
                {
                    return _ForceExecute;
                }
            }
            private bool _RunInProcess = true;
            public bool RunInProcess
            {
                get
                {
                    return _RunInProcess;
                }
            }
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
            public DateTime LastRunTime
            {
                get
                {
                    return _LastRunTime;
                }
                set
                {
                    _LastRunTime = value;
                }
            }
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

            [System.Text.Json.Serialization.JsonIgnore]
            [Newtonsoft.Json.JsonIgnore]
            public ITaskPlugin? Task { get; set; } = null;

            private List<QueueItemType> _Blocks = new List<QueueItemType>();
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
                        Logging.LogKey(Logging.LogType.Debug, "process.timered_event", "timered_event.executing_item_with_correlation_id", null, new[] { _ItemType.ToString(), _CorrelationId });

                        try
                        {
                            if (Task != null)
                            {
                                // execute the task plugin
                                DateTime startTime = DateTime.UtcNow;
                                await Task.Execute();
                                _LastRunDuration = (DateTime.UtcNow - startTime).TotalSeconds;
                            }

                            // execute sub tasks
                            if (SubTasks != null)
                            {
                                SubTaskExecute();
                            }
                        }
                        catch (Exception ex)
                        {
                            Logging.LogKey(Logging.LogType.Warning, "process.timered_event", "timered_event.error_occurred", null, null, ex);
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

                        Logging.LogKey(Logging.LogType.Information, "process.timered_event", "timered_event.total_run_time", null, new[] { _ItemType.ToString(), _LastRunDuration.ToString() });
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

        public enum QueueItemState
        {
            NeverStarted,
            Running,
            Stopped,
            Disabled
        }
    }
}

