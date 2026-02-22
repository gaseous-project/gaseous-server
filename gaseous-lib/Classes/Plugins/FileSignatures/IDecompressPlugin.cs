namespace gaseous_server.Classes.Plugins.FileSignatures
{
    /// <summary>
    /// Interface for file signature plugins that process file hash objects.
    /// </summary>
    public interface IDecompressionPlugin
    {
        /// <summary>
        /// Gets the type of plugin.
        /// </summary>
        public string PluginType => "Decompression";

        /// <summary>
        /// Gets the name of the plugin.
        /// </summary>
        public string Name { get;}

        /// <summary>
        /// Gets the file extension that this plugin handles.
        /// </summary>
        public string Extension { get; }

        /// <summary>
        /// Decompresses a file from the specified compressed file path to the output directory.
        /// </summary>
        /// <param name="CompressedFilePath">The path to the compressed file.</param>
        /// <param name="OutputDirectory">The directory where the decompressed files will be extracted.</param>
        /// <returns>A task representing the asynchronous decompression operation.</returns>
        public Task DecompressFile(string CompressedFilePath, string OutputDirectory);
    }
}