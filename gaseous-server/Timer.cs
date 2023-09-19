using System;
using gaseous_tools;

namespace gaseous_server
{
    // see: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-5.0&tabs=visual-studio-mac#timed-background-tasks-1
    public class TimedHostedService : IHostedService, IDisposable
    {
        private int executionCount = 0;
        //private readonly ILogger<TimedHostedService> _logger;
        private Timer _timer;

        //public TimedHostedService(ILogger<TimedHostedService> logger)
        //{
        //    _logger = logger;
        //}

        public Task StartAsync(CancellationToken stoppingToken)
        {
            //_logger.LogInformation("Timed Hosted Service running.");
            Logging.Log(Logging.LogType.Debug, "Background", "Starting background task monitor");

            _timer = new Timer(DoWork, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(5));

            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            var count = Interlocked.Increment(ref executionCount);

            //_logger.LogInformation(
            //    "Timed Hosted Service is working. Count: {Count}", count);

            List<ProcessQueue.QueueItem> ActiveList = new List<ProcessQueue.QueueItem>();
            ActiveList.AddRange(ProcessQueue.QueueItems);
            foreach (ProcessQueue.QueueItem qi in ActiveList) {
                if ((DateTime.UtcNow > qi.NextRunTime || qi.Force == true) && CheckProcessBlockList(qi) == true) {
                    qi.Execute();
                    if (qi.RemoveWhenStopped == true && qi.ItemState == ProcessQueue.QueueItemState.Stopped)
                    {
                        ProcessQueue.QueueItems.Remove(qi);
                    }
                }
            }
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            //_logger.LogInformation("Timed Hosted Service is stopping.");
            Logging.Log(Logging.LogType.Debug, "Background", "Stopping background task monitor");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        private bool CheckProcessBlockList(ProcessQueue.QueueItem queueItem)
        {
            if (queueItem.Blocks.Count > 0)
            {
                foreach (ProcessQueue.QueueItem qi in ProcessQueue.QueueItems)
                {
                    if (queueItem.Blocks.Contains(qi.ItemType) && qi.ItemState == ProcessQueue.QueueItemState.Running)
                    {
                        return false;
                    }
                }

                return true;
            }
            else
            {
                return true;
            }
        }
    }
}

