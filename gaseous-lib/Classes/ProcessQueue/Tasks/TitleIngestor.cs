using gaseous_server.Classes;

namespace gaseous_server.ProcessQueue.Plugins
{
    /// <summary>
    /// Represents a plugin that ingests titles into the process queue.
    /// </summary>
    public class TitleIngestor : ITaskPlugin
    {
        /// <inheritdoc/>
        public QueueItemType ItemType => QueueItemType.TitleIngestor;

        /// <inheritdoc/>
        public object? Data { get; set; }

        /// <inheritdoc/>
        public required QueueProcessor.QueueItem ParentQueueItem { get; set; }

        /// <inheritdoc/>
        public async Task Execute()
        {
            Logging.LogKey(Logging.LogType.Debug, "process.timered_event", "timered_event.starting_title_ingestor");
            Classes.ImportGame import = new ImportGame
            {
                CallingQueueItem = ParentQueueItem
            };
            import.ProcessDirectory(Config.LibraryConfiguration.LibraryImportDirectory);
        }
    }
}