using gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes;
using Humanizer;

namespace gaseous_server.Classes.Plugins.MetadataProviders.IGDBProvider
{
    /// <summary>
    /// Provides metadata from the IGDB (Internet Game Database) API.
    /// </summary>
    public class Provider : IMetadataProvider
    {
        /// <inheritdoc/>
        public string Name => "IGDB Metadata Provider";

        /// <inheritdoc/>
        public FileSignature.MetadataSources SourceType => FileSignature.MetadataSources.IGDB;

        /// <inheritdoc/>
        public Storage Storage { get; set; } = new Storage(FileSignature.MetadataSources.IGDB);

        /// <inheritdoc/>
        public IProxyProvider? ProxyProvider { get; set; }

        /// <summary>
        /// Gets or sets the settings for the IGDB metadata provider.
        /// Required settings:
        /// <list type="bullet">
        /// <item>
        /// <term>"ClientID"</term>
        /// <description>Your IGDB Client ID.</description>
        /// </item>
        /// <item>
        /// <term>"ClientSecret"</term>
        /// <description>Your IGDB Client Secret.</description>
        /// </item>
        /// </list>
        /// </summary>
        public Dictionary<string, object>? Settings { get; set; }

        /// <inheritdoc/>
        public bool UsesInternet => true;

        /// <summary>
        /// HTTP communications handler for making API requests.
        /// </summary>
        private readonly HTTPComms comms = new HTTPComms();

        /// <summary>
        /// Cached IGDB authentication token. Do not reference directly; use IGDBAuthToken property.
        /// </summary>
        internal IGDBAuth? igdbAuth = null;

        /// <summary>
        /// Represents the IGDB authentication token and its expiration.
        /// </summary>
        internal class IGDBAuth
        {
            /// <summary>
            /// The client ID associated with the token.
            /// </summary>
            public string client_id { get; set; } = string.Empty;

            /// <summary>
            /// The access token string. Used with API requests.
            /// </summary>
            public string access_token { get; set; } = string.Empty;

            /// <summary>
            /// The number of seconds until the token expires.
            /// </summary>
            public int expires_in { get; set; } = 0;

            /// <summary>
            /// The type of the token (usually "bearer").
            /// </summary>
            public string token_type { get; set; } = string.Empty;

            /// <summary>
            /// The time the token was obtained.
            /// </summary>
            public DateTime obtained_at { get; set; } = DateTime.MinValue;

            /// <summary>
            /// The exact expiration time of the token.
            /// </summary>
            public DateTime expires_at => obtained_at.AddSeconds(expires_in - 60); // Subtract 60 seconds as buffer

            public Dictionary<string, string> ToHeaders()
            {
                return new Dictionary<string, string>
                {
                    { "Accept", "application/json" },
                    { "Client-ID", client_id }, // Client-ID will be set separately
                    { "Authorization", $"{token_type} {access_token}" }
                };
            }
        }

        /// <summary>
        /// Gets a valid IGDB authentication token, refreshing it if necessary.
        /// </summary>
        /// <returns>
        /// The IGDBAuth object containing the access token and related information, or null if authentication fails or is not configured.
        /// </returns>
        private IGDBAuth? IGDBAuthToken
        {
            get
            {
                // fail if settings are not configured
                if (Settings == null || !Settings.ContainsKey("ClientID") || !Settings.ContainsKey("ClientSecret"))
                {
                    return null;
                }

                // if token is valid, return it
                if (igdbAuth != null && DateTime.UtcNow < igdbAuth.expires_at)
                {
                    return igdbAuth;
                }

                // token is missing or expired, request a new one
                var clientId = Settings["ClientID"].ToString();
                var clientSecret = Settings["ClientSecret"].ToString();

                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
                {
                    return null;
                }

                var tokenRequestUrl = $"https://id.twitch.tv/oauth2/token?client_id={clientId}&client_secret={clientSecret}&grant_type=client_credentials";

                var response = comms.SendRequestAsync<IGDBAuth>(HTTPComms.HttpMethod.POST, tokenRequestUrl).GetAwaiter().GetResult();
                if (response.StatusCode != 200 || response.Body == null)
                {
                    return null;
                }
                igdbAuth = response.Body;
                igdbAuth.client_id = clientId;
                igdbAuth.obtained_at = DateTime.UtcNow;
                return igdbAuth;
            }
        }

        private string IGDBUrl => "https://api.igdb.com/v4/";

        /// <inheritdoc/>
        public async Task<AgeRating?> GetAgeRatingAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<AgeRating>("age_ratings", id, forceRefresh);
        }

