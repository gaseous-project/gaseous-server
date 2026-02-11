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
            return await GetEntityAsync<AgeRating>(id);
        }

        /// <inheritdoc/>
        public async Task<AgeRatingCategory?> GetAgeRatingCategoryAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<AgeRatingCategory>(id);
        }

        /// <inheritdoc/>
        public async Task<AgeRatingContentDescription?> GetAgeRatingContentDescriptionAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<AgeRatingContentDescription>(id);
        }

        /// <inheritdoc/>
        public async Task<AgeRatingOrganization?> GetAgeRatingOrganizationAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<AgeRatingOrganization>(id);
        }

        /// <inheritdoc/>
        public async Task<AlternativeName?> GetAlternativeNameAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<AlternativeName>(id);
        }

        /// <inheritdoc/>
        public async Task<Artwork?> GetArtworkAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<Artwork>(id);
        }

        /// <inheritdoc/>
        public async Task<ClearLogo?> GetClearLogoAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<ClearLogo>(id);
        }

        /// <inheritdoc/>
        public async Task<Collection?> GetCollectionAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<Collection>(id);
        }

        /// <inheritdoc/>
        public async Task<Company?> GetCompanyAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<Company>(id);
        }

        /// <inheritdoc/>
        public async Task<CompanyLogo?> GetCompanyLogoAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<CompanyLogo>(id);
        }

        /// <inheritdoc/>
        public async Task<Cover?> GetCoverAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<Cover>(id);
        }

        /// <inheritdoc/>
        public async Task<ExternalGame?> GetExternalGameAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<ExternalGame>(id);
        }

        /// <inheritdoc/>
        public async Task<Franchise?> GetFranchiseAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<Franchise>(id);
        }

        /// <inheritdoc/>
        public async Task<Game?> GetGameAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<Game>(id);
        }

        /// <inheritdoc/>
        public async Task<byte[]?> GetGameImageAsync(long gameId, string url, ImageType imageType)
        {
            return await Storage.GetCacheValue<byte[]>(new byte[0], "id", gameId);
        }

        /// <inheritdoc/>
        public async Task<GameLocalization?> GetGameLocalizationAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<GameLocalization>(id);
        }

        /// <inheritdoc/>
        public async Task<GameMode?> GetGameModeAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<GameMode>(id);
        }

        /// <inheritdoc/>
        public async Task<GameVideo?> GetGameVideoAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<GameVideo>(id);
        }

        /// <inheritdoc/>
        public async Task<Genre?> GetGenreAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<Genre>(id);
        }

        /// <inheritdoc/>
        public async Task<InvolvedCompany?> GetInvolvedCompanyAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<InvolvedCompany>(id);
        }

        /// <inheritdoc/>
        public async Task<MultiplayerMode?> GetMultiplayerModeAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<MultiplayerMode>(id);
        }

        /// <inheritdoc/>
        public async Task<Platform?> GetPlatformAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<Platform>(id);
        }

        /// <inheritdoc/>
        public async Task<PlatformLogo?> GetPlatformLogoAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<PlatformLogo>(id);
        }

        /// <inheritdoc/>
        public async Task<PlatformVersion?> GetPlatformVersionAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<PlatformVersion>(id);
        }

        /// <inheritdoc/>
        public async Task<PlayerPerspective?> GetPlayerPerspectiveAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<PlayerPerspective>(id);
        }

        /// <inheritdoc/>
        public async Task<Region?> GetRegionAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<Region>(id);
        }

        /// <inheritdoc/>
        public async Task<ReleaseDate?> GetReleaseDateAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<ReleaseDate>(id);
        }

        /// <inheritdoc/>
        public async Task<Screenshot?> GetScreenshotAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<Screenshot>(id);
        }

        /// <inheritdoc/>
        public async Task<Theme?> GetThemeAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<Theme>(id);
        }

        /// <inheritdoc/>
        public Task<Game[]?> SearchGamesAsync(SearchType searchType, long platformId, List<string> searchCandidates)
        {
            throw new NotImplementedException();
        }

        private async Task<T?> GetEntityAsync<T>(long id, bool forceRefresh = false) where T : class
        {
            if (id == 0)
            {
                return null;
            }

            T? metadata = Activator.CreateInstance(typeof(T)) as T;

            // get name of type for storage purposes
            string typeName = typeof(T).Name;

            var cacheStatus = await Storage.GetCacheStatusAsync(typeName, id);
            if (cacheStatus == Storage.CacheStatus.Current)
            {
                return await Storage.GetCacheValue<T>(metadata, "id", id);
            }

            return null;
        }
    }
}