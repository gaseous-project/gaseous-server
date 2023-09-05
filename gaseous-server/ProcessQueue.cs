﻿using System;
using gaseous_tools;

namespace gaseous_server
{
	public static class ProcessQueue
	{
        public static List<QueueItem> QueueItems = new List<QueueItem>();

        public class QueueItem
        {
            public QueueItem(QueueItemType ItemType, int ExecutionInterval, bool AllowManualStart = true)
            {
                _ItemType = ItemType;
                _ItemState = QueueItemState.NeverStarted;
                _LastRunTime = DateTime.UtcNow.AddMinutes(ExecutionInterval);
                _Interval = ExecutionInterval;
                _AllowManualStart = AllowManualStart;
            }

            public QueueItem(QueueItemType ItemType, int ExecutionInterval, List<QueueItemType> Blocks, bool AllowManualStart = true)
            {
                _ItemType = ItemType;
                _ItemState = QueueItemState.NeverStarted;
                _LastRunTime = DateTime.UtcNow.AddMinutes(ExecutionInterval);
                _Interval = ExecutionInterval;
                _AllowManualStart = AllowManualStart;
                _Blocks = Blocks;
            }

            private QueueItemType _ItemType = QueueItemType.NotConfigured;
            private QueueItemState _ItemState = QueueItemState.NeverStarted;
            private DateTime _LastRunTime = DateTime.UtcNow;
            private DateTime _LastFinishTime = DateTime.UtcNow;
            private int _Interval = 0;
            private string _LastResult = "";
            private string? _LastError = null;
            private bool _ForceExecute = false;
            private bool _AllowManualStart = true;
            private List<QueueItemType> _Blocks = new List<QueueItemType>();

            public QueueItemType ItemType => _ItemType;
            public QueueItemState ItemState => _ItemState;
            public DateTime LastRunTime => _LastRunTime;
            public DateTime LastFinishTime => _LastFinishTime;
            public DateTime NextRunTime {
                get
                {
                    return LastRunTime.AddMinutes(Interval);
                }
            }
            public int Interval => _Interval;
            public string LastResult => _LastResult;
            public string? LastError => _LastError;
            public bool Force => _ForceExecute;
            public bool AllowManualStart => _AllowManualStart;
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

                        Logging.Log(Logging.LogType.Information, "Timered Event", "Executing " + _ItemType);

                        try
                        {
                            switch (_ItemType)
                            {
                                case QueueItemType.SignatureIngestor:
                                    Logging.Log(Logging.LogType.Information, "Timered Event", "Starting Signature Ingestor");
                                    SignatureIngestors.XML.XMLIngestor tIngest = new SignatureIngestors.XML.XMLIngestor();
                                    
                                    Logging.Log(Logging.LogType.Information, "Signature Import", "Processing TOSEC files");
                                    tIngest.Import(Path.Combine(Config.LibraryConfiguration.LibrarySignatureImportDirectory, "TOSEC"), gaseous_signature_parser.parser.SignatureParser.TOSEC);
                                    
                                    Logging.Log(Logging.LogType.Information, "Signature Import", "Processing MAME Arcade files");
                                    tIngest.Import(Path.Combine(Config.LibraryConfiguration.LibrarySignatureImportDirectory, "MAME Arcade"), gaseous_signature_parser.parser.SignatureParser.MAMEArcade);

                                    Logging.Log(Logging.LogType.Information, "Signature Import", "Processing MAME MESS files");
                                    tIngest.Import(Path.Combine(Config.LibraryConfiguration.LibrarySignatureImportDirectory, "MAME MESS"), gaseous_signature_parser.parser.SignatureParser.MAMEMess);
                                    
                                    break;

                                case QueueItemType.TitleIngestor:
                                    Logging.Log(Logging.LogType.Information, "Timered Event", "Starting Title Ingestor");
                                    Classes.ImportGames importGames = new Classes.ImportGames(Config.LibraryConfiguration.LibraryImportDirectory);
                                    break;

                                case QueueItemType.MetadataRefresh:
                                    Logging.Log(Logging.LogType.Information, "Timered Event", "Starting Metadata Refresher");
                                    Classes.MetadataManagement.RefreshMetadata(true);
                                    break;

                                case QueueItemType.OrganiseLibrary:
                                    Logging.Log(Logging.LogType.Information, "Timered Event", "Starting Library Organiser");
                                    Classes.ImportGame.OrganiseLibrary();
                                    break;

                                case QueueItemType.LibraryScan:
                                    Logging.Log(Logging.LogType.Information, "Timered Event", "Starting Library Scanner");
                                    Classes.ImportGame.LibraryScan();
                                    break;

                                case QueueItemType.CollectionCompiler:
                                    Logging.Log(Logging.LogType.Information, "Timered Event", "Starting Collection Compiler");
                                    Classes.Collections.CompileCollections();
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
                    }
                }
            }

            public void ForceExecute()
            {
                _ForceExecute = true;
            }
        }

        public enum QueueItemType
        {
            NotConfigured,
            SignatureIngestor,
            TitleIngestor,
            MetadataRefresh,
            OrganiseLibrary,
            LibraryScan,
            CollectionCompiler
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

