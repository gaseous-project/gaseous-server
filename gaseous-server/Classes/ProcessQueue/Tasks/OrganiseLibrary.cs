using gaseous_server.Classes;

namespace gaseous_server.ProcessQueue.Plugins
{
    /// <summary>
    /// Represents a plugin task that organises the game library in the process queue.
    /// </summary>
    public class OrganiseLibrary : ITaskPlugin
    {
        /// <inheritdoc/>
        public QueueItemType ItemType => QueueItemType.OrganiseLibrary;

        /// <inheritdoc/>
        public object? Data { get; set; }

        /// <inheritdoc/>
        public required QueueProcessor.QueueItem ParentQueueItem { get; set; }

        /// <inheritdoc/>
        public async Task Execute()
        {
            Logging.LogKey(Logging.LogType.Debug, "process.timered_event", "timered_event.starting_library_organiser");
            Classes.ImportGame importLibraryOrg = new ImportGame
            {
                CallingQueueItem = this
            };
            await importLibraryOrg.OrganiseLibrary();
        }
    }
}