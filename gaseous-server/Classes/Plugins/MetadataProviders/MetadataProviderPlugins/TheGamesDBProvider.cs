using gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes;

namespace gaseous_server.Classes.Plugins.MetadataProviders.TheGamesDBProvider
{
    /// <summary>
    /// Metadata provider implementation for The Games DB.
    /// </summary>
    public class Provider : IMetadataProvider
    {
        /// <inheritdoc/>
        public string Name => "TheGamesDB Metadata Provider";

        /// <inheritdoc/>
        public FileSignature.MetadataSources SourceType => FileSignature.MetadataSources.TheGamesDb;

        /// <inheritdoc/>
        public Storage Storage { get; set; } = new Storage(FileSignature.MetadataSources.TheGamesDb);

        /// <summary>
        /// Proxy provider is not required for TheGamesDB.
        /// </summary>
        public IProxyProvider? ProxyProvider { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        /// <inheritdoc/>
        public Dictionary<string, object>? Settings { get; set; }

        /// <inheritdoc/>
        public bool UsesInternet => true;

        /// <inheritdoc/>
        public Task<AgeRating?> GetAgeRatingAsync(long id, bool forceRefresh = false)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<AgeRatingCategory?> GetAgeRatingCategoryAsync(long id, bool forceRefresh = false)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<AgeRatingContentDescription?> GetAgeRatingContentDescriptionAsync(long id, bool forceRefresh = false)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<AgeRatingOrganization?> GetAgeRatingOrganizationAsync(long id, bool forceRefresh = false)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<AlternativeName?> GetAlternativeNameAsync(long id, bool forceRefresh = false)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<Artwork?> GetArtworkAsync(long id, bool forceRefresh = false)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<ClearLogo?> GetClearLogoAsync(long id, bool forceRefresh = false)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<Collection?> GetCollectionAsync(long id, bool forceRefresh = false)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<Company?> GetCompanyAsync(long id, bool forceRefresh = false)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<CompanyLogo?> GetCompanyLogoAsync(long id, bool forceRefresh = false)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<Cover?> GetCoverAsync(long id, bool forceRefresh = false)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<ExternalGame?> GetExternalGameAsync(long id, bool forceRefresh = false)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<Franchise?> GetFranchiseAsync(long id, bool forceRefresh = false)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<Game?> GetGameAsync(long id, bool forceRefresh = false)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<GameLocalization?> GetGameLocalizationAsync(long id, bool forceRefresh = false)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<GameMode?> GetGameModeAsync(long id, bool forceRefresh = false)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<GameVideo?> GetGameVideoAsync(long id, bool forceRefresh = false)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<Genre?> GetGenreAsync(long id, bool forceRefresh = false)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<InvolvedCompany?> GetInvolvedCompanyAsync(long id, bool forceRefresh = false)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<MultiplayerMode?> GetMultiplayerModeAsync(long id, bool forceRefresh = false)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<Platform?> GetPlatformAsync(long id, bool forceRefresh = false)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<PlatformLogo?> GetPlatformLogoAsync(long id, bool forceRefresh = false)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<PlatformVersion?> GetPlatformVersionAsync(long id, bool forceRefresh = false)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<Region?> GetRegionAsync(long id, bool forceRefresh = false)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<ReleaseDate?> GetReleaseDateAsync(long id, bool forceRefresh = false)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<Screenshot?> GetScreenshotAsync(long id, bool forceRefresh = false)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<Theme?> GetThemeAsync(long id, bool forceRefresh = false)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<Game[]?> SearchGamesAsync(SearchType searchType, long platformId, List<string> searchCandidates)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<string> GetImageAsync(string url)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<byte[]?> GetImageAsync(long gameId, string url, ImageType imageType, ImageSize imageSize)
        {
            return null;
        }
    }
}