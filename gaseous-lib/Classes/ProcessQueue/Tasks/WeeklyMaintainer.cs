using gaseous_server.Classes;

namespace gaseous_server.ProcessQueue.Plugins
{
    /// <summary>
    /// Represents a plugin that performs weekly maintenance tasks in the process queue.
    /// </summary>
    public class WeeklyMaintainer : ITaskPlugin
    {
        /// <inheritdoc/>
        public QueueItemType ItemType => QueueItemType.WeeklyMaintainer;

        /// <inheritdoc/>
        public object? Data { get; set; }

        /// <inheritdoc/>
        public required QueueProcessor.QueueItem ParentQueueItem { get; set; }

        /// <inheritdoc/>
        public async Task Execute()
        {
            Logging.LogKey(Logging.LogType.Debug, "process.timered_event", "timered_event.starting_weekly_maintenance");
            Classes.Maintenance weeklyMaintenance = new Maintenance
            {
                CallingQueueItem = ParentQueueItem
            };
            await weeklyMaintenance.RunWeeklyMaintenance();
        }
    }
}