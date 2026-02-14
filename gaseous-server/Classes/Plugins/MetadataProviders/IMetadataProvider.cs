using gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes;

namespace gaseous_server.Classes.Plugins.MetadataProviders
{
    /// <summary>
    /// Interface for metadata provider plugins.
    /// All metadata provider plugins must implement this interface, and normalise to the HasheousClient.Models.Metadata.IGDB models.
    /// 
    /// Note: Effective version 2 and later:
    /// Local game metadata content is to be saved in the path ~/.gaseous-server/Data/GameMetadata/Bundles/{SourceType}/{GameId}/
    /// where {SourceType} is the FileSignature.MetadataSources enum value for the provider (e.g., IGDB, TheGamesDB, etc), and {GameId} is the unique identifier for the game in that metadata source.
    /// Each metadata provider is responsible for managing its own local storage within that path.
    /// Content directories (images and videos) should be stored in subdirectories with the name of the content type:
    /// - artwork
    /// - boxart
    /// - clearlogos
    /// - covers
    /// - fanart
    /// - screenshots
    /// - videos
    /// Each metadata provider should implement caching and retrieval logic as needed, and is expected to handle mapping content types to standardized names when metadata is requested.
    /// Each metadata provider should also be able to cope with the deletion of local cached content, and re-download it as necessary - as maintenance processes may clear out unused cached content periodically.
    /// </summary>
    public interface IMetadataProvider
    {
        /// <summary>
        /// Gets the type of plugin.
        /// </summary>
        public gaseous_server.Classes.Plugins.PluginManagement.PluginTypes PluginType => gaseous_server.Classes.Plugins.PluginManagement.PluginTypes.MetadataProvider;

        /// <summary>
        /// Gets the name of the plugin.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the source type of metadata provider.
        /// </summary>
        public FileSignature.MetadataSources SourceType { get; }

        /// <summary>
        /// Gets the storage handler for this metadata provider.
        /// </summary>
        public gaseous_server.Classes.Plugins.MetadataProviders.Storage Storage { get; set; }

        /// <summary>
        /// Gets or sets the proxy provider used by this metadata provider.
        /// </summary>
        public IProxyProvider? ProxyProvider { get; set; }

        /// <summary>
        /// Gets or sets the configuration settings for the metadata provider plugin.
        /// </summary>
        public Dictionary<string, object>? Settings { get; set; }

        /// <summary>
        /// Gets a value indicating whether this plugin requires internet connectivity to function.
        /// </summary>
        public bool UsesInternet { get; }

        // Metadata Retrieval Methods
        // -------------------------------------------------------------
        // The following methods retrieve specific metadata types by ID.
        // All methods support optional forceRefresh to bypass caching.
        // Returns null if the metadata is not found.

        /// <summary>
        /// Retrieves age rating information for a specific rating ID.
        /// </summary>
        /// <param name="id">The unique identifier of the age rating.</param>
        /// <param name="forceRefresh">If true, bypasses cache and fetches fresh data from the source.</param>
        /// <returns>An AgeRating object if found; otherwise, null.</returns>
        public Task<AgeRating?> GetAgeRatingAsync(long id, bool forceRefresh = false);

        /// <summary>
        /// Retrieves age rating category information (e.g., ESRB, PEGI).
        /// </summary>
        /// <param name="id">The unique identifier of the age rating category.</param>
        /// <param name="forceRefresh">If true, bypasses cache and fetches fresh data from the source.</param>
        /// <returns>An AgeRatingCategory object if found; otherwise, null.</returns>
        public Task<AgeRatingCategory?> GetAgeRatingCategoryAsync(long id, bool forceRefresh = false);

        /// <summary>
        /// Retrieves content descriptors for age ratings (e.g., Violence, Language).
        /// </summary>
        /// <param name="id">The unique identifier of the content description.</param>
        /// <param name="forceRefresh">If true, bypasses cache and fetches fresh data from the source.</param>
        /// <returns>An AgeRatingContentDescription object if found; otherwise, null.</returns>
        public Task<AgeRatingContentDescription?> GetAgeRatingContentDescriptionAsync(long id, bool forceRefresh = false);

