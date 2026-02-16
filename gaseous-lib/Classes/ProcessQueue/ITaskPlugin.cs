namespace gaseous_server.ProcessQueue.Plugins
{
    /// <summary>
    /// Defines the contract for a task plugin used in the process queue.
    /// </summary>
    public interface ITaskPlugin
    {
        /// <summary>
        /// Gets the type of plugin.
        /// </summary>
        public string PluginType => "Task";

        /// <summary>
        /// Gets the type of the queue item associated with the plugin.
        /// </summary>
        public gaseous_server.ProcessQueue.QueueItemType ItemType { get; }

        /// <summary>
        /// Gets or sets the data associated with the plugin.
        /// </summary>
        public object? Data { get; set; }

        /// <summary>
        /// Sets the queue item associated with the plugin.
        /// </summary>
        /// <remarks>This allows for cases where the action needs to update the queue item.</remarks>
        public gaseous_server.ProcessQueue.QueueProcessor.QueueItem ParentQueueItem { get; set; }

        /// <summary>
        /// Executes the plugin task asynchronously.
        /// </summary>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public Task Execute();

        /// <summary>
        /// Defines the contract for a subtask item used within a task plugin.
        /// </summary>
        public interface ISubTaskItem
        {
            /// <summary>
            /// Gets the type of subtask.
            /// </summary>
            public gaseous_server.ProcessQueue.QueueItemSubTasks SubTaskType { get; }

            /// <summary>
            /// Gets or sets the data associated with the subtask.
            /// </summary>
            public object? Data { get; set; }

            /// <summary>
            /// Gets or sets the parent task plugin associated with this subtask item.
            /// </summary>
            public QueueProcessor.QueueItem.SubTask ParentSubTaskItem { get; set; }

            /// <summary>
            /// Executes the subtask asynchronously.
            /// </summary>
            /// <returns>A Task representing the asynchronous operation.</returns>
            public Task Execute();
        }
    }
}