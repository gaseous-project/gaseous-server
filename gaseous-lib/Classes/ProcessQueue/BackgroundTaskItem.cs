using System.Reflection;
using gaseous_server.Classes;

namespace gaseous_server.ProcessQueue
{
    public class BackgroundTaskItem
    {
        public BackgroundTaskItem()
        {

        }

        public BackgroundTaskItem(ProcessQueue.QueueItemType TaskName)
        {
            this.Task = TaskName.ToString();
            this.TaskEnum = TaskName;

            switch (TaskName)
            {
                case ProcessQueue.QueueItemType.SignatureIngestor:
                    this._UserManageable = true;
                    this._SaveLastRunTime = true;
                    this.DefaultInterval = 60;
                    this.MinimumAllowedInterval = 20;
                    this.DefaultAllowedDays = new List<DayOfWeek>{
                        DayOfWeek.Sunday,
                        DayOfWeek.Monday,
                        DayOfWeek.Tuesday,
                        DayOfWeek.Wednesday,
                        DayOfWeek.Thursday,
                        DayOfWeek.Friday,
                        DayOfWeek.Saturday
                    };
                    this.DefaultAllowedStartHours = 0;
                    this.DefaultAllowedStartMinutes = 0;
                    this.DefaultAllowedEndHours = 23;
                    this.DefaultAllowedEndMinutes = 59;
                    this.RunInProcess = false;
                    break;

                case ProcessQueue.QueueItemType.TitleIngestor:
                    this._UserManageable = true;
                    this._SaveLastRunTime = true;
                    this.DefaultInterval = 1;
                    this.MinimumAllowedInterval = 1;
                    this.DefaultAllowedDays = new List<DayOfWeek>{
                        DayOfWeek.Sunday,
                        DayOfWeek.Monday,
                        DayOfWeek.Tuesday,
                        DayOfWeek.Wednesday,
                        DayOfWeek.Thursday,
                        DayOfWeek.Friday,
                        DayOfWeek.Saturday
                    };
                    this.DefaultAllowedStartHours = 0;
                    this.DefaultAllowedStartMinutes = 0;
                    this.DefaultAllowedEndHours = 23;
                    this.DefaultAllowedEndMinutes = 59;
                    this._Blocks = new List<ProcessQueue.QueueItemType>{
                        ProcessQueue.QueueItemType.OrganiseLibrary,
                        ProcessQueue.QueueItemType.LibraryScan,
                        ProcessQueue.QueueItemType.LibraryScanWorker,
                        ProcessQueue.QueueItemType.MetadataRefresh
                    };
                    this.RunInProcess = true;
                    break;

                case ProcessQueue.QueueItemType.MetadataRefresh:
                    this._UserManageable = true;
                    this._SaveLastRunTime = true;
                    this.DefaultInterval = 1440;
                    this.MinimumAllowedInterval = 1440;
                    this.DefaultAllowedDays = new List<DayOfWeek>{
                        DayOfWeek.Sunday,
                        DayOfWeek.Monday,
                        DayOfWeek.Tuesday,
                        DayOfWeek.Wednesday,
                        DayOfWeek.Thursday,
                        DayOfWeek.Friday,
                        DayOfWeek.Saturday
                    };
                    this.DefaultAllowedStartHours = 0;
                    this.DefaultAllowedStartMinutes = 0;
                    this.DefaultAllowedEndHours = 23;
                    this.DefaultAllowedEndMinutes = 59;
                    this._Blocks = new List<ProcessQueue.QueueItemType>
                    {
                        ProcessQueue.QueueItemType.OrganiseLibrary,
                        ProcessQueue.QueueItemType.LibraryScan,
                        ProcessQueue.QueueItemType.LibraryScanWorker,
                        ProcessQueue.QueueItemType.TitleIngestor
                    };
                    this.RunInProcess = false;
                    break;

                case ProcessQueue.QueueItemType.OrganiseLibrary:
                    this._UserManageable = true;
                    this._SaveLastRunTime = true;
                    this.DefaultInterval = 1440;
                    this.MinimumAllowedInterval = 120;
                    this.DefaultAllowedDays = new List<DayOfWeek>{
                        DayOfWeek.Sunday,
                        DayOfWeek.Monday,
                        DayOfWeek.Tuesday,
                        DayOfWeek.Wednesday,
                        DayOfWeek.Thursday,
                        DayOfWeek.Friday,
                        DayOfWeek.Saturday
                    };
                    this.DefaultAllowedStartHours = 0;
                    this.DefaultAllowedStartMinutes = 0;
                    this.DefaultAllowedEndHours = 23;
                    this.DefaultAllowedEndMinutes = 59;
                    this._Blocks = new List<ProcessQueue.QueueItemType>{
                        ProcessQueue.QueueItemType.LibraryScan,
                        ProcessQueue.QueueItemType.LibraryScanWorker,
                        ProcessQueue.QueueItemType.TitleIngestor,
                        ProcessQueue.QueueItemType.MetadataRefresh
                    };
                    this.RunInProcess = true;
                    break;

                case ProcessQueue.QueueItemType.LibraryScan:
                    this._UserManageable = true;
                    this._SaveLastRunTime = true;
                    this.DefaultInterval = 1440;
                    this.MinimumAllowedInterval = 120;
                    this.DefaultAllowedDays = new List<DayOfWeek>{
                        DayOfWeek.Sunday,
                        DayOfWeek.Monday,
                        DayOfWeek.Tuesday,
                        DayOfWeek.Wednesday,
                        DayOfWeek.Thursday,
                        DayOfWeek.Friday,
                        DayOfWeek.Saturday
                    };
                    this.DefaultAllowedStartHours = 0;
                    this.DefaultAllowedStartMinutes = 0;
                    this.DefaultAllowedEndHours = 23;
                    this.DefaultAllowedEndMinutes = 59;
                    this._Blocks = new List<ProcessQueue.QueueItemType>{
                        ProcessQueue.QueueItemType.OrganiseLibrary,
                        ProcessQueue.QueueItemType.MetadataRefresh
                    };
                    this.RunInProcess = false;
                    break;

                case ProcessQueue.QueueItemType.DailyMaintainer:
                    this._UserManageable = true;
                    this._SaveLastRunTime = true;
                    this.DefaultInterval = 1440;
                    this.MinimumAllowedInterval = 1440;
                    this.DefaultAllowedDays = new List<DayOfWeek>{
                        DayOfWeek.Sunday,
                        DayOfWeek.Monday,
                        DayOfWeek.Tuesday,
                        DayOfWeek.Wednesday,
                        DayOfWeek.Thursday,
                        DayOfWeek.Friday,
                        DayOfWeek.Saturday
                    };
                    this.DefaultAllowedStartHours = 1;
                    this.DefaultAllowedStartMinutes = 0;
                    this.DefaultAllowedEndHours = 5;
                    this.DefaultAllowedEndMinutes = 59;
                    this._Blocks = new List<ProcessQueue.QueueItemType>{
                        ProcessQueue.QueueItemType.All
                    };
                    this.RunInProcess = false;
                    break;

                case ProcessQueue.QueueItemType.WeeklyMaintainer:
                    this._UserManageable = true;
                    this._SaveLastRunTime = true;
                    this.DefaultInterval = 10080;
                    this.MinimumAllowedInterval = 10080;
                    this.DefaultAllowedDays = new List<DayOfWeek>{
                        DayOfWeek.Monday
                    };
                    this.DefaultAllowedStartHours = 1;
                    this.DefaultAllowedStartMinutes = 0;
                    this.DefaultAllowedEndHours = 5;
                    this.DefaultAllowedEndMinutes = 59;
                    this._Blocks = new List<ProcessQueue.QueueItemType>{
                        ProcessQueue.QueueItemType.All
                    };
                    this.RunInProcess = false;
                    break;

                case ProcessQueue.QueueItemType.BackgroundDatabaseUpgrade:
                    this._UserManageable = false;
                    this.DefaultInterval = 1;
                    this.MinimumAllowedInterval = 1;
                    this.DefaultAllowedDays = new List<DayOfWeek>{
                        DayOfWeek.Sunday,
                        DayOfWeek.Monday,
                        DayOfWeek.Tuesday,
                        DayOfWeek.Wednesday,
                        DayOfWeek.Thursday,
                        DayOfWeek.Friday,
                        DayOfWeek.Saturday
                    };
                    this.DefaultAllowedStartHours = 0;
                    this.DefaultAllowedStartMinutes = 0;
                    this.DefaultAllowedEndHours = 23;
                    this.DefaultAllowedEndMinutes = 59;
                    this._Blocks.Add(ProcessQueue.QueueItemType.All);
                    this.RunInProcess = true;
                    break;

                case ProcessQueue.QueueItemType.TempCleanup:
                    this._UserManageable = true;
                    this.DefaultInterval = 1;
                    this.MinimumAllowedInterval = 1;
                    this.DefaultAllowedDays = new List<DayOfWeek>{
                        DayOfWeek.Sunday,
                        DayOfWeek.Monday,
                        DayOfWeek.Tuesday,
                        DayOfWeek.Wednesday,
                        DayOfWeek.Thursday,
                        DayOfWeek.Friday,
                        DayOfWeek.Saturday
                    };
                    this.DefaultAllowedStartHours = 0;
                    this.DefaultAllowedStartMinutes = 0;
                    this.DefaultAllowedEndHours = 23;
                    this.DefaultAllowedEndMinutes = 59;
                    this.RunInProcess = false;
                    break;

                default:
                    this._UserManageable = false;
                    this.DefaultAllowedDays = new List<DayOfWeek>{
                        DayOfWeek.Sunday,
                        DayOfWeek.Monday,
                        DayOfWeek.Tuesday,
                        DayOfWeek.Wednesday,
                        DayOfWeek.Thursday,
                        DayOfWeek.Friday,
                        DayOfWeek.Saturday
                    };
                    this.DefaultAllowedStartHours = 0;
                    this.DefaultAllowedStartMinutes = 0;
                    this.DefaultAllowedEndHours = 23;
                    this.DefaultAllowedEndMinutes = 59;
                    this.RunInProcess = true;
                    break;
            }
        }