        /// <summary>
        /// Retrieves information about age rating organizations (e.g., ESRB, PEGI, CERO).
        /// </summary>
        /// <param name="id">The unique identifier of the rating organization.</param>
        /// <param name="forceRefresh">If true, bypasses cache and fetches fresh data from the source.</param>
        /// <returns>An AgeRatingOrganization object if found; otherwise, null.</returns>
        public Task<AgeRatingOrganization?> GetAgeRatingOrganizationAsync(long id, bool forceRefresh = false);

        /// <summary>
        /// Retrieves alternative names for games (e.g., regional titles, abbreviations).
        /// </summary>
        /// <param name="id">The unique identifier of the alternative name.</param>
        /// <param name="forceRefresh">If true, bypasses cache and fetches fresh data from the source.</param>
        /// <returns>An AlternativeName object if found; otherwise, null.</returns>
        public Task<AlternativeName?> GetAlternativeNameAsync(long id, bool forceRefresh = false);

        /// <summary>
        /// Retrieves artwork images for games (e.g., promotional art, concept art).
        /// </summary>
        /// <param name="id">The unique identifier of the artwork.</param>
        /// <param name="forceRefresh">If true, bypasses cache and fetches fresh data from the source.</param>
        /// <returns>An Artwork object if found; otherwise, null.</returns>
        public Task<Artwork?> GetArtworkAsync(long id, bool forceRefresh = false);

        /// <summary>
        /// Retrieves clear logo images (transparent PNG logos) for games.
        /// </summary>
        /// <param name="id">The unique identifier of the clear logo.</param>
        /// <param name="forceRefresh">If true, bypasses cache and fetches fresh data from the source.</param>
        /// <returns>A ClearLogo object if found; otherwise, null.</returns>
        public Task<ClearLogo?> GetClearLogoAsync(long id, bool forceRefresh = false);

        /// <summary>
        /// Retrieves game collection information (e.g., series, sagas).
        /// </summary>
        /// <param name="id">The unique identifier of the collection.</param>
        /// <param name="forceRefresh">If true, bypasses cache and fetches fresh data from the source.</param>
        /// <returns>A Collection object if found; otherwise, null.</returns>
        public Task<Collection?> GetCollectionAsync(long id, bool forceRefresh = false);

        /// <summary>
        /// Retrieves company information (developers, publishers, etc.).
        /// </summary>
        /// <param name="id">The unique identifier of the company.</param>
        /// <param name="forceRefresh">If true, bypasses cache and fetches fresh data from the source.</param>
        /// <returns>A Company object if found; otherwise, null.</returns>
        public Task<Company?> GetCompanyAsync(long id, bool forceRefresh = false);

        /// <summary>
        /// Retrieves logo images for companies.
        /// </summary>
        /// <param name="id">The unique identifier of the company logo.</param>
        /// <param name="forceRefresh">If true, bypasses cache and fetches fresh data from the source.</param>
        /// <returns>A CompanyLogo object if found; otherwise, null.</returns>
        public Task<CompanyLogo?> GetCompanyLogoAsync(long id, bool forceRefresh = false);

        /// <summary>
        /// Retrieves cover art images for games (box art).
        /// </summary>
        /// <param name="id">The unique identifier of the cover.</param>
        /// <param name="forceRefresh">If true, bypasses cache and fetches fresh data from the source.</param>
        /// <returns>A Cover object if found; otherwise, null.</returns>
        public Task<Cover?> GetCoverAsync(long id, bool forceRefresh = false);

        /// <summary>
        /// Retrieves external game identifiers (e.g., links to other databases or platforms).
        /// </summary>
        /// <param name="id">The unique identifier of the external game reference.</param>
        /// <param name="forceRefresh">If true, bypasses cache and fetches fresh data from the source.</param>
        /// <returns>An ExternalGame object if found; otherwise, null.</returns>
        public Task<ExternalGame?> GetExternalGameAsync(long id, bool forceRefresh = false);

