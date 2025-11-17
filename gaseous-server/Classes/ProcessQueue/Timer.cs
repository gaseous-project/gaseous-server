using System;
using gaseous_server.Classes;

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
            Logging.LogKey(Logging.LogType.Debug, "process.background", "background.starting_background_task_monitor");

            _timer = new Timer(DoWork, null, TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(5));

            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            var count = Interlocked.Increment(ref executionCount);

            // don't execute if the server first run state is not up to date
            if (Config.FirstRunStatus != Config.FirstRunStatusWhenSet)
            {
                return;
            }

            // check if a database upgrade process is in the queue
            ProcessQueue.QueueProcessor.QueueItemState[] upgradeStates = new ProcessQueue.QueueProcessor.QueueItemState[]
            {
                ProcessQueue.QueueProcessor.QueueItemState.Running,
                ProcessQueue.QueueProcessor.QueueItemState.Stopped,
                ProcessQueue.QueueProcessor.QueueItemState.NeverStarted
            };
            if (ProcessQueue.QueueProcessor.QueueItems.Any(qi => qi.ItemType == ProcessQueue.QueueItemType.BackgroundDatabaseUpgrade && upgradeStates.Contains(qi.ItemState)))
            {
                Config.DatabaseConfiguration.UpgradeInProgress = true;
            }
            else
            {
                Config.DatabaseConfiguration.UpgradeInProgress = false;
            }

            List<ProcessQueue.QueueProcessor.QueueItem> ActiveList = new List<ProcessQueue.QueueProcessor.QueueItem>();
            ActiveList.AddRange(ProcessQueue.QueueProcessor.QueueItems);
            foreach (ProcessQueue.QueueProcessor.QueueItem qi in ActiveList)
            {
                if (Config.DatabaseConfiguration.UpgradeInProgress == false || (Config.DatabaseConfiguration.UpgradeInProgress == true && qi.ItemType == ProcessQueue.QueueItemType.BackgroundDatabaseUpgrade))
                {
                    if (qi.ItemState != ProcessQueue.QueueProcessor.QueueItemState.Disabled)
                    {
                        if (CheckIfProcessIsBlockedByOthers(qi) == false)
                        {
                            qi.BlockedState(false);
                            if (DateTime.UtcNow > qi.NextRunTime || qi.Force == true)
                            {
                                // execute queued process
                                _ = Task.Run(() => qi.Execute());

                                if (qi.RemoveWhenStopped == true && qi.ItemState == ProcessQueue.QueueProcessor.QueueItemState.Stopped)
                                {
                                    ProcessQueue.QueueProcessor.QueueItems.Remove(qi);
                                }
                            }
                        }
                        else
                        {
                            qi.BlockedState(true);
                        }
                    }
                }
            }
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            //_logger.LogInformation("Timed Hosted Service is stopping.");
            Logging.LogKey(Logging.LogType.Debug, "process.background", "background.stopping_background_task_monitor");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        private bool CheckIfProcessIsBlockedByOthers(ProcessQueue.QueueProcessor.QueueItem queueItem)
        {
            foreach (ProcessQueue.QueueProcessor.QueueItem qi in ProcessQueue.QueueProcessor.QueueItems)
            {
                if (qi.ItemState == ProcessQueue.QueueProcessor.QueueItemState.Running)
                {
                    // other service is running, check if queueItem is blocked by it
                    if (
                        qi.Blocks.Contains(queueItem.ItemType) ||
                        qi.Blocks.Contains(ProcessQueue.QueueItemType.All)
                    )
                    {
                        //Console.WriteLine(queueItem.ItemType.ToString() + " is blocked by " + qi.ItemType.ToString());
                        return true;
                    }
                }
            }

            return false;
        }
    }
}

