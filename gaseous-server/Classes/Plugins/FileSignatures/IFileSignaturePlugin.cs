namespace gaseous_server.Classes.Plugins.FileSignatures
{
    /// <summary>
    /// Interface for file signature plugins that process file hash objects.
    /// </summary>
    public interface IFileSignaturePlugin
    {
        /// <summary>
        /// Gets the type of plugin.
        /// </summary>
        public gaseous_server.Classes.Plugins.PluginManagement.PluginTypes PluginType => gaseous_server.Classes.Plugins.PluginManagement.PluginTypes.FileSignatureProvider;

        /// <summary>
        /// Gets the name of the plugin.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets or sets the configuration settings for the file signature plugin.
        /// </summary>
        public Dictionary<string, object>? Settings { get; set; }

        /// <summary>
        /// Gets a value indicating whether this plugin requires internet connectivity to function.
        /// </summary>
        public bool UsesInternet { get; }

        /// <summary>
        /// Gets the signature for a game file based on its hash and metadata.
        /// </summary>
        /// <param name="hash">The hash object of the file</param>
        /// <param name="ImageName">The name of the image file</param>
        /// <param name="ImageExtension">The extension of the image file</param>
        /// <param name="ImageSize">The size of the image file</param>
        /// <param name="GameFileImportPath">The import path of the game file</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the signature games model or null if not found.</returns>
        public Task<gaseous_server.Models.Signatures_Games?> GetSignature(HashObject hash, string ImageName, string ImageExtension, long ImageSize, string GameFileImportPath);
    }
}