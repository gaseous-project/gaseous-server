using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using gaseous_server.Classes;
using gaseous_server.Classes.Metadata;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Razor.Hosting;
using RestEase;
using Asp.Versioning;

namespace gaseous_server.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0", Deprecated = true)]
    [ApiVersion("1.1")]
    [Authorize]
    public class SystemController : Controller
    {
        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public SystemInfo GetSystemStatus()
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);

            SystemInfo ReturnValue = new SystemInfo();

            // // disk size
            // List<SystemInfo.PathItem> Disks = new List<SystemInfo.PathItem>();
            // foreach (GameLibrary.LibraryItem libraryItem in GameLibrary.GetLibraries())
            // {
            //     SystemInfo.PathItem pathItem = GetDisk(libraryItem.Path);
            //     pathItem.Name = libraryItem.Name;

            //     Disks.Add(pathItem);
            // }
            // ReturnValue.Paths = Disks;

            // database size
            string sql = "SELECT table_schema, SUM(data_length + index_length) FROM information_schema.tables WHERE table_schema = '" + Config.DatabaseConfiguration.DatabaseName + "'";
            DataTable dbResponse = db.ExecuteCMD(sql);
            ReturnValue.DatabaseSize = (long)(System.Decimal)dbResponse.Rows[0][1];

            // platform statistics
            sql = @"
SELECT 
    view_Games_Roms.PlatformId,
    Platform.`Name`,
    SUM(view_Games_Roms.Size) AS Size,
    COUNT(view_Games_Roms.`Id`) AS Count
FROM
    view_Games_Roms
        LEFT JOIN
    `Metadata_Platform` AS `Platform` ON view_Games_Roms.PlatformId = Platform.`Id`
        AND Platform.SourceId = 0
