using SharpCompress.Archives;
using SharpCompress.Common;

namespace gaseous_server.Classes.Plugins.FileSignatures
{
    /// <summary>
    /// Provides ZIP file decompression functionality using SharpCompress library.
    /// </summary>
    public class ZipDecompress : IDecompressionPlugin
    {
        /// <inheritdoc/>
        public string Name { get; } = "ZIP";

        /// <inheritdoc/>
        public string Extension { get; } = ".zip";

        /// <inheritdoc/>
        public Task DecompressFile(string CompressedFilePath, string OutputDirectory)
        {
            Logging.LogKey(Logging.LogType.Information, "process.get_signature", "getsignature.decompressing_using_zip");
            try
            {
                using (var archive = SharpCompress.Archives.Zip.ZipArchive.Open(CompressedFilePath))
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
                Logging.LogKey(Logging.LogType.Warning, "process.get_signature", "getsignature.unzip_error", null, null, zipEx);
                throw;
            }

            return Task.CompletedTask;
        }
    }
}