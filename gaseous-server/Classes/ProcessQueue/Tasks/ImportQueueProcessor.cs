using gaseous_server.Classes;
using gaseous_server.Models;
using static gaseous_server.ProcessQueue.QueueProcessor.QueueItem;

namespace gaseous_server.ProcessQueue.Plugins
{
    /// <summary>
    /// Processes import queue items in the process queue.
    /// </summary>
    public class ImportQueueProcessor : ITaskPlugin
    {
        /// <inheritdoc/>
        public QueueItemType ItemType => QueueItemType.ImportQueueProcessor;

        /// <inheritdoc/>
        public object? Data { get; set; }

        /// <inheritdoc/>
        public required QueueProcessor.QueueItem ParentQueueItem { get; set; }

        /// <inheritdoc/>
        public async Task Execute()
        {
            Logging.LogKey(Logging.LogType.Debug, "process.timered_event", "timered_event.starting_import_queue_processor");

            if (ImportGame.ImportStates != null)
            {
                // get all pending imports
                List<ImportStateItem> pendingImports = ImportGame.ImportStates.Where(x => x.State == ImportStateItem.ImportState.Pending).ToList();

                // process each import
                foreach (ImportStateItem importState in pendingImports)
                {
                    // check the subtask list for any tasks with the same session id
                    SubTask? subTask = ParentQueueItem.SubTasks.FirstOrDefault(x => x.Settings is Guid && (Guid)x.Settings == importState.SessionId);
                    if (subTask == null)
                    {
                        // process the import
                        Logging.LogKey(Logging.LogType.Information, "process.import_queue_processor", "importqueue.processing_import", null, new[] { importState.FileName });
                        ParentQueueItem.AddSubTask(QueueItemSubTasks.ImportQueueProcessor, Path.GetFileName(importState.FileName), importState.SessionId, true);
                        // update the import state
                        ImportGame.UpdateImportState(importState.SessionId, ImportStateItem.ImportState.Queued, ImportStateItem.ImportType.Unknown, null);
                    }
                }

                // check for queued imports that have stalled (don't have a related sub task)
                List<ImportStateItem> stalledImports = ImportGame.ImportStates.Where(x => x.State == ImportStateItem.ImportState.Queued).ToList();
                foreach (ImportStateItem importState in stalledImports)
                {
                    // check the subtask list for any tasks with the same session id
                    SubTask? subTask = ParentQueueItem.SubTasks.FirstOrDefault(x => x.Settings is Guid && (Guid)x.Settings == importState.SessionId);
                    if (subTask == null)
                    {
                        // process the import
                        Logging.LogKey(Logging.LogType.Warning, "process.import_queue_processor", "importqueue.requeuing_stalled_import", null, new[] { importState.FileName });
                        ParentQueueItem.AddSubTask(QueueItemSubTasks.ImportQueueProcessor, Path.GetFileName(importState.FileName), importState.SessionId, true);
                    }
                }
            }

            // clean up
            Classes.ImportGame.DeleteOrphanedDirectories(Config.LibraryConfiguration.LibraryImportDirectory);
            Classes.ImportGame.RemoveOldImportStates();

            // force execute this task again so the user doesn't have to wait for imports to be processed
            ParentQueueItem.LastRunTime = DateTime.UtcNow.AddMinutes(-ParentQueueItem.Interval);
        }
    }
}