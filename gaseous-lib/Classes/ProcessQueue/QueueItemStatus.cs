namespace gaseous_server.Classes
{
    public class QueueItemStatus
    {
        internal object? CallingQueueItem = null;

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

            SetCallingItemState(_CurrentItemNumber + " of " + _MaxItemsNumber + ": " + _StatusText, _CurrentItemNumber + " of " + _MaxItemsNumber);
        }

        public void ClearStatus()
        {
            this._CurrentItemNumber = 0;
            this._MaxItemsNumber = 0;
            this._StatusText = "";

            // SetCallingItemState("", "");
        }

        private void SetCallingItemState(string state, string progress)
        {
            if (CallingQueueItem != null)
            {
                // get object type of CallingQueueItem
                string type = CallingQueueItem.GetType().ToString();

                // check if type is QueueItem
                switch (type)
                {
                    case "gaseous_server.ProcessQueue.QueueProcessor+QueueItem":
                        // set CallingQueueItem to QueueItem
                        ProcessQueue.QueueProcessor.QueueItem callingQueueItem = (ProcessQueue.QueueProcessor.QueueItem)CallingQueueItem;
                        callingQueueItem.CurrentState = state;
                        callingQueueItem.CurrentStateProgress = progress;
                        break;
                    case "gaseous_server.ProcessQueue.QueueProcessor+QueueItem+SubTask":
                        // set CallingQueueItem to QueueItem.SubTask
                        ProcessQueue.QueueProcessor.QueueItem.SubTask callingSubTask = (ProcessQueue.QueueProcessor.QueueItem.SubTask)CallingQueueItem;
                        callingSubTask.CurrentState = state;
                        callingSubTask.CurrentStateProgress = progress;
                        break;
                }

                SendStatusToReportingServer(CallingQueueItem.GetType(), state, progress);
            }
        }

        private void SendStatusToReportingServer(Type type, string state, string progress)
        {
            var outProcessData = CallContext.GetData("OutProcess");
            if (CallingQueueItem != null && outProcessData != null && bool.TryParse(outProcessData.ToString(), out bool isOutProcess) && isOutProcess)
            {
                // structure data for reporting server
                Dictionary<string, string> data = new Dictionary<string, string>
                {
                    { "CurrentState", state },
                    { "CurrentStateProgress", progress }
                };

                string url = "";
                if (type == typeof(ProcessQueue.QueueProcessor.QueueItem))
                {
                    url = $"/api/v1.1/BackgroundTasks/{((ProcessQueue.QueueProcessor.QueueItem)CallingQueueItem).CorrelationId}/";
                }
                else if (type == typeof(ProcessQueue.QueueProcessor.QueueItem.SubTask))
                {
                    url = $"/api/v1.1/BackgroundTasks/{((ProcessQueue.QueueProcessor.QueueItem.SubTask)CallingQueueItem).ParentCorrelationId}/SubTask/{((ProcessQueue.QueueProcessor.QueueItem.SubTask)CallingQueueItem).CorrelationId}/";
                }

                string jsonOutput = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                var reportingServerUrlData = CallContext.GetData("ReportingServerUrl");
                if (reportingServerUrlData != null)
                {
                    string reportingServerUrl = reportingServerUrlData.ToString();
                    if (!string.IsNullOrEmpty(reportingServerUrl))
                    {
                        // send the data to the reporting server
                        try
                        {
                            using (var client = new System.Net.Http.HttpClient())
                            {
                                var content = new System.Net.Http.StringContent(jsonOutput, System.Text.Encoding.UTF8, "application/json");

                                string sendUrl = $"{reportingServerUrl}{url}";

                                var response = client.PutAsync(sendUrl, content).Result;
                            }
                        }
                        catch (Exception ex)
                        {
                            // swallow the error
                            Console.WriteLine(ex.ToString());
                        }
                    }
                }
            }
        }
    }
}