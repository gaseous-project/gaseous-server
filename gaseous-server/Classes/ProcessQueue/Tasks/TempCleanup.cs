using gaseous_server.Classes;

namespace gaseous_server.ProcessQueue.Plugins
{
    /// <summary>
    /// Represents a plugin task for cleaning up temporary files in the process queue.
    /// </summary>
    public class TempCleanup : ITaskPlugin
    {
        /// <inheritdoc/>
        public QueueItemType ItemType => QueueItemType.TempCleanup;

        /// <inheritdoc/>
        public object? Data { get; set; }

        /// <inheritdoc/>
        public required QueueProcessor.QueueItem ParentQueueItem { get; set; }

        /// <inheritdoc/>
        public async Task Execute()
        {
            try
            {
                foreach (GameLibrary.LibraryItem libraryItem in await GameLibrary.GetLibraries())
                {
                    string rootPath = Path.Combine(Config.LibraryConfiguration.LibraryTempDirectory, libraryItem.Id.ToString());
                    if (Directory.Exists(rootPath))
                    {
                        foreach (string directory in Directory.GetDirectories(rootPath))
                        {
                            DirectoryInfo info = new DirectoryInfo(directory);
                            if (info.LastWriteTimeUtc.AddMinutes(5) < DateTime.UtcNow)
                            {
                                Logging.LogKey(Logging.LogType.Information, "process.get_signature", "getsignature.deleting_temporary_decompress_folder", null, new[] { directory });
                                Directory.Delete(directory, true);
                            }
                        }
                    }
                }
            }
            catch (Exception tcEx)
            {
                Logging.LogKey(Logging.LogType.Warning, "process.get_signature", "getsignature.error_cleaning_temporary_files", null, null, tcEx);
            }
        }
    }
}