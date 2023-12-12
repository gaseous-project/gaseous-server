namespace gaseous_server.Classes
{
    public class QueueItemStatus
    {
        internal ProcessQueue.QueueItem? CallingQueueItem = null;

        private int _CurrentItemNumber = 0;
        private int _MaxItemsNumber = 0;
        private string _StatusText = "";

        public int CurrentItemNumber => _CurrentItemNumber;
        public int MaxItemsNumber => _MaxItemsNumber;
        public string StatusText => _StatusText;

        public void SetStatus(int CurrentItemNumber, int MaxItemsNumber, string StatusText)
        {
            this._CurrentItemNumber = CurrentItemNumber;
            this._MaxItemsNumber = MaxItemsNumber;
            this._StatusText = StatusText;

            if (CallingQueueItem != null)
            {
                CallingQueueItem.CurrentState = _CurrentItemNumber + " of " + _MaxItemsNumber + ": " + _StatusText;
                CallingQueueItem.CurrentStateProgress = _CurrentItemNumber + " of " + _MaxItemsNumber;
            }
        }

        public void ClearStatus()
        {
            this._CurrentItemNumber = 0;
            this._MaxItemsNumber = 0;
            this._StatusText = "";

            if (CallingQueueItem != null)
            {
                CallingQueueItem.CurrentState = "";
                CallingQueueItem.CurrentStateProgress = "";
            }
        }
    }
}