        /// <inheritdoc/>
        public async Task<AgeRatingCategory?> GetAgeRatingCategoryAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<AgeRatingCategory>("age_rating_categories", id, forceRefresh);
        }

        /// <inheritdoc/>
        public async Task<AgeRatingContentDescription?> GetAgeRatingContentDescriptionAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<AgeRatingContentDescription>("age_rating_content_descriptions_v2", id, forceRefresh);
        }

        /// <inheritdoc/>
        public async Task<AgeRatingOrganization?> GetAgeRatingOrganizationAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<AgeRatingOrganization>("age_rating_organizations", id, forceRefresh);
        }

        /// <inheritdoc/>
        public async Task<AlternativeName?> GetAlternativeNameAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<AlternativeName>("alternative_names", id, forceRefresh);
        }

        /// <inheritdoc/>
        public async Task<Artwork?> GetArtworkAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<Artwork>("artworks", id, forceRefresh);
        }

        /// <inheritdoc/>
        public async Task<ClearLogo?> GetClearLogoAsync(long id, bool forceRefresh = false)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<Collection?> GetCollectionAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<Collection>("collections", id, forceRefresh);
        }

        /// <inheritdoc/>
        public async Task<Company?> GetCompanyAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<Company>("companies", id, forceRefresh);
        }

        /// <inheritdoc/>
        public async Task<CompanyLogo?> GetCompanyLogoAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<CompanyLogo>("company_logos", id, forceRefresh);
        }

        /// <inheritdoc/>
        public async Task<Cover?> GetCoverAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<Cover>("covers", id, forceRefresh);
        }

        /// <inheritdoc/>
        public async Task<ExternalGame?> GetExternalGameAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<ExternalGame>("external_games", id, forceRefresh);
        }

        /// <inheritdoc/>
        public async Task<Franchise?> GetFranchiseAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<Franchise>("franchises", id, forceRefresh);
        }

        /// <inheritdoc/>
        public async Task<Game?> GetGameAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<Game>("games", id, forceRefresh);
        }

        /// <inheritdoc/>
        public async Task<GameLocalization?> GetGameLocalizationAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<GameLocalization>("game_localizations", id, forceRefresh);
        }

        /// <inheritdoc/>
        public async Task<GameMode?> GetGameModeAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<GameMode>("game_modes", id, forceRefresh);
        }

        /// <inheritdoc/>
        public async Task<GameVideo?> GetGameVideoAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<GameVideo>("game_videos", id, forceRefresh);
        }

        /// <inheritdoc/>
        public async Task<Genre?> GetGenreAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<Genre>("genres", id, forceRefresh);
        }

        /// <inheritdoc/>
        public async Task<InvolvedCompany?> GetInvolvedCompanyAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<InvolvedCompany>("involved_companies", id, forceRefresh);
        }

        /// <inheritdoc/>
        public async Task<MultiplayerMode?> GetMultiplayerModeAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<MultiplayerMode>("multiplayer_modes", id, forceRefresh);
        }

        /// <inheritdoc/>
        public async Task<Platform?> GetPlatformAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<Platform>("platforms", id, forceRefresh);
        }

        /// <inheritdoc/>
        public async Task<PlatformLogo?> GetPlatformLogoAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<PlatformLogo>("platform_logos", id, forceRefresh);
        }

        /// <inheritdoc/>
        public async Task<PlatformVersion?> GetPlatformVersionAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<PlatformVersion>("platform_versions", id, forceRefresh);
        }

        /// <inheritdoc/>
        public async Task<Region?> GetRegionAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<Region>("regions", id, forceRefresh);
        }

        /// <inheritdoc/>
        public async Task<ReleaseDate?> GetReleaseDateAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<ReleaseDate>("release_dates", id, forceRefresh);
        }

        /// <inheritdoc/>
        public async Task<Screenshot?> GetScreenshotAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<Screenshot>("screenshots", id, forceRefresh);
        }

        /// <inheritdoc/>
        public async Task<Theme?> GetThemeAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<Theme>("themes", id, forceRefresh);
        }

        /// <inheritdoc/>
        public async Task<Game[]?> SearchGamesAsync(SearchType searchType, long platformId, List<string> searchCandidates)
        {
            List<Game>? results = new List<Game>();

            if (ProxyProvider != null)
            {
                // use proxy provider if defined
                if (ProxyProvider.Storage == null)
                {
                    ProxyProvider.Storage = this.Storage;
                }
                var proxyResult = await ProxyProvider.SearchGamesAsync(searchType, platformId, searchCandidates);

                return proxyResult;
            }

            if (IGDBAuthToken != null)
            {
                foreach (var candidate in searchCandidates)
                {
                    string searchBody = "fields id,name,slug,platforms,summary; ";
                    string escCandidate = candidate.Replace("\\", "\\\\").Replace("\"", "\\\"");
                    switch (searchType)
                    {
                        case SearchType.search:
                            searchBody += $"search \"{escCandidate}\"; where platforms = {platformId};";
                            break;
                        case SearchType.wherefuzzy:
                            searchBody += $"where platforms = ({platformId}) & name ~ *\"{escCandidate}\"*;";
                            break;
                        case SearchType.where:
                            searchBody += $"where platforms = ({platformId}) & name ~ \"{escCandidate}\";";
                            break;
                    }

                    // send request
                    var requestUrl = $"{IGDBUrl}games";
                    var headers = IGDBAuthToken.ToHeaders();
                    var response = await comms.SendRequestAsync<Dictionary<string, object>[]>(HTTPComms.HttpMethod.POST, requestUrl, headers, searchBody);
                    if (response.StatusCode == 200 && response.Body != null && response.Body.Length > 0)
                    {
                        List<Dictionary<string, object>> games = response.Body.ToList();

                        foreach (var game in games)
                        {
                            Game? result = ConvertToEntity<Game>(game);

                            results.Add(result!);
                        }
                    }
                }

                return results.DistinctBy(g => g.Id).ToArray();
            }

            return null;
        }

        /// <summary>
        /// Generic method to get an entity by endpoint and ID.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="endpoint"></param>
        /// <param name="id"></param>
        /// <param name="forceRefresh"></param>
        /// <returns></returns>
        private async Task<T?> GetEntityAsync<T>(string endpoint, long id, bool forceRefresh = false) where T : class
        {
            if (id == 0)
            {
                return null;
            }

            // check storage first
            List<Storage.CacheStatus> forceLoadStatuses = new List<Storage.CacheStatus>
            {
                Storage.CacheStatus.NotPresent,
                Storage.CacheStatus.Expired
            };

            T? metadata = Activator.CreateInstance(typeof(T)) as T;

            // get name of type for storage purposes
            string typeName = typeof(T).Name;

            var cacheStatus = await Storage.GetCacheStatusAsync(typeName, id);
            if (forceLoadStatuses.Contains(cacheStatus) || forceRefresh)
            {
                // check proxy provider if defined
                if (ProxyProvider != null)
                {
                    if (ProxyProvider.Storage == null)
                    {
                        ProxyProvider.Storage = this.Storage;
                    }
                    var proxyResult = await ProxyProvider.GetEntityAsync<T>(endpoint, id);
                    if (proxyResult != null)
                    {
                        return null;
                    }
                }

                // fall back to direct IGDB API call if no proxy provider available
                if (IGDBAuthToken != null)
                {
                    // call the IGDB API directly
                    string body = "fields *; where id = " + id + ";";
                    var requestUrl = $"{IGDBUrl}{endpoint}";
                    var headers = IGDBAuthToken.ToHeaders();
                    var response = await comms.SendRequestAsync<Dictionary<string, object>[]>(HTTPComms.HttpMethod.POST, requestUrl, headers, body);
                    if (response.StatusCode == 200 && response.Body != null && response.Body.Length > 0)
                    {
                        Dictionary<string, object> item = response.Body[0];

                        T? result = ConvertToEntity<T>(item);

                        if (result != null)
                        {
                            // save to storage
                            _ = Storage.StoreCacheValue<T>(result);

                            return result;
                        }
                    }
                }
            }
            else
            {
                // load from storage
                T? cachedItem = await Storage.GetCacheValue<T>(metadata, "id", id);

                if (cachedItem != null)
                {
                    return cachedItem;
                }
            }

            return null;
        }

        private static T ConvertToEntity<T>(Dictionary<string, object> item) where T : class
        {
            // update any attributes of type DateTimeOffset from epoch seconds to proper DateTimeOffset
            var properties = typeof(T).GetProperties();
            foreach (var prop in properties)
            {
                var underlyingType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                if (underlyingType == typeof(DateTimeOffset))
                {
                    // Check for JsonPropertyName attribute first, fall back to property name
                    string keyName = prop.Name;
                    var jsonPropertyNameAttr = (System.Text.Json.Serialization.JsonPropertyNameAttribute?)Attribute.GetCustomAttribute(prop, typeof(System.Text.Json.Serialization.JsonPropertyNameAttribute));
                    if (jsonPropertyNameAttr != null && !string.IsNullOrEmpty(jsonPropertyNameAttr.Name))
                    {
                        keyName = jsonPropertyNameAttr.Name;
                    }
                    else
                    {
                        // Also check Newtonsoft.Json JsonProperty attribute
                        var newtonsoftAttr = (Newtonsoft.Json.JsonPropertyAttribute?)Attribute.GetCustomAttribute(prop, typeof(Newtonsoft.Json.JsonPropertyAttribute));
                        if (newtonsoftAttr != null && !string.IsNullOrEmpty(newtonsoftAttr.PropertyName))
                        {
                            keyName = newtonsoftAttr.PropertyName;
                        }
                    }

                    // Try to find the key in the dictionary (case-insensitive if needed)
                    string? actualKey = item.Keys.FirstOrDefault(k => k.Equals(keyName, StringComparison.OrdinalIgnoreCase));
                    if (actualKey != null && long.TryParse(item[actualKey].ToString(), out long epochSeconds))
                    {
                        DateTimeOffset dto = DateTimeOffset.FromUnixTimeSeconds(epochSeconds);
                        item[actualKey] = dto;
                    }
                }
            }

            // convert dictionary to target type
            string json = System.Text.Json.JsonSerializer.Serialize(item);
            T? result = System.Text.Json.JsonSerializer.Deserialize<T>(json);

            return result!;
        }
    }
}