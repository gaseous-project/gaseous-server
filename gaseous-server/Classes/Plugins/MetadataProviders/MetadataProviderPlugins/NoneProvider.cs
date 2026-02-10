using gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes;
using Humanizer;

namespace gaseous_server.Classes.Plugins.MetadataProviders.NoneProvider
{
    /// <summary>
    /// Provides metadata from the IGDB (Internet Game Database) API.
    /// </summary>
    public class Provider : IMetadataProvider
    {
        /// <inheritdoc/>
        public string Name => "None";

        /// <inheritdoc/>
        public FileSignature.MetadataSources SourceType => FileSignature.MetadataSources.None;

        /// <inheritdoc/>
        public Storage Storage { get; set; } = new Storage(FileSignature.MetadataSources.None);

        /// <summary>
        /// Proxy provider is not required for the None provider.
        /// </summary>
        /// <exception cref="NotImplementedException">Always thrown since the None provider does not use a proxy.</exception>
        public IProxyProvider? ProxyProvider { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        /// <inheritdoc/>
        public Dictionary<string, object>? Settings { get; set; }

        /// <inheritdoc/>
        public bool UsesInternet => false;

        /// <inheritdoc/>
        public async Task<AgeRating?> GetAgeRatingAsync(long id, bool forceRefresh = false)
        {
            return await Storage.GetCacheValue<AgeRating>(new AgeRating(), "id", id);
        }

        /// <inheritdoc/>
        public async Task<AgeRatingCategory?> GetAgeRatingCategoryAsync(long id, bool forceRefresh = false)
        {
            return await Storage.GetCacheValue<AgeRatingCategory>(new AgeRatingCategory(), "id", id);
        }

        /// <inheritdoc/>
        public async Task<AgeRatingContentDescription?> GetAgeRatingContentDescriptionAsync(long id, bool forceRefresh = false)
        {
            return await Storage.GetCacheValue<AgeRatingContentDescription>(new AgeRatingContentDescription(), "id", id);
        }

        /// <inheritdoc/>
        public async Task<AgeRatingOrganization?> GetAgeRatingOrganizationAsync(long id, bool forceRefresh = false)
        {
            return await Storage.GetCacheValue<AgeRatingOrganization>(new AgeRatingOrganization(), "id", id);
        }

        /// <inheritdoc/>
        public async Task<AlternativeName?> GetAlternativeNameAsync(long id, bool forceRefresh = false)
        {
            return await Storage.GetCacheValue<AlternativeName>(new AlternativeName(), "id", id);
        }

        /// <inheritdoc/>
        public async Task<Artwork?> GetArtworkAsync(long id, bool forceRefresh = false)
        {
            return await Storage.GetCacheValue<Artwork>(new Artwork(), "id", id);
        }

        /// <inheritdoc/>
        public async Task<ClearLogo?> GetClearLogoAsync(long id, bool forceRefresh = false)
        {
            return await Storage.GetCacheValue<ClearLogo>(new ClearLogo(), "id", id);
        }

        /// <inheritdoc/>
        public async Task<Collection?> GetCollectionAsync(long id, bool forceRefresh = false)
        {
            return await Storage.GetCacheValue<Collection>(new Collection(), "id", id);
        }

        /// <inheritdoc/>
        public async Task<Company?> GetCompanyAsync(long id, bool forceRefresh = false)
        {
            return await Storage.GetCacheValue<Company>(new Company(), "id", id);
        }

        /// <inheritdoc/>
        public async Task<CompanyLogo?> GetCompanyLogoAsync(long id, bool forceRefresh = false)
        {
            return await Storage.GetCacheValue<CompanyLogo>(new CompanyLogo(), "id", id);
        }

        /// <inheritdoc/>
        public async Task<Cover?> GetCoverAsync(long id, bool forceRefresh = false)
        {
            return await Storage.GetCacheValue<Cover>(new Cover(), "id", id);
        }

        /// <inheritdoc/>
        public async Task<ExternalGame?> GetExternalGameAsync(long id, bool forceRefresh = false)
        {
            return await Storage.GetCacheValue<ExternalGame>(new ExternalGame(), "id", id);
        }

        /// <inheritdoc/>
        public async Task<Franchise?> GetFranchiseAsync(long id, bool forceRefresh = false)
        {
            return await Storage.GetCacheValue<Franchise>(new Franchise(), "id", id);
        }

        /// <inheritdoc/>
        public async Task<Game?> GetGameAsync(long id, bool forceRefresh = false)
        {
            return await Storage.GetCacheValue<Game>(new Game(), "id", id);
        }

        /// <inheritdoc/>
        public async Task<byte[]?> GetGameImageAsync(long gameId, string url, ImageType imageType)
        {
            return await Storage.GetCacheValue<byte[]>(new byte[0], "id", gameId);
        }

        /// <inheritdoc/>
        public async Task<GameLocalization?> GetGameLocalizationAsync(long id, bool forceRefresh = false)
        {
            return await Storage.GetCacheValue<GameLocalization>(new GameLocalization(), "id", id);
        }

        /// <inheritdoc/>
        public async Task<GameMode?> GetGameModeAsync(long id, bool forceRefresh = false)
        {
            return await Storage.GetCacheValue<GameMode>(new GameMode(), "id", id);
        }

        /// <inheritdoc/>
        public async Task<GameVideo?> GetGameVideoAsync(long id, bool forceRefresh = false)
        {
            return await Storage.GetCacheValue<GameVideo>(new GameVideo(), "id", id);
        }

        /// <inheritdoc/>
        public async Task<Genre?> GetGenreAsync(long id, bool forceRefresh = false)
        {
            return await Storage.GetCacheValue<Genre>(new Genre(), "id", id);
        }

        /// <inheritdoc/>
        public async Task<InvolvedCompany?> GetInvolvedCompanyAsync(long id, bool forceRefresh = false)
        {
            return await Storage.GetCacheValue<InvolvedCompany>(new InvolvedCompany(), "id", id);
        }

        /// <inheritdoc/>
        public async Task<MultiplayerMode?> GetMultiplayerModeAsync(long id, bool forceRefresh = false)
        {
            return await Storage.GetCacheValue<MultiplayerMode>(new MultiplayerMode(), "id", id);
        }

        /// <inheritdoc/>
        public async Task<Platform?> GetPlatformAsync(long id, bool forceRefresh = false)
        {
            return await Storage.GetCacheValue<Platform>(new Platform(), "id", id);
        }

        /// <inheritdoc/>
        public async Task<PlatformLogo?> GetPlatformLogoAsync(long id, bool forceRefresh = false)
        {
            return await Storage.GetCacheValue<PlatformLogo>(new PlatformLogo(), "id", id);
        }

        /// <inheritdoc/>
        public async Task<PlatformVersion?> GetPlatformVersionAsync(long id, bool forceRefresh = false)
        {
            return await Storage.GetCacheValue<PlatformVersion>(new PlatformVersion(), "id", id);
        }

        /// <inheritdoc/>
        public async Task<Region?> GetRegionAsync(long id, bool forceRefresh = false)
        {
            return await Storage.GetCacheValue<Region>(new Region(), "id", id);
        }

        /// <inheritdoc/>
        public async Task<ReleaseDate?> GetReleaseDateAsync(long id, bool forceRefresh = false)
        {
            return await Storage.GetCacheValue<ReleaseDate>(new ReleaseDate(), "id", id);
        }

        /// <inheritdoc/>
        public async Task<Screenshot?> GetScreenshotAsync(long id, bool forceRefresh = false)
        {
            return await Storage.GetCacheValue<Screenshot>(new Screenshot(), "id", id);
        }

        /// <inheritdoc/>
        public async Task<Theme?> GetThemeAsync(long id, bool forceRefresh = false)
        {
            return await Storage.GetCacheValue<Theme>(new Theme(), "id", id);
        }

        /// <inheritdoc/>
        public Task<Game[]?> SearchGamesAsync(SearchType searchType, long platformId, List<string> searchCandidates)
        {
            throw new NotImplementedException();
        }
    }
}