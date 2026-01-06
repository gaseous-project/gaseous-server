namespace gaseous_server.Classes.Plugins.MetadataProviders
{
    /// <summary>
    /// Interface for metadata provider plugins.
    /// All metadata provider plugins must implement this interface, and normalise to the HasheousClient.Models.Metadata.IGDB models.
    /// </summary>
    public interface IMetadataProvider
    {
        /// <summary>
        /// Gets the type of plugin.
        /// </summary>
        public string PluginType => "MetadataProvider";

        /// <summary>
        /// Gets the name of the plugin.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the source type of metadata provider.
        /// </summary>
        public FileSignature.MetadataSources SourceType { get; }

        /// <summary>
        /// Gets or sets the configuration settings for the metadata provider plugin.
        /// </summary>
        public Dictionary<string, object>? Settings { get; set; }

        /// <summary>
        /// Gets a value indicating whether this plugin requires internet connectivity to function.
        /// </summary>
        public bool UsesInternet { get; }

    }
}