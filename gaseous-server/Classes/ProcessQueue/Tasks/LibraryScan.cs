using gaseous_server.Classes;

namespace gaseous_server.ProcessQueue.Plugins
{
    /// <summary>
    /// Represents a plugin task for scanning game libraries in the process queue.
    /// </summary>
    public class LibraryScan : ITaskPlugin
    {
        /// <inheritdoc/>
        public QueueItemType ItemType => QueueItemType.LibraryScan;

        /// <inheritdoc/>
        public object? Data { get; set; }

        /// <inheritdoc/>
        public required QueueProcessor.QueueItem ParentQueueItem { get; set; }

        /// <inheritdoc/>
        public async Task Execute()
        {
            Logging.LogKey(Logging.LogType.Debug, "process.timered_event", "timered_event.starting_library_scanners");

            // get all libraries
            if (ParentQueueItem.SubTasks == null || ParentQueueItem.SubTasks.Count == 0)
            {
                List<GameLibrary.LibraryItem> libraries = await GameLibrary.GetLibraries();

                // process each library
                foreach (GameLibrary.LibraryItem library in libraries)
                {
                    Guid childCorrelationId = ParentQueueItem.AddSubTask(QueueItemSubTasks.LibraryScanWorker, library.Name, library, true);
                    Logging.LogKey(Logging.LogType.Information, "process.library_scan", "libraryscan.queuing_library_for_scanning_with_correlation_id", null, new[] { library.Name, childCorrelationId.ToString() });
                }
            }
        }
    }
}