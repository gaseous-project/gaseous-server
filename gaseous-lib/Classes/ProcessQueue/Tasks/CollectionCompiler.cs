using gaseous_server.Classes;

namespace gaseous_server.ProcessQueue.Plugins
{
    /// <summary>
    /// Represents a plugin that compiles collections as part of the process queue.
    /// </summary>
    public class CollectionCompiler : ITaskPlugin
    {
        /// <inheritdoc/>
        public QueueItemType ItemType => QueueItemType.CollectionCompiler;

        /// <inheritdoc/>
        public object? Data { get; set; }

        /// <inheritdoc/>
        public required QueueProcessor.QueueItem ParentQueueItem { get; set; }

        /// <inheritdoc/>
        public async Task Execute()
        {
            Logging.LogKey(Logging.LogType.Debug, "process.timered_event", "timered_event.starting_collection_compiler");
            if (ParentQueueItem.Options != null)
            {
                Dictionary<string, object> collectionOptions = (Dictionary<string, object>)ParentQueueItem.Options;
                // Classes.Collections.CompileCollections((long)collectionOptions["Id"], (string)collectionOptions["UserId"]);
            }
        }
    }
}