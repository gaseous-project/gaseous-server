﻿using System;
using System.ComponentModel.Design.Serialization;
using System.Data;
using gaseous_server.Classes;

namespace gaseous_server
{
	public static class ProcessQueue
	{
        public static List<QueueItem> QueueItems = new List<QueueItem>();

        public class QueueItem
        {
            public QueueItem(QueueItemType ItemType, int ExecutionInterval, bool AllowManualStart = true, bool RemoveWhenStopped = false)
            {
                _ItemType = ItemType;
                _ItemState = QueueItemState.NeverStarted;
                _LastRunTime = DateTime.Parse(Config.ReadSetting("LastRun_" + _ItemType.ToString(), DateTime.UtcNow.ToString("yyyy-MM-ddThh:mm:ssZ"))).AddMinutes(-5);
                _Interval = ExecutionInterval;
                _AllowManualStart = AllowManualStart;
                _RemoveWhenStopped = RemoveWhenStopped;
            }

            public QueueItem(QueueItemType ItemType, int ExecutionInterval, List<QueueItemType> Blocks, bool AllowManualStart = true, bool RemoveWhenStopped = false)
            {
                _ItemType = ItemType;
                _ItemState = QueueItemState.NeverStarted;
                _LastRunTime = DateTime.Parse(Config.ReadSetting("LastRun_" + _ItemType.ToString(), DateTime.UtcNow.ToString("yyyy-MM-ddThh:mm:ssZ"))).AddMinutes(-5);
                _Interval = ExecutionInterval;
                _AllowManualStart = AllowManualStart;
                _RemoveWhenStopped = RemoveWhenStopped;
                _Blocks = Blocks;
            }

            private QueueItemType _ItemType = QueueItemType.NotConfigured;
            private QueueItemState _ItemState = QueueItemState.NeverStarted;
            private DateTime _LastRunTime = DateTime.UtcNow;
            private double _LastRunDuration = 0;
            private DateTime _LastFinishTime
            {
                get
                {
                    return DateTime.Parse(Config.ReadSetting("LastRun_" + _ItemType.ToString(), DateTime.UtcNow.ToString("yyyy-MM-ddThh:mm:ssZ")));
                }
                set
                {
                    if (_SaveLastRunTime == true)
                    {
                        Config.SetSetting("LastRun_" + _ItemType.ToString(), value.ToString("yyyy-MM-ddThh:mm:ssZ"));
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
            private List<QueueItemType> _Blocks = new List<QueueItemType>();

            public QueueItemType ItemType => _ItemType;
            public QueueItemState ItemState => _ItemState;
            public DateTime LastRunTime => _LastRunTime;
            public DateTime LastFinishTime => _LastFinishTime;
            public double LastRunDuration => _LastRunDuration;
            public DateTime NextRunTime 
            {
                get
                {
                    return LastRunTime.AddMinutes(Interval);
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

                                    Logging.Log(Logging.LogType.Debug, "Signature Import", "Processing TOSEC files");
                                    tIngest.Import(Path.Combine(Config.LibraryConfiguration.LibrarySignatureImportDirectory, "TOSEC"), gaseous_signature_parser.parser.SignatureParser.TOSEC);
                                    
                                    Logging.Log(Logging.LogType.Debug, "Signature Import", "Processing MAME Arcade files");
                                    tIngest.Import(Path.Combine(Config.LibraryConfiguration.LibrarySignatureImportDirectory, "MAME Arcade"), gaseous_signature_parser.parser.SignatureParser.MAMEArcade);

                                    Logging.Log(Logging.LogType.Debug, "Signature Import", "Processing MAME MESS files");
                                    tIngest.Import(Path.Combine(Config.LibraryConfiguration.LibrarySignatureImportDirectory, "MAME MESS"), gaseous_signature_parser.parser.SignatureParser.MAMEMess);
                                    
                                    _SaveLastRunTime = true;

                                    break;

                                case QueueItemType.TitleIngestor:
                                    Logging.Log(Logging.LogType.Debug, "Timered Event", "Starting Title Ingestor");
                                    Classes.ImportGames importGames = new Classes.ImportGames(Config.LibraryConfiguration.LibraryImportDirectory)
                                    {
                                        CallingQueueItem = this
                                    };

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
                                    Classes.ImportGame.OrganiseLibrary();

                                    _SaveLastRunTime = true;

                                    break;

                                case QueueItemType.LibraryScan:
                                    Logging.Log(Logging.LogType.Debug, "Timered Event", "Starting Library Scanners");
                                    Classes.ImportGame import = new ImportGame
                                    {
                                        CallingQueueItem = this
                                    };
                                    import.LibraryScan();

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

                                case QueueItemType.Rematcher:
                                    Logging.Log(Logging.LogType.Debug, "Timered Event", "Starting Rematch");
                                    Classes.ImportGame importRematch = new ImportGame
                                    {
                                        CallingQueueItem = this
                                    };
                                    importRematch.Rematcher(_ForceExecute);

                                    _SaveLastRunTime = true;

                                    break;

                                case QueueItemType.CollectionCompiler:
                                    Logging.Log(Logging.LogType.Debug, "Timered Event", "Starting Collection Compiler");
                                    Classes.Collections.CompileCollections((long)Options);
                                    break;

                                case QueueItemType.MediaGroupCompiler:
                                    Logging.Log(Logging.LogType.Debug, "Timered Event", "Starting Media Group Compiler");
                                    Classes.RomMediaGroup.CompileMediaGroup((long)Options);
                                    break;

                                case QueueItemType.BackgroundDatabaseUpgrade:
                                    Logging.Log(Logging.LogType.Debug, "Timered Event", "Starting Background Upgrade");
                                    DatabaseMigration.UpgradeScriptBackgroundTasks();
                                    break;

                                case QueueItemType.Maintainer:
                                    Logging.Log(Logging.LogType.Debug, "Timered Event", "Starting Maintenance");
                                    Classes.Maintenance maintenance = new Maintenance{
                                        CallingQueueItem = this
                                    };
                                    maintenance.RunMaintenance();
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
                        _ItemState = QueueItemState.Stopped;
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
            /// Looks for roms in the library that have an unknown platform or game match
            /// </summary>
            Rematcher,

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
            /// Performs a clean up of old files, and optimises the database
            /// </summary>
            Maintainer
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

