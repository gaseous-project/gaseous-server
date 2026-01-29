using gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes;

namespace gaseous_server.Classes.Plugins.MetadataProviders
{
    /// <summary>
    /// Interface for proxy provider implementations.
    /// Metadata providers can call upon proxy providers to handle API calls through metadata proxy services such as Hasheous Proxy.
    /// </summary>
    public interface IProxyProvider
    {
        /// <summary>
        /// Gets the type of plugin.
        /// </summary>
        public gaseous_server.Classes.Plugins.PluginManagement.PluginTypes PluginType => gaseous_server.Classes.Plugins.PluginManagement.PluginTypes.MetadataProxyProvider;

        /// <summary>
        /// Gets the name of the plugin.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the source type of metadata provider.
        /// </summary>
        public FileSignature.MetadataSources SourceType { get; }

        /// <summary>
        /// Gets the storage handler for this metadata proxy provider (if required).
        /// </summary>
        public gaseous_server.Classes.Plugins.MetadataProviders.Storage? Storage { get; set; }

        /// <summary>
        /// Gets or sets the configuration settings for the metadata provider plugin.
        /// </summary>
        public Dictionary<string, object>? Settings { get; set; }

        /// <summary>
        /// Gets a value indicating whether this plugin requires internet connectivity to function.
        /// </summary>
        public bool UsesInternet { get; }

        /// <summary>
        /// Asynchronously retrieves a single entity of the specified type by its identifier.
        /// </summary>
        /// <typeparam name="T">The type of entity to retrieve.</typeparam>
        /// <param name="itemType">The type of item being requested.</param>
        /// <param name="id">The unique identifier of the entity.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the entity if found, or null if not found.</returns>
        public Task<T?> GetEntityAsync<T>(string itemType, long id) where T : class;

        /// <summary>
        /// Asynchronously searches for games of the specified type based on a query string.
        /// </summary>
        /// <param name="searchType">The type of search to perform.</param>
        /// <param name="platformId">The platform identifier to filter search results.</param>
        /// <param name="searchCandidates">The list of search candidate strings.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains an array of matching entities, or null if none are found.</returns>
        public Task<Game[]?> SearchGamesAsync(gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes.SearchType searchType, long platformId, List<string> searchCandidates);

        /// <summary>
        /// Retrieves an image from a given URL.
        /// </summary>
        /// <param name="gameId">
        /// The unique identifier of the game the image is associated with.
        /// </param>
        /// <param name="url">
        /// The URL of the image to retrieve.
        /// </param>
        /// <param name="imageType">
        /// The type of image being retrieved (e.g., Cover, Screenshot).
        /// </param>
        /// <param name="imageSize">
        /// The size of the image being retrieved (e.g., Small, Medium, Large, Original).
        /// </param>
        /// <returns>
        /// A byte array representing the image data.
        /// </returns>
        public Task<byte[]?> GetGameImageAsync(long gameId, string url, ImageType imageType, ImageSize imageSize);
    }
}