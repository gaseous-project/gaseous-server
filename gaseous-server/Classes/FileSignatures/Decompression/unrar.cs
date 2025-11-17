using SharpCompress.Archives;
using SharpCompress.Common;

namespace gaseous_server.Classes.Plugins.FileSignatures
{
    /// <summary>
    /// Provides RAR file decompression functionality using SharpCompress library.
    /// </summary>
    public class RarDecompress : IDecompressionPlugin
    {
        /// <inheritdoc/>
        public string Name { get; } = "RAR";

        /// <inheritdoc/>
        public string Extension { get; } = ".rar";

        /// <inheritdoc/>
        public Task DecompressFile(string CompressedFilePath, string OutputDirectory)
        {
            Logging.LogKey(Logging.LogType.Information, "process.get_signature", "getsignature.decompressing_using_rar");
            try
            {
                using (var archive = SharpCompress.Archives.Rar.RarArchive.Open(CompressedFilePath))
                {
                    foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
                    {
                        Logging.LogKey(Logging.LogType.Information, "process.get_signature", "getsignature.extracting_file", null, new string[] { entry.Key });
                        entry.WriteToDirectory(OutputDirectory, new ExtractionOptions()
                        {
                            ExtractFullPath = true,
                            Overwrite = true
                        });
                    }
                }
            }
            catch (Exception zipEx)
            {
                Logging.LogKey(Logging.LogType.Warning, "process.get_signature", "getsignature.unrar_error", null, null, zipEx);
                throw;
            }

            return Task.CompletedTask;
        }
    }
}