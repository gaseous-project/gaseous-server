using gaseous_server.Classes;

namespace gaseous_server.ProcessQueue.Plugins
{
    /// <summary>
    /// Represents a plugin that performs daily maintenance tasks in the process queue.
    /// </summary>
    public class DailyMaintainer : ITaskPlugin
    {
        /// <inheritdoc/>
        public QueueItemType ItemType => QueueItemType.DailyMaintainer;

        /// <inheritdoc/>
        public object? Data { get; set; }

        /// <inheritdoc/>
        public required QueueProcessor.QueueItem ParentQueueItem { get; set; }

        /// <inheritdoc/>
        public async Task Execute()
        {
            Logging.LogKey(Logging.LogType.Debug, "process.timered_event", "timered_event.starting_daily_maintenance");
            Classes.Maintenance maintenance = new Maintenance
            {
                CallingQueueItem = ParentQueueItem
            };
            await maintenance.RunDailyMaintenance();
        }
    }
}