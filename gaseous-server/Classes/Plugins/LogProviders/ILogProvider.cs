namespace gaseous_server.Classes.Plugins.LogProviders
{
    /// <summary>
    /// Interface for log provider plugins.
    /// All log provider plugins must implement this interface.
    /// </summary>
    public interface ILogProvider
    {
        /// <summary>
        /// Gets the type of plugin.
        /// </summary>
        public gaseous_server.Classes.Plugins.PluginManagement.PluginTypes PluginType => gaseous_server.Classes.Plugins.PluginManagement.PluginTypes.LogProvider;

        /// <summary>
        /// Gets the name of the plugin.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the list of operating systems supported by the log provider.
        /// </summary>
        public List<gaseous_server.Classes.Plugins.PluginManagement.OperatingSystems> SupportedOperatingSystems { get; }

        /// <summary>
        /// Gets a value indicating whether the log provider supports fetching logs.
        /// </summary>
        public bool SupportsLogFetch { get; }

        /// <summary>
        /// Gets or sets the configuration settings for the log provider plugin.
        /// </summary>
        public Dictionary<string, object>? Settings { get; set; }

        /// <summary>
        /// Logs a message using the log provider.
        /// </summary>
        /// <param name="logItem">The log item to be logged.</param>
        /// <returns>A task that represents the asynchronous logging operation. The task result contains a boolean indicating whether the logging was successful.</returns>
        public Task<bool> LogMessage(Logging.LogItem logItem);

        /// <summary>
        /// Logs a message using the log provider.
        /// </summary>
        /// <param name="logItem">The log item to be logged.</param>
        /// <param name="exception">An optional exception associated with the log item. Used for when a log provider encounters an error.</param>
        /// <returns>A task that represents the asynchronous logging operation. The task result contains a boolean indicating whether the logging was successful.</returns>
        public Task<bool> LogMessage(Logging.LogItem logItem, Exception? exception);

        /// <summary>
        /// Runs maintenance tasks for the log provider.
        /// </summary>
        /// <returns>A task that represents the asynchronous maintenance operation. The task result contains a boolean indicating whether the maintenance was successful.</returns>
        public Task<bool> RunMaintenance();

        /// <summary>
        /// Fetches a log message by its unique identifier. Requires SupportsLogFetch to be true.
        /// </summary>
        /// <param name="id">
        /// The unique identifier of the log message to fetch.
        /// </param>
        /// <returns>
        /// The log message with the specified unique identifier.
        /// </returns>
        public Task<Logging.LogItem?> GetLogMessageById(string id);

        /// <summary>
        /// Fetches log messages based on the provided view model. Requires SupportsLogFetch to be true.
        /// </summary>
        /// <param name="model">
        /// The view model containing the criteria for fetching log messages.
        /// </param>
        /// <returns>
        /// A list of log messages that match the criteria specified in the view model.
        /// </returns>
        public Task<List<Logging.LogItem>> GetLogMessages(Logging.LogsViewModel model);

        /// <summary>
        /// Shuts down the log provider, performing any necessary cleanup.
        /// </summary>
        public void Shutdown();
    }
}