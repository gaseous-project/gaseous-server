using System;
using gaseous_tools;

namespace gaseous_server
{
	public static class ProcessQueue
	{
        public static List<QueueItem> QueueItems = new List<QueueItem>();

        public class QueueItem
        {
            public QueueItem(QueueItemType ItemType, int ExecutionInterval)
            {
                _ItemType = ItemType;
                _ItemState = QueueItemState.NeverStarted;
                _LastRunTime = DateTime.UtcNow.AddMinutes(ExecutionInterval);
                _Interval = ExecutionInterval;
            }

            private QueueItemType _ItemType = QueueItemType.NotConfigured;
            private QueueItemState _ItemState = QueueItemState.NeverStarted;
            private DateTime _LastRunTime = DateTime.UtcNow;
            private DateTime _LastFinishTime = DateTime.UtcNow;
            private int _Interval = 0;
            private string _LastResult = "";
            private Exception? _LastError = null;
            private bool _ForceExecute = false;

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
            public Exception? LastError => _LastError;
            public bool Force => _ForceExecute;

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
                        _ForceExecute = false;

                        Logging.Log(Logging.LogType.Information, "Timered Event", "Executing " + _ItemType);

                        try
                        {
                            switch (_ItemType)
                            {
                                case QueueItemType.SignatureIngestor:
                                    Logging.Log(Logging.LogType.Information, "Timered Event", "Starting Signature Ingestor");
                                    break;

                                case QueueItemType.TitleIngestor:
                                    Logging.Log(Logging.LogType.Information, "Timered Event", "Starting Title Ingestor");
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Logging.Log(Logging.LogType.Warning, "Timered Event", "An error occurred", ex);
                            _LastResult = "";
                            _LastError = ex;
                        }

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
            TitleIngestor
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

