using gaseous_server.Classes;
using gaseous_server.Classes.Metadata;
using gaseous_server.Models;
using HasheousClient.Models.Metadata.IGDB;
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

        /// <summary>
        /// Represents a subtask item for the ImportQueueProcessor.
        /// </summary>
        public class SubTaskItem : ITaskPlugin.ISubTaskItem
        {
            /// <inheritdoc/>
            public QueueItemSubTasks SubTaskType => QueueItemSubTasks.ImportQueueProcessor;

            /// <inheritdoc/>
            public object? Data { get; set; }

            /// <inheritdoc/>
            public required QueueProcessor.QueueItem.SubTask ParentSubTaskItem { get; set; }

            /// <inheritdoc/>
            public async Task Execute()
            {
                Logging.LogKey(Logging.LogType.Information, "process.import_queue_processor", "importqueue.processing_import", null, new[] { ParentSubTaskItem.TaskName });

                // update the import state
                ImportGame.UpdateImportState((Guid)ParentSubTaskItem.Settings, ImportStateItem.ImportState.Processing, ImportStateItem.ImportType.Unknown, null);

                ImportStateItem importState = ImportGame.GetImportState((Guid)ParentSubTaskItem.Settings);
                if (importState != null)
                {
                    Dictionary<string, object>? ProcessData = new Dictionary<string, object>();
                    ProcessData.Add("path", Path.GetFileName(importState.FileName));
                    ProcessData.Add("sessionid", importState.SessionId.ToString());

                    // get the hash of the file
                    HashObject hash = new HashObject(importState.FileName);
                    ProcessData.Add("md5hash", hash.md5hash);
                    ProcessData.Add("sha1hash", hash.sha1hash);
                    ProcessData.Add("crc32hash", hash.crc32hash);

                    // check if the file is a bios file first
                    Models.PlatformMapping.PlatformMapItem? IsBios = Classes.Bios.BiosHashSignatureLookup(hash.md5hash);

                    if (IsBios != null)
                    {
                        // file is a bios
                        Bios.ImportBiosFile(importState.FileName, hash, ref ProcessData);

                        ImportGame.UpdateImportState((Guid)ParentSubTaskItem.Settings, ImportStateItem.ImportState.Completed, ImportStateItem.ImportType.BIOS, ProcessData);
                    }
                    else if (
                        Common.SkippableFiles.Contains<string>(Path.GetFileName(importState.FileName), StringComparer.OrdinalIgnoreCase) ||
                        !PlatformMapping.SupportedFileExtensions.Contains(Path.GetExtension(importState.FileName), StringComparer.OrdinalIgnoreCase)
                    )
                    {
                        Logging.LogKey(Logging.LogType.Debug, "process.import_game", "importqueue.skipping_item_not_supported", null, new[] { importState.FileName });

                        // move the file to the errors directory
                        string targetPathWithFileName = importState.FileName.Replace(Config.LibraryConfiguration.LibraryImportDirectory, Config.LibraryConfiguration.LibraryImportErrorDirectory);

                        // create target directory if it doesn't exist
                        if (!Directory.Exists(Path.GetDirectoryName(targetPathWithFileName)!))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(targetPathWithFileName)!);
                        }

                        File.Move(importState.FileName, targetPathWithFileName, true);

                        ProcessData.Add("type", "skipped");
                        ProcessData.Add("status", "skipped");
                        ImportGame.UpdateImportState((Guid)ParentSubTaskItem.Settings, ImportStateItem.ImportState.Completed, ImportStateItem.ImportType.Unknown, ProcessData);
                    }
                    else
                    {
                        // file is a rom
                        Platform? platformOverride = null;
                        if (importState.PlatformOverride != null)
                        {
                            platformOverride = await Platforms.GetPlatform((long)importState.PlatformOverride);
                        }
                        ImportGame.ImportGameFile(importState.FileName, hash, ref ProcessData, platformOverride);

                        ImportGame.UpdateImportState((Guid)ParentSubTaskItem.Settings, ImportStateItem.ImportState.Processing, ImportStateItem.ImportType.Rom, ProcessData);

                        // refresh the metadata for the game - this is a task that can run in the background
                        ParentSubTaskItem.AllowConcurrentExecution = true;
                        if (ProcessData.ContainsKey("metadatamapid"))
                        {
                            long? metadataMapId = (long?)ProcessData["metadatamapid"];
                            if (metadataMapId != null)
                            {
                                MetadataManagement metadataManagement = new MetadataManagement();
                                await metadataManagement.RefreshSpecificGameAsync((long)metadataMapId);
                            }
                        }
                        ImportGame.UpdateImportState((Guid)ParentSubTaskItem.Settings, ImportStateItem.ImportState.Completed, ImportStateItem.ImportType.Rom, ProcessData);
                    }
                }
                else
                {
                    Logging.LogKey(Logging.LogType.Warning, "process.import_queue_processor", "importqueue.import_not_found", null, new[] { ParentSubTaskItem.TaskName });
                }
            }
        }
    }
}