        public string Task { get; set; }
        public ProcessQueue.QueueItemType TaskEnum { get; set; }
        public bool Enabled
        {
            get
            {
                if (_UserManageable == true)
                {
                    return bool.Parse(Config.ReadSetting<string>("Enabled_" + Task, true.ToString()));
                }
                else
                {
                    return true;
                }
            }
            set
            {
                if (_UserManageable == true)
                {
                    Config.SetSetting<string>("Enabled_" + Task, value.ToString());
                }
            }
        }
        private bool _UserManageable;
        public bool UserManageable => _UserManageable;
        private bool _SaveLastRunTime;
        public bool SaveLastRunTime => _SaveLastRunTime;
        public int Interval
        {
            get
            {
                return int.Parse(Config.ReadSetting<string>("Interval_" + Task, DefaultInterval.ToString()));
            }
        }
        public int DefaultInterval { get; set; }
        public int MinimumAllowedInterval { get; set; }
        public bool RunInProcess { get; set; }
        public List<DayOfWeek> AllowedDays
        {
            get
            {
                string jsonDefaultAllowedDays = Newtonsoft.Json.JsonConvert.SerializeObject(DefaultAllowedDays);
                return Newtonsoft.Json.JsonConvert.DeserializeObject<List<DayOfWeek>>(Config.ReadSetting<string>("AllowedDays_" + Task, jsonDefaultAllowedDays));
            }
        }
        public int AllowedStartHours
        {
            get
            {
                return int.Parse(Config.ReadSetting<string>("AllowedStartHours_" + Task, DefaultAllowedStartHours.ToString()));
            }
        }
        public int AllowedStartMinutes
        {
            get
            {
                return int.Parse(Config.ReadSetting<string>("AllowedStartMinutes_" + Task, DefaultAllowedStartMinutes.ToString()));
            }
        }
        public int AllowedEndHours
        {
            get
            {
                return int.Parse(Config.ReadSetting<string>("AllowedEndHours_" + Task, DefaultAllowedEndHours.ToString()));
            }
        }
        public int AllowedEndMinutes
        {
            get
            {
                return int.Parse(Config.ReadSetting<string>("AllowedEndMinutes_" + Task, DefaultAllowedEndMinutes.ToString()));
            }
        }
        public List<DayOfWeek> DefaultAllowedDays { get; set; }
        public int DefaultAllowedStartHours { get; set; }
        public int DefaultAllowedStartMinutes { get; set; }
        public int DefaultAllowedEndHours { get; set; }
        public int DefaultAllowedEndMinutes { get; set; }
        private List<ProcessQueue.QueueItemType> _Blocks = new List<ProcessQueue.QueueItemType>();
        public List<ProcessQueue.QueueItemType> Blocks
        {
            get
            {
                if (_Blocks.Contains(ProcessQueue.QueueItemType.All))
                {
                    List<ProcessQueue.QueueItemType> blockList = new List<ProcessQueue.QueueItemType>();
                    List<ProcessQueue.QueueItemType> skipBlockItems = new List<ProcessQueue.QueueItemType>{
                        ProcessQueue.QueueItemType.All,
                        ProcessQueue.QueueItemType.NotConfigured,
                        this.TaskEnum
                    };
                    foreach (ProcessQueue.QueueItemType blockType in Enum.GetValues(typeof(ProcessQueue.QueueItemType)))
                    {
                        if (!skipBlockItems.Contains(blockType))
                        {
                            blockList.Add(blockType);
                        }
                    }
                    return blockList;
                }
                else
                {
                    return _Blocks;
                }
            }
        }
        public List<ProcessQueue.QueueItemType> BlockedBy
        {
            get
            {
                List<ProcessQueue.QueueItemType> blockedBy = new List<ProcessQueue.QueueItemType>();

                foreach (ProcessQueue.QueueItemType blockType in Enum.GetValues(typeof(ProcessQueue.QueueItemType)))
                {
                    if (blockType != this.TaskEnum)
                    {
                        BackgroundTaskItem taskItem = new BackgroundTaskItem(blockType);
                        if (taskItem.Blocks.Contains(this.TaskEnum))
                        {
                            if (!blockedBy.Contains(blockType))
                            {
                                blockedBy.Add(blockType);
                            }
                        }
                    }
                }

                return blockedBy;
            }
        }
    }

    public class BackgroundTaskSettingsItem
    {
        public string Task { get; set; }
        public bool Enabled { get; set; }
        public int Interval { get; set; }
        public List<DayOfWeek> AllowedDays { get; set; }
        public int AllowedStartHours { get; set; }
        public int AllowedStartMinutes { get; set; }
        public int AllowedEndHours { get; set; }
        public int AllowedEndMinutes { get; set; }
    }
}