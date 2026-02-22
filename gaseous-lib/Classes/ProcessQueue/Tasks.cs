namespace gaseous_server.ProcessQueue
{
    /// <summary>
    /// Defines the primary queue item types used to identify and route work to background services.
    /// </summary>
    public enum QueueItemType
    {
        /// <summary>
        /// Reserved for blocking all services - no actual background service is tied to this type
        /// </summary>
        All,

        /// <summary>
        /// Default type - no background service is tied to this type
        /// </summary>
        NotConfigured,

        /// <summary>
        /// Ingests signature DAT files into the database
        /// </summary>
        SignatureIngestor,

        /// <summary>
        /// Imports game files into the database and moves them to the required location on disk
        /// </summary>
        TitleIngestor,

        /// <summary>
        /// Processes the import queue and imports files into the database
        /// </summary>
        ImportQueueProcessor,

        /// <summary>
        /// Forces stored metadata to be refreshed
        /// </summary>
        MetadataRefresh,

        /// <summary>
        /// Ensures all managed files are where they are supposed to be
        /// </summary>
        OrganiseLibrary,

        /// <summary>
        /// Looks for orphaned files in the library and re-adds them to the database
        /// </summary>
        LibraryScan,

        /// <summary>
        /// Performs the work for the LibraryScan task
        /// </summary>
        LibraryScanWorker,

        /// <summary>
        /// Builds collections - set the options attribute to the id of the collection to build
        /// </summary>
        CollectionCompiler,

        /// <summary>
        /// Builds media groups - set the options attribute to the id of the media group to build
        /// </summary>
        MediaGroupCompiler,

        /// <summary>
        /// Performs and post database upgrade scripts that can be processed as a background task
        /// </summary>
        BackgroundDatabaseUpgrade,

        /// <summary>
        /// Performs a clean up of old files, and purge old logs
        /// </summary>
        DailyMaintainer,

        /// <summary>
        /// Performs more intensive cleanups and optimises the database
        /// </summary>
        WeeklyMaintainer,

        /// <summary>
        /// Cleans up marked paths in the temporary directory
        /// </summary>
        TempCleanup
    }

    /// <summary>
    /// Represents the specific sub task types associated with queue items.
    /// </summary>
    public enum QueueItemSubTasks
    {
        /// <summary>
        /// Processes items in the import queue.
        /// </summary>
        ImportQueueProcessor,

        /// <summary>
        /// Refreshes platform-related metadata.
        /// </summary>
        MetadataRefresh_Platform,

        /// <summary>
        /// Refreshes signature metadata.
        /// </summary>
        MetadataRefresh_Signatures,

        /// <summary>
        /// Refreshes game metadata.
        /// </summary>
        MetadataRefresh_Game,

        /// <summary>
        /// Executes database migration 1031.
        /// </summary>
        DatabaseMigration_1031,

        /// <summary>
        /// Performs work for the library scan task.
        /// </summary>
        LibraryScanWorker,

        /// <summary>
        /// Import signatures from a specific parser type - set the options attribute to the parser type to use
        /// </summary>
        SignatureIngest
    }
}