GROUP BY Platform.`Name`
ORDER BY Platform.`Name`; ";
            dbResponse = db.ExecuteCMD(sql);
            ReturnValue.PlatformStatistics = new List<SystemInfo.PlatformStatisticsItem>();
            foreach (DataRow dr in dbResponse.Rows)
            {
                SystemInfo.PlatformStatisticsItem platformStatisticsItem = new SystemInfo.PlatformStatisticsItem
                {
                    Platform = (string)dr["name"],
                    RomCount = (long)dr["Count"],
                    TotalSize = (long)(System.Decimal)dr["Size"]
                };
                ReturnValue.PlatformStatistics.Add(platformStatisticsItem);
            }

            return ReturnValue;
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("Version")]
        [ProducesResponseType(typeof(Dictionary<string, object>), StatusCodes.Status200OK)]
        [AllowAnonymous]
        public ActionResult GetSystemVersion()
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);

            Dictionary<string, object> ageRatingAndGroups = AgeGroups.GetAgeRatingAndGroupings();

            Dictionary<string, object> retVal = new Dictionary<string, object>
            {
                {
                    "AppVersion", Assembly.GetExecutingAssembly().GetName().Version.ToString()
                },
                {
                    "DBSchemaVersion", db.GetDatabaseSchemaVersion().ToString()
                },
                {
                    "FirstRunStatus", Config.ReadSetting<string>("FirstRunStatus", "0")
                },
                {
                    "AgeRatingBoardsStrings", ageRatingAndGroups["ClassificationBoards"]
                },
                {
                    "AgeRatingStrings", ageRatingAndGroups["AgeRatings"]
                },
                {
                    "AgeRatingGroups", ageRatingAndGroups["AgeGroups"]
                },
                {
                    "emulatorDebugMode", Config.ReadSetting<string>("emulatorDebugMode", false.ToString()).ToLower()
                },
                {
                    "SupportedMetadataSources", Models.MetadataMapSupportedDataSources.SupportedMetadataSources
                }
            };

            return Ok(retVal);
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("VersionFile")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public FileContentResult GetSystemVersionAsFile()
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);

            Dictionary<string, object> ageRatingAndGroups = AgeGroups.GetAgeRatingAndGroupings();

            string ver = "var AppVersion = \"" + Assembly.GetExecutingAssembly().GetName().Version.ToString() + "\";" + Environment.NewLine +
                "var DBSchemaVersion = \"" + db.GetDatabaseSchemaVersion() + "\";" + Environment.NewLine +
                "var FirstRunStatus = \"" + Config.ReadSetting<string>("FirstRunStatus", "0") + "\";" + Environment.NewLine +
                "var AgeRatingBoardsStrings = " + JsonSerializer.Serialize(ageRatingAndGroups["ClassificationBoards"], new JsonSerializerOptions
                {
                    WriteIndented = true
                }) + ";" + Environment.NewLine +
                "var AgeRatingStrings = " + JsonSerializer.Serialize(ageRatingAndGroups["AgeRatings"], new JsonSerializerOptions
                {
                    WriteIndented = true
                }) + ";" + Environment.NewLine +
                "var AgeRatingGroups = " + JsonSerializer.Serialize(ageRatingAndGroups["AgeGroups"], new JsonSerializerOptions
                {
                    WriteIndented = true
                }) + ";" + Environment.NewLine +
                "var emulatorDebugMode = " + Config.ReadSetting<string>("emulatorDebugMode", false.ToString()).ToLower() + ";";
            byte[] bytes = Encoding.UTF8.GetBytes(ver);
            return File(bytes, "text/javascript");
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("Settings/BackgroundTasks/Configuration")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult GetBackgroundTasks()
        {
            Dictionary<string, BackgroundTaskItem> Intervals = new Dictionary<string, BackgroundTaskItem>();
            foreach (ProcessQueue.QueueItemType itemType in Enum.GetValues(typeof(ProcessQueue.QueueItemType)))
            {
                BackgroundTaskItem taskItem = new BackgroundTaskItem(itemType);
                if (taskItem.UserManageable == true)
                {
                    if (!Intervals.ContainsKey(itemType.ToString()))
                    {
                        Intervals.Add(itemType.ToString(), taskItem);
                    }
                }
            }

            return Ok(Intervals);
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpPost]
        [Route("Settings/BackgroundTasks/Configuration")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult SetBackgroundTasks([FromBody] List<BackgroundTaskSettingsItem> model)
        {
            foreach (BackgroundTaskSettingsItem TaskConfiguration in model)
            {
                if (Enum.IsDefined(typeof(ProcessQueue.QueueItemType), TaskConfiguration.Task))
                {
                    try
                    {
                        BackgroundTaskItem taskItem = new BackgroundTaskItem(
                            (ProcessQueue.QueueItemType)Enum.Parse(typeof(ProcessQueue.QueueItemType), TaskConfiguration.Task)
                        );

                        if (taskItem.UserManageable == true)
                        {
                            // update task enabled
                            Logging.LogKey(Logging.LogType.Information, "process.update_background_task", "updatebackgroundtask.updating_task_enabled_value", new string[] { TaskConfiguration.Task, TaskConfiguration.Enabled.ToString() });

                            Config.SetSetting<string>("Enabled_" + TaskConfiguration.Task, TaskConfiguration.Enabled.ToString());

                            // update existing process
                            foreach (ProcessQueue.QueueProcessor.QueueItem item in ProcessQueue.QueueProcessor.QueueItems)
                            {
                                if (item.ItemType.ToString().ToLower() == TaskConfiguration.Task.ToLower())
                                {
                                    item.Enabled(Boolean.Parse(TaskConfiguration.Enabled.ToString()));
                                }
                            }

                            // update task interval
                            if (TaskConfiguration.Interval >= taskItem.MinimumAllowedInterval)
                            {
                                Logging.LogKey(Logging.LogType.Information, "process.update_background_task", "updatebackgroundtask.updating_task_new_interval", new string[] { TaskConfiguration.Task, TaskConfiguration.Interval.ToString() });

                                Config.SetSetting<string>("Interval_" + TaskConfiguration.Task, TaskConfiguration.Interval.ToString());

                                // update existing process
                                foreach (ProcessQueue.QueueProcessor.QueueItem item in ProcessQueue.QueueProcessor.QueueItems)
                                {
                                    if (item.ItemType.ToString().ToLower() == TaskConfiguration.Task.ToLower())
                                    {
                                        item.Interval = TaskConfiguration.Interval;
                                    }
                                }
                            }
                            else
                            {
                                Logging.LogKey(Logging.LogType.Warning, "process.update_background_task", "updatebackgroundtask.interval_below_minimum_skipping", new string[] { TaskConfiguration.Interval.ToString(), TaskConfiguration.Task, taskItem.MinimumAllowedInterval.ToString() });
                            }

                            // update task weekdays
                            Logging.LogKey(Logging.LogType.Information, "process.update_background_task", "updatebackgroundtask.updating_task_new_weekdays", new string[] { TaskConfiguration.Task, String.Join(", ", TaskConfiguration.AllowedDays) });

                            Config.SetSetting<string>("AllowedDays_" + TaskConfiguration.Task, Newtonsoft.Json.JsonConvert.SerializeObject(TaskConfiguration.AllowedDays));

                            // update existing process
                            foreach (ProcessQueue.QueueProcessor.QueueItem item in ProcessQueue.QueueProcessor.QueueItems)
                            {
                                if (item.ItemType.ToString().ToLower() == TaskConfiguration.Task.ToLower())
                                {
                                    item.AllowedDays = TaskConfiguration.AllowedDays;
                                }
                            }

                            // update task hours
                            Logging.LogKey(Logging.LogType.Information, "process.update_background_task", "updatebackgroundtask.updating_task_new_hours", new string[] { TaskConfiguration.Task, TaskConfiguration.AllowedStartHours.ToString(), TaskConfiguration.AllowedStartMinutes.ToString("00"), TaskConfiguration.AllowedEndHours.ToString(), TaskConfiguration.AllowedEndMinutes.ToString("00") });

                            Config.SetSetting<string>("AllowedStartHours_" + TaskConfiguration.Task, TaskConfiguration.AllowedStartHours.ToString());
                            Config.SetSetting<string>("AllowedStartMinutes_" + TaskConfiguration.Task, TaskConfiguration.AllowedStartMinutes.ToString());
                            Config.SetSetting<string>("AllowedEndHours_" + TaskConfiguration.Task, TaskConfiguration.AllowedEndHours.ToString());
                            Config.SetSetting<string>("AllowedEndMinutes_" + TaskConfiguration.Task, TaskConfiguration.AllowedEndMinutes.ToString());

                            // update existing process
                            foreach (ProcessQueue.QueueProcessor.QueueItem item in ProcessQueue.QueueProcessor.QueueItems)
                            {
                                if (item.ItemType.ToString().ToLower() == TaskConfiguration.Task.ToLower())
                                {
                                    item.AllowedStartHours = TaskConfiguration.AllowedStartHours;
                                    item.AllowedStartMinutes = TaskConfiguration.AllowedStartMinutes;
                                    item.AllowedEndHours = TaskConfiguration.AllowedEndHours;
                                    item.AllowedEndMinutes = TaskConfiguration.AllowedEndMinutes;
                                }
                            }

                        }
                        else
                        {
                            Logging.LogKey(Logging.LogType.Warning, "process.update_background_task", "updatebackgroundtask.unable_to_update_non_user_manageable", new string[] { TaskConfiguration.Task });
                        }
                    }
                    catch
                    {
                        // task name not defined
                        Logging.LogKey(Logging.LogType.Warning, "process.update_background_task", "updatebackgroundtask.task_not_user_definable_skipping", new string[] { TaskConfiguration.Task });
                    }
                }
            }

            return Ok();
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("Settings/System")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult GetSystemSettings()
        {
            SystemSettingsModel systemSettingsModel = new SystemSettingsModel
            {
                AlwaysLogToDisk = Config.LoggingConfiguration.AlwaysLogToDisk,
                MinimumLogRetentionPeriod = Config.LoggingConfiguration.LogRetention,
                EmulatorDebugMode = Boolean.Parse(Config.ReadSetting<string>("emulatorDebugMode", false.ToString())),
                SignatureSource = new SystemSettingsModel.SignatureSourceItem()
                {
                    Source = Config.MetadataConfiguration.SignatureSource,
                    HasheousHost = Config.MetadataConfiguration.HasheousHost,
                    HasheousSubmitFixes = (bool)Config.MetadataConfiguration.HasheousSubmitFixes,
                    HasheousAPIKey = Config.MetadataConfiguration.HasheousAPIKey
                },
                MetadataSources = new List<SystemSettingsModel.MetadataSourceItem>
                {
                    new SystemSettingsModel.MetadataSourceItem(FileSignature.MetadataSources.None, false, "", "", Config.MetadataConfiguration.DefaultMetadataSource),
                    new SystemSettingsModel.MetadataSourceItem(FileSignature.MetadataSources.IGDB, Config.IGDB.UseHasheousProxy, Config.IGDB.ClientId, Config.IGDB.Secret, Config.MetadataConfiguration.DefaultMetadataSource),
                    new SystemSettingsModel.MetadataSourceItem(FileSignature.MetadataSources.TheGamesDb, true, "", "", Config.MetadataConfiguration.DefaultMetadataSource)
                }
            };

            return Ok(systemSettingsModel);
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpPost]
        [Route("Settings/System")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult SetSystemSettings(SystemSettingsModel model)
        {
            if (ModelState.IsValid)
            {
                Config.LoggingConfiguration.AlwaysLogToDisk = model.AlwaysLogToDisk;
                Config.LoggingConfiguration.LogRetention = model.MinimumLogRetentionPeriod;
                Config.SetSetting<string>("emulatorDebugMode", model.EmulatorDebugMode.ToString());
                Config.MetadataConfiguration.SignatureSource = model.SignatureSource.Source;
                Config.MetadataConfiguration.HasheousHost = model.SignatureSource.HasheousHost;
                Config.MetadataConfiguration.HasheousAPIKey = model.SignatureSource.HasheousAPIKey;
                Config.MetadataConfiguration.HasheousSubmitFixes = model.SignatureSource.HasheousSubmitFixes;

                // reset the default metadata source to none
                Config.MetadataConfiguration.DefaultMetadataSource = FileSignature.MetadataSources.None;

                foreach (SystemSettingsModel.MetadataSourceItem metadataSourceItem in model.MetadataSources)
                {
                    // configure the default metadata source
                    if (metadataSourceItem.Default == true)
                    {
                        Config.MetadataConfiguration.DefaultMetadataSource = metadataSourceItem.Source;
                    }

                    // configure the metadata source
                    switch (metadataSourceItem.Source)
                    {
                        case FileSignature.MetadataSources.None:
                            break;
                        case FileSignature.MetadataSources.IGDB:
                            Config.IGDB.UseHasheousProxy = metadataSourceItem.UseHasheousProxy;
                            Config.IGDB.ClientId = metadataSourceItem.ClientId;
                            Config.IGDB.Secret = metadataSourceItem.Secret;
                            break;
                        case FileSignature.MetadataSources.TheGamesDb:
                            break;
                        default:
                            break;
                    }
                }
                Config.UpdateConfig();
            }

            return Ok(model);
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpPut]
        [Route("Settings/System")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult SetSystemSetting([FromBody] Dictionary<string, object> value)
        {
            foreach (var kvp in value)
            {
                try
                {
                    string strValue = kvp.Value.ToString();

                    switch (kvp.Key.ToLower())
                    {
                        case "logginconfiguration.alwayslogtodisk":
                            bool parsedValue = bool.Parse(strValue);
                            Config.LoggingConfiguration.AlwaysLogToDisk = parsedValue;
                            break;

                        case "loggingconfiguration.logretention":
                            int retentionValue = int.Parse(strValue);
                            Config.LoggingConfiguration.LogRetention = retentionValue;
                            break;

                        case "emulatordebugmode":
                            bool emulatorDebugMode = bool.Parse(strValue);
                            Config.SetSetting<string>("emulatorDebugMode", emulatorDebugMode.ToString());
                            break;

                        case "metadataconfiguration.signaturesource":
                            Config.MetadataConfiguration.SignatureSource = (HasheousClient.Models.MetadataModel.SignatureSources)Enum.Parse(typeof(HasheousClient.Models.MetadataModel.SignatureSources), strValue);
                            break;

                        case "metadataconfiguration.hasheoushost":
                            Config.MetadataConfiguration.HasheousHost = strValue;
                            break;

                        case "metadataconfiguration.hasheousapikey":
                            Config.MetadataConfiguration.HasheousAPIKey = strValue;
                            break;

                        case "metadataconfiguration.hasheoussubmitfixes":
                            bool hasheousSubmitFixes = bool.Parse(strValue);
                            Config.MetadataConfiguration.HasheousSubmitFixes = hasheousSubmitFixes;
                            break;

                        case "metadataconfiguration.defaultmetadatasource":
                            Config.MetadataConfiguration.DefaultMetadataSource = (FileSignature.MetadataSources)Enum.Parse(typeof(FileSignature.MetadataSources), strValue);
                            break;

                        case "igdb.usehasheousproxy":
                            bool useHasheousProxy = bool.Parse(strValue);
                            Config.IGDB.UseHasheousProxy = useHasheousProxy;
                            break;

                        case "igdb.clientid":
                            Config.IGDB.ClientId = strValue;
                            break;

                        case "igdb.secret":
                            Config.IGDB.Secret = strValue;
                            break;

                        default:
                            Logging.LogKey(Logging.LogType.Warning, "process.system_setting", "systemsetting.unknown_system_setting", null, new string[] { kvp.Key });
                            return BadRequest("Unknown system setting: " + kvp.Key);
                    }
                }
                catch (Exception ex)
                {
                    Logging.LogKey(Logging.LogType.Warning, "process.system_setting", "systemsetting.error_setting_system_setting", null, new string[] { kvp.Key, ex.Message });
                    return BadRequest("Error setting system setting: " + ex.Message);
                }

                // Update the configuration file
                Logging.LogKey(Logging.LogType.Information, "process.system_setting", "systemsetting.updating_system_setting", null, new string[] { kvp.Value?.ToString() ?? "" });
                Config.UpdateConfig();
            }

            return Ok();
        }

        public static SystemInfo.PathItem GetDisk(string Path)
        {
            SystemInfo.PathItem pathItem = new SystemInfo.PathItem
            {
                LibraryPath = Path,
                SpaceUsed = Common.DirSize(new DirectoryInfo(Path)),
                SpaceAvailable = new DriveInfo(Path).AvailableFreeSpace,
                TotalSpace = new DriveInfo(Path).TotalSize
            };

            return pathItem;
        }

        public class SystemInfo
        {
            public Version ApplicationVersion
            {
                get
                {
                    return Assembly.GetExecutingAssembly().GetName().Version;
                }
            }
            public List<PathItem>? Paths { get; set; }
            public long DatabaseSize { get; set; }
            public List<PlatformStatisticsItem>? PlatformStatistics { get; set; }

            public class PathItem
            {
                public string Name { get; set; }
                public string LibraryPath { get; set; }
                public long SpaceUsed { get; set; }
                public long SpaceAvailable { get; set; }
                public long TotalSpace { get; set; }
            }

            public class PlatformStatisticsItem
            {
                public string Platform { get; set; }
                public long RomCount { get; set; }
                public long TotalSize { get; set; }
            }
        }
    }

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
                    break;

                case ProcessQueue.QueueItemType.TitleIngestor:
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
                    this._Blocks = new List<ProcessQueue.QueueItemType>{
                        ProcessQueue.QueueItemType.OrganiseLibrary,
                        ProcessQueue.QueueItemType.LibraryScan,
                        ProcessQueue.QueueItemType.LibraryScanWorker,
                        ProcessQueue.QueueItemType.MetadataRefresh
                    };
                    break;

                case ProcessQueue.QueueItemType.MetadataRefresh:
                    this._UserManageable = true;
                    this.DefaultInterval = 360;
                    this.MinimumAllowedInterval = 360;
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
                    break;

                case ProcessQueue.QueueItemType.OrganiseLibrary:
                    this._UserManageable = true;
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
                    break;

                case ProcessQueue.QueueItemType.LibraryScan:
                    this._UserManageable = true;
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
                    break;

                case ProcessQueue.QueueItemType.DailyMaintainer:
                    this._UserManageable = true;
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
                    break;

                case ProcessQueue.QueueItemType.WeeklyMaintainer:
                    this._UserManageable = true;
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
        public int Interval
        {
            get
            {
                return int.Parse(Config.ReadSetting<string>("Interval_" + Task, DefaultInterval.ToString()));
            }
        }
        public int DefaultInterval { get; set; }
        public int MinimumAllowedInterval { get; set; }
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

                List<BackgroundTaskItem> backgroundTaskItems = new List<BackgroundTaskItem>();
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

    public class SystemSettingsModel
    {
        public bool AlwaysLogToDisk { get; set; }
        public int MinimumLogRetentionPeriod { get; set; }
        public bool EmulatorDebugMode { get; set; }
        public SignatureSourceItem SignatureSource { get; set; }
        public List<MetadataSourceItem> MetadataSources { get; set; }

        public class SignatureSourceItem
        {
            public HasheousClient.Models.MetadataModel.SignatureSources Source { get; set; }
            public string HasheousHost { get; set; }
            public string HasheousAPIKey { get; set; }
            public bool HasheousSubmitFixes { get; set; }
        }

        public class MetadataSourceItem
        {
            public MetadataSourceItem()
            {

            }

            public MetadataSourceItem(FileSignature.MetadataSources source, bool useHasheousProxy, string clientId, string secret, FileSignature.MetadataSources defaultSource)
            {
                Source = source;
                UseHasheousProxy = useHasheousProxy;
                ClientId = clientId;
                Secret = secret;
                if (Source == defaultSource)
                {
                    Default = true;
                }
                else
                {
                    Default = false;
                }
            }

            public FileSignature.MetadataSources Source { get; set; }
            public bool UseHasheousProxy { get; set; }
            public string ClientId { get; set; }
            public string Secret { get; set; }
            public bool Default { get; set; }
            public bool? Configured
            {
                get
                {
                    switch (Source)
                    {
                        case FileSignature.MetadataSources.None:
                            return true;
                        case FileSignature.MetadataSources.IGDB:
                            if ((!String.IsNullOrEmpty(ClientId) && !String.IsNullOrEmpty(Secret)) || UseHasheousProxy == true)
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        case FileSignature.MetadataSources.TheGamesDb:
                            if ((!String.IsNullOrEmpty(ClientId) && !String.IsNullOrEmpty(Secret)) || UseHasheousProxy == true)
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        default:
                            return false;
                    }
                }
            }
            public bool? UsesProxy
            {
                get
                {
                    switch (Source)
                    {
                        case FileSignature.MetadataSources.None:
                            return false;
                        case FileSignature.MetadataSources.IGDB:
                            return true;
                        case FileSignature.MetadataSources.TheGamesDb:
                            return true;
                        default:
                            return false;
                    }
                }
            }
            public bool? UsesClientIdAndSecret
            {
                get
                {
                    switch (Source)
                    {
                        case FileSignature.MetadataSources.None:
                            return false;
                        case FileSignature.MetadataSources.IGDB:
                            return true;
                        case FileSignature.MetadataSources.TheGamesDb:
                            return false;
                        default:
                            return false;
                    }
                }
            }
        }
    }
}