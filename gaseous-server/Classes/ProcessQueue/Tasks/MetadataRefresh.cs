using gaseous_server.Classes;

namespace gaseous_server.ProcessQueue.Plugins
{
    /// <summary>
    /// Task plugin for refreshing metadata in the process queue.
    /// </summary>
    public class MetadataRefresh : ITaskPlugin
    {
        /// <inheritdoc/>
        public QueueItemType ItemType => QueueItemType.MetadataRefresh;

        /// <inheritdoc/>
        public object? Data { get; set; }

        /// <inheritdoc/>
        public required QueueProcessor.QueueItem ParentQueueItem { get; set; }

        /// <inheritdoc/>
        public async Task Execute()
        {
            Logging.LogKey(Logging.LogType.Debug, "process.timered_event", "timered_event.starting_metadata_refresher");

            // clear the sub tasks
            if (ParentQueueItem.SubTasks != null)
            {
                ParentQueueItem.SubTasks.Clear();
            }
            else
            {
                ParentQueueItem.SubTasks = new List<QueueProcessor.QueueItem.SubTask>();
            }

            // set up metadata refresh subtasks
            ParentQueueItem.AddSubTask(QueueItemSubTasks.MetadataRefresh_Platform, "Platform Metadata", null, true);
            ParentQueueItem.AddSubTask(QueueItemSubTasks.MetadataRefresh_Signatures, "Signature Metadata", null, true);
            ParentQueueItem.AddSubTask(QueueItemSubTasks.MetadataRefresh_Game, "Game Metadata", null, true);
        }
    }
}