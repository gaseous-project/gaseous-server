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

        /// <summary>
        /// Subtask for refreshing signature metadata.
        /// </summary>
        public class SubTaskSignatureRefresh : ITaskPlugin.ISubTaskItem
        {
            /// <inheritdoc/>
            public QueueItemSubTasks SubTaskType => QueueItemSubTasks.MetadataRefresh_Signatures;

            /// <inheritdoc/>
            public object? Data { get; set; }

            /// <inheritdoc/>
            public required QueueProcessor.QueueItem.SubTask ParentSubTaskItem { get; set; }

            /// <inheritdoc/>
            public async Task Execute()
            {
                Logging.LogKey(Logging.LogType.Information, "process.metadata_refresh", "metadatarefresh.refreshing_signature_metadata_for", null, new[] { ParentSubTaskItem.TaskName });
                MetadataManagement metadataSignatures = new MetadataManagement(ParentSubTaskItem);
                await metadataSignatures.RefreshSignatures(true);
            }
        }

        /// <summary>
        /// Subtask for refreshing platform metadata.
        /// </summary>
        public class SubTaskPlatformRefresh : ITaskPlugin.ISubTaskItem
        {
            /// <inheritdoc/>
            public QueueItemSubTasks SubTaskType => QueueItemSubTasks.MetadataRefresh_Platform;

            /// <inheritdoc/>
            public object? Data { get; set; }

            /// <inheritdoc/>
            public required QueueProcessor.QueueItem.SubTask ParentSubTaskItem { get; set; }

            /// <inheritdoc/>
            public async Task Execute()
            {
                Logging.LogKey(Logging.LogType.Information, "process.metadata_refresh", "metadatarefresh.refreshing_platform_metadata_for", null, new[] { ParentSubTaskItem.TaskName });
                MetadataManagement metadataPlatform = new MetadataManagement(ParentSubTaskItem);
                await metadataPlatform.RefreshPlatforms(true);
            }
        }

        /// <summary>
        /// Subtask for refreshing game metadata.
        /// </summary>
        public class SubTaskGameRefresh : ITaskPlugin.ISubTaskItem
        {
            /// <inheritdoc/>
            public QueueItemSubTasks SubTaskType => QueueItemSubTasks.MetadataRefresh_Game;

            /// <inheritdoc/>
            public object? Data { get; set; }

            /// <inheritdoc/>
            public required QueueProcessor.QueueItem.SubTask ParentSubTaskItem { get; set; }

            /// <inheritdoc/>
            public async Task Execute()
            {
                Logging.LogKey(Logging.LogType.Information, "process.metadata_refresh", "metadatarefresh.refreshing_game_metadata_for", null, new[] { ParentSubTaskItem.TaskName });
                MetadataManagement metadataGame = new MetadataManagement(ParentSubTaskItem);
                metadataGame.UpdateRomCounts();
                await metadataGame.RefreshGames(true);
            }
        }
    }
}