        /// <summary>
        /// Retrieves franchise information for game series.
        /// </summary>
        /// <param name="id">The unique identifier of the franchise.</param>
        /// <param name="forceRefresh">If true, bypasses cache and fetches fresh data from the source.</param>
        /// <returns>A Franchise object if found; otherwise, null.</returns>
        public Task<Franchise?> GetFranchiseAsync(long id, bool forceRefresh = false);

        /// <summary>
        /// Retrieves localization information for games (language support, regional versions).
        /// </summary>
        /// <param name="id">The unique identifier of the game localization.</param>
        /// <param name="forceRefresh">If true, bypasses cache and fetches fresh data from the source.</param>
        /// <returns>A GameLocalization object if found; otherwise, null.</returns>
        public Task<GameLocalization?> GetGameLocalizationAsync(long id, bool forceRefresh = false);

        /// <summary>
        /// Retrieves game mode information (e.g., Single-player, Multiplayer, Co-op).
        /// </summary>
        /// <param name="id">The unique identifier of the game mode.</param>
        /// <param name="forceRefresh">If true, bypasses cache and fetches fresh data from the source.</param>
        /// <returns>A GameMode object if found; otherwise, null.</returns>
        public Task<GameMode?> GetGameModeAsync(long id, bool forceRefresh = false);

        /// <summary>
        /// Retrieves comprehensive game information including title, description, and associated metadata.
        /// </summary>
        /// <param name="id">The unique identifier of the game.</param>
        /// <param name="forceRefresh">If true, bypasses cache and fetches fresh data from the source.</param>
        /// <returns>A Game object if found; otherwise, null.</returns>
        public Task<Game?> GetGameAsync(long id, bool forceRefresh = false);

        /// <summary>
        /// Retrieves video content for games (e.g., trailers, gameplay videos).
        /// </summary>
        /// <param name="id">The unique identifier of the game video.</param>
        /// <param name="forceRefresh">If true, bypasses cache and fetches fresh data from the source.</param>
        /// <returns>A GameVideo object if found; otherwise, null.</returns>
        public Task<GameVideo?> GetGameVideoAsync(long id, bool forceRefresh = false);

        /// <summary>
        /// Retrieves genre information for games (e.g., Action, RPG, Strategy).
        /// </summary>
        /// <param name="id">The unique identifier of the genre.</param>
        /// <param name="forceRefresh">If true, bypasses cache and fetches fresh data from the source.</param>
        /// <returns>A Genre object if found; otherwise, null.</returns>
        public Task<Genre?> GetGenreAsync(long id, bool forceRefresh = false);

        /// <summary>
        /// Retrieves information about companies involved in game development (with role details).
        /// </summary>
        /// <param name="id">The unique identifier of the involved company relationship.</param>
        /// <param name="forceRefresh">If true, bypasses cache and fetches fresh data from the source.</param>
        /// <returns>An InvolvedCompany object if found; otherwise, null.</returns>
        public Task<InvolvedCompany?> GetInvolvedCompanyAsync(long id, bool forceRefresh = false);

        /// <summary>
        /// Retrieves detailed multiplayer mode information (player counts, online/offline capabilities).
        /// </summary>
        /// <param name="id">The unique identifier of the multiplayer mode.</param>
        /// <param name="forceRefresh">If true, bypasses cache and fetches fresh data from the source.</param>
        /// <returns>A MultiplayerMode object if found; otherwise, null.</returns>
        public Task<MultiplayerMode?> GetMultiplayerModeAsync(long id, bool forceRefresh = false);

        /// <summary>
        /// Retrieves logo images for gaming platforms.
        /// </summary>
        /// <param name="id">The unique identifier of the platform logo.</param>
        /// <param name="forceRefresh">If true, bypasses cache and fetches fresh data from the source.</param>
        /// <returns>A PlatformLogo object if found; otherwise, null.</returns>
        public Task<PlatformLogo?> GetPlatformLogoAsync(long id, bool forceRefresh = false);

        /// <summary>
        /// Retrieves platform information (consoles, PC, mobile devices).
        /// </summary>
        /// <param name="id">The unique identifier of the platform.</param>
        /// <param name="forceRefresh">If true, bypasses cache and fetches fresh data from the source.</param>
        /// <returns>A Platform object if found; otherwise, null.</returns>
        public Task<Platform?> GetPlatformAsync(long id, bool forceRefresh = false);

        /// <summary>
        /// Retrieves specific version information for platforms (e.g., PS4 Pro, Xbox One S).
        /// </summary>
        /// <param name="id">The unique identifier of the platform version.</param>
        /// <param name="forceRefresh">If true, bypasses cache and fetches fresh data from the source.</param>
        /// <returns>A PlatformVersion object if found; otherwise, null.</returns>
        public Task<PlatformVersion?> GetPlatformVersionAsync(long id, bool forceRefresh = false);

        /// <summary>
        /// Retrieves player perspective information for games (e.g., First-person, Third-person, Isometric).
        /// </summary> <param name="id">The unique identifier of the player perspective.</param>
        /// <param name="forceRefresh">If true, bypasses cache and fetches fresh data from the source.</param>
        /// <returns>A PlayerPerspective object if found; otherwise, null.</returns>
        public Task<PlayerPerspective?> GetPlayerPerspectiveAsync(long id, bool forceRefresh = false);

        /// <summary>
        /// Retrieves regional information (e.g., North America, Europe, Japan).
        /// </summary>
        /// <param name="id">The unique identifier of the region.</param>
        /// <param name="forceRefresh">If true, bypasses cache and fetches fresh data from the source.</param>
        /// <returns>A Region object if found; otherwise, null.</returns>
        public Task<Region?> GetRegionAsync(long id, bool forceRefresh = false);

        /// <summary>
        /// Retrieves release date information for games on specific platforms/regions.
        /// </summary>
        /// <param name="id">The unique identifier of the release date.</param>
        /// <param name="forceRefresh">If true, bypasses cache and fetches fresh data from the source.</param>
        /// <returns>A ReleaseDate object if found; otherwise, null.</returns>
        public Task<ReleaseDate?> GetReleaseDateAsync(long id, bool forceRefresh = false);

        /// <summary>
        /// Retrieves screenshot images from games.
        /// </summary>
        /// <param name="id">The unique identifier of the screenshot.</param>
        /// <param name="forceRefresh">If true, bypasses cache and fetches fresh data from the source.</param>
        /// <returns>A Screenshot object if found; otherwise, null.</returns>
        public Task<Screenshot?> GetScreenshotAsync(long id, bool forceRefresh = false);

        /// <summary>
        /// Retrieves theme information for games (e.g., Fantasy, Sci-Fi, Horror).
        /// </summary>
        /// <param name="id">The unique identifier of the theme.</param>
        /// <param name="forceRefresh">If true, bypasses cache and fetches fresh data from the source.</param>
        /// <returns>A Theme object if found; otherwise, null.</returns>
        public Task<Theme?> GetThemeAsync(long id, bool forceRefresh = false);

        /// <summary>
        /// Searches for games using various search criteria and candidate names.
        /// </summary>
        /// <param name="searchType">The type of search to perform (e.g., by title, exact match).</param>
        /// <param name="platformId">The platform identifier to limit the search scope.</param>
        /// <param name="searchCandidates">A list of search terms or candidate names to search for.</param>
        /// <returns>An array of Game objects matching the search criteria; null if no results found.</returns>
        public Task<Game[]?> SearchGamesAsync(gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes.SearchType searchType, long platformId, List<string> searchCandidates);

        /// <summary>
        /// Retrieves an image from a given URL.
        /// </summary>
        /// <param name="gameId">
        /// The unique identifier of the game the image is associated with.
        /// </param>
        /// <param name="url">
        /// The URL of the image to retrieve. Depending on the metadata provider implementation, this may be a direct link or an image id.
        /// </param>
        /// <param name="imageType">
        /// The type of image being retrieved (e.g., Cover, Screenshot).
        /// </param>
        /// <returns>
        /// A byte array representing the image data.
        /// </returns>
        public Task<byte[]?> GetGameImageAsync(long gameId, string url, ImageType imageType);
    }
}