using SharpCompress.Archives;
using SharpCompress.Common;

namespace gaseous_server.Classes.Plugins.FileSignatures
{
    /// <summary>
    /// Provides 7z file decompression functionality using SharpCompress library.
    /// </summary>
    public class SevenZipDecompress : IDecompressionPlugin
    {
        /// <inheritdoc/>
        public string Name { get; } = "SevenZip";

        /// <inheritdoc/>
        public string Extension { get; } = ".7z";

        /// <inheritdoc/>
        public Task DecompressFile(string CompressedFilePath, string OutputDirectory)
        {
            Logging.LogKey(Logging.LogType.Information, "process.get_signature", "getsignature.decompressing_using_7z");
            try
            {
                using (var archive = SharpCompress.Archives.SevenZip.SevenZipArchive.Open(CompressedFilePath))
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
                Logging.LogKey(Logging.LogType.Warning, "process.get_signature", "getsignature.sevenzip_error", null, null, zipEx);
                throw;
            }

            return Task.CompletedTask;
        }
    }
}