using gaseous_server.Classes;

namespace gaseous_server.ProcessQueue.Plugins
{
    /// <summary>
    /// Represents a plugin that compiles media groups in the process queue.
    /// </summary>
    public class MediaGroupCompiler : ITaskPlugin
    {
        /// <inheritdoc/>
        public QueueItemType ItemType => QueueItemType.MediaGroupCompiler;

        /// <inheritdoc/>
        public object? Data { get; set; }

        /// <inheritdoc/>
        public required QueueProcessor.QueueItem ParentQueueItem { get; set; }

        /// <inheritdoc/>
        public async Task Execute()
        {
            Logging.LogKey(Logging.LogType.Debug, "process.timered_event", "timered_event.starting_media_group_compiler");
            if (ParentQueueItem.Options != null)
            {
                await Classes.RomMediaGroup.CompileMediaGroup((long)ParentQueueItem.Options);
            }
        }
    }
}