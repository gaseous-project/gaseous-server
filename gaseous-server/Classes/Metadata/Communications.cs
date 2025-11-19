using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Net;
using System.Reflection;
using System.Security.Policy;
using System.Threading.Tasks;
using HasheousClient.Models.Metadata.IGDB;
using Humanizer;
using IGDB;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RestEase;

namespace gaseous_server.Classes.Metadata
{
    /// <summary>
    /// Handles all metadata API communications
    /// </summary>
    public class Communications
    {
        static Communications()
        {
            var handler = new HttpClientHandler();
            handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            client = new HttpClient(handler);

            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "deflate");
        }

        // configure the IGDB client
        private static IGDBClient igdb = new IGDBClient(
                    // Found in Twitch Developer portal for your app
                    Config.IGDB.ClientId,
                    Config.IGDB.Secret
                );

        private static HasheousClient.Hasheous _hasheous { get; set;}
        // provide the hasheous client
        public static HasheousClient.Hasheous hasheousClient
        {
            get
            {
                if (_hasheous != null)
                {
                    return _hasheous;
                }

                // configure the Hasheous client
                _hasheous = new HasheousClient.Hasheous();

                // set the base URI
                if (HasheousClient.WebApp.HttpHelper.BaseUri == null)
                {
                    string HasheousHost = "";
                    if (Config.MetadataConfiguration.HasheousHost == null)
                    {
                        HasheousHost = "https://hasheous.org/";
                    }
                    else
                    {
                        HasheousHost = Config.MetadataConfiguration.HasheousHost;
                    }
                    HasheousClient.WebApp.HttpHelper.BaseUri = HasheousHost;
                }

                // set the client API key
                HasheousClient.WebApp.HttpHelper.ClientKey = Config.MetadataConfiguration.HasheousClientAPIKey;
                if (client.DefaultRequestHeaders.Contains("X-Client-API-Key"))
                {
                    client.DefaultRequestHeaders.Remove("X-Client-API-Key");
                }
                client.DefaultRequestHeaders.Add("X-Client-API-Key", Config.MetadataConfiguration.HasheousClientAPIKey);

                // set the client secret
                if (Config.MetadataConfiguration.HasheousAPIKey != null)
                {
                    HasheousClient.WebApp.HttpHelper.APIKey = Config.MetadataConfiguration.HasheousAPIKey;
                }
                
                return _hasheous;
            }
        }


        private static HttpClient client = new HttpClient();

        /// <summary>
        /// Configure metadata API communications
        /// </summary>
        public static FileSignature.MetadataSources MetadataSource
        {
            get
            {
                return _MetadataSource;
            }
            set
            {
                _MetadataSource = value;

                switch (value)
                {
                    case FileSignature.MetadataSources.IGDB:
                        if (Config.IGDB.UseHasheousProxy == false)
                        {
                            // set rate limiter avoidance values
                            RateLimitAvoidanceWait = 1500;
                            RateLimitAvoidanceThreshold = 3;
                            RateLimitAvoidancePeriod = 1;

                            // set rate limiter recovery values
                            RateLimitRecoveryWaitTime = 10000;
                        }
                        else
                        {
                            // set rate limiter avoidance values
                            RateLimitAvoidanceWait = 1500;
                            RateLimitAvoidanceThreshold = 6;
                            RateLimitAvoidancePeriod = 1;

                            // set rate limiter recovery values
                            RateLimitRecoveryWaitTime = 10000;
                        }

                        break;

                    default:
                        // leave all values at default
                        break;
                }
            }
        }
        private static FileSignature.MetadataSources _MetadataSource = FileSignature.MetadataSources.None;

        // rate limit avoidance - what can we do to ensure that rate limiting is avoided?
        // these values affect all communications

        /// <summary>
        /// How long to wait to avoid hitting an API rate limiter
        /// </summary>
        private static int RateLimitAvoidanceWait = 2000;

        /// <summary>
        /// How many API calls in the period are allowed before we start introducing a wait
        /// </summary>
        private static int RateLimitAvoidanceThreshold = 80;

        /// <summary>
        /// A counter of API calls since the beginning of the period
        /// </summary>
        private static int RateLimitAvoidanceCallCount = 0;

        /// <summary>
        /// How large the period (in seconds) to measure API call counts against
        /// </summary>
        private static int RateLimitAvoidancePeriod = 60;

        /// <summary>
        /// The start of the rate limit avoidance period
        /// </summary>
        private static DateTime RateLimitAvoidanceStartTime = DateTime.UtcNow;

        /// <summary>
        /// Used to determine if we're already in rate limit avoidance mode - always query "InRateLimitAvoidanceMode"
        /// for up to date mode status.
        /// This bool is used to track status changes and should not be relied upon for current status.
        /// </summary>
        private static bool InRateLimitAvoidanceModeStatus = false;

        /// <summary>
        /// Determine if we're in rate limit avoidance mode.
        /// </summary>
        private static bool InRateLimitAvoidanceMode
        {
            get
            {
                if (RateLimitAvoidanceStartTime.AddSeconds(RateLimitAvoidancePeriod) <= DateTime.UtcNow)
                {
                    // avoidance period has expired - reset
                    RateLimitAvoidanceCallCount = 0;
                    RateLimitAvoidanceStartTime = DateTime.UtcNow;

                    return false;
                }
                else
                {
                    // we're in the avoidance period
                    if (RateLimitAvoidanceCallCount > RateLimitAvoidanceThreshold)
                    {
                        // the number of call counts indicates we should throttle things a bit
                        if (InRateLimitAvoidanceModeStatus == false)
                        {
                            Logging.LogKey(Logging.LogType.Information, "process.api_connection", "apiconnection.entered_rate_limit_avoidance_period", null, new string[] { RateLimitAvoidanceWait.ToString() });
                            InRateLimitAvoidanceModeStatus = true;
                        }
                        return true;
                    }
                    else
                    {
                        // still in full speed mode - no throttle required
                        if (InRateLimitAvoidanceModeStatus == true)
                        {
                            Logging.LogKey(Logging.LogType.Information, "process.api_connection", "apiconnection.exited_rate_limit_avoidance_period");
                            InRateLimitAvoidanceModeStatus = false;
                        }
                        return false;
                    }
                }
            }
        }

        // rate limit handling - how long to wait to allow the server to recover and try again
        // these values affect ALL communications if a 429 response code is received

        /// <summary>
        /// How long to wait (in milliseconds) if a 429 status code is received before trying again
        /// </summary>
        private static int RateLimitRecoveryWaitTime = 10000;

        /// <summary>
        /// The time when normal communications can attempt to be resumed
        /// </summary>
        private static DateTime RateLimitResumeTime = DateTime.UtcNow.AddMinutes(5 * -1);

        // rate limit retry - how many times to retry before aborting
        private int RetryAttempts = 0;
        private int RetryAttemptsMax = 3;

        public enum MetadataEndpoint
        {
            AgeGroup,
            AgeRating,
            AgeRatingCategory,
            AgeRatingContentDescription,
            AgeRatingContentDescriptionV2,
            AgeRatingOrganization,
            AlternativeName,
            Artwork,
            Character,
            CharacterGender,
            CharacterMugShot,
            CharacterSpecies,
            Collection,
            CollectionMembership,
            CollectionMembershipType,
            CollectionRelation,
            CollectionRelationType,
            CollectionType,
            Company,
            CompanyLogo,
            CompanyStatus,
            CompanyWebsite,
            Country,
            Cover,
            Event,
            EventLogo,
            EventNetwork,
            ExternalGame,
            ExternalGameSource,
            Franchise,
            Game,
            GameEngine,
            GameEngineLogo,
            GameLocalization,
            GameMode,
            GameReleaseFormat,
            GameStatus,
            GameTimeToBeat,
            GameType,
            GameVersion,
            GameVersionFeature,
            GameVersionFeatureValue,
            GameVideo,
            Genre,
            InvolvedCompany,
            Keyword,
            Language,
            LanguageSupport,
            LanguageSupportType,
            MultiplayerMode,
            NetworkType,
            Platform,
            PlatformFamily,
            PlatformLogo,
            PlatformVersion,
            PlatformVersionCompany,
            PlatformVersionReleaseDate,
            PlatformWebsite,
            PlayerPerspective,
            PopularityPrimitive,
            PopularityType,
            Region,
            ReleaseDate,
            ReleaseDateRegion,
            ReleaseDateStatus,
            Search,
            Screenshot,
            Theme,
            Website,
            WebsiteType
        }

        /// <summary>
        /// Request data from the metadata API using a slug using the default source
        /// </summary>
        /// <typeparam name="T">
        /// The type of object to return
        /// </typeparam>
        /// <param name="Endpoint">
        /// The endpoint to access - can only be either Platform or Game
        /// </param>
        /// <param name="Slug">
        /// The slug to query for
        /// </param>
        /// <returns>
        /// The object requested
        /// </returns>
        public async Task<T[]?> APIComm<T>(MetadataEndpoint Endpoint, string Slug)
        {
            return await APIComm<T>(_MetadataSource, Endpoint, Slug);
        }

        /// <summary>
        /// Request data from the metadata API using a slug
        /// </summary>
        /// <typeparam name="T">
        /// The type of object to return
        /// </typeparam>
        /// <param name="Endpoint">
        /// The endpoint to access - can only be either Platform or Game
        /// </param>
        /// <param name="Slug">
        /// The slug to query for
        /// </param>
        /// <returns>
        /// The object requested
        /// </returns>
        public async Task<T[]?> APIComm<T>(FileSignature.MetadataSources SourceType, MetadataEndpoint Endpoint, string Slug)
        {
            switch (SourceType)
            {
                case FileSignature.MetadataSources.None:
                    return null;
                case FileSignature.MetadataSources.IGDB:
                    if (Config.IGDB.UseHasheousProxy == false)
                    {
                        string fieldList = "fields *;";
                        string query = "where slug = \"" + Slug + "\"";
                        string EndpointString = "";

                        switch (Endpoint)
                        {
                            case MetadataEndpoint.Platform:
                                EndpointString = IGDBClient.Endpoints.Platforms;
                                break;

                            case MetadataEndpoint.Game:
                                EndpointString = IGDBClient.Endpoints.Games;
                                break;

                            default:
                                throw new ArgumentException("Invalid endpoint specified: " + Endpoint.ToString());

                        }

                        return await IGDBAPI<T>(EndpointString, fieldList, query);
                    }
                    else
                    {
                        return await HasheousAPI<T>(SourceType, Endpoint.ToString(), "slug", Slug);
                    }

                case FileSignature.MetadataSources.TheGamesDb:
                    // not implemented
                    return null;

                default:
                    return null;
            }
        }

        /// <summary>
        /// Request data from the metadata API using an id using the default source
        /// </summary>
        /// <typeparam name="T">
        /// The type of object to return
        /// </typeparam>
        /// <param name="Endpoint">
        /// The endpoint to access
        /// </param>
        /// <param name="Id">
        /// The Id to query for
        /// </param>
        /// <returns>
        /// The object requested
        /// </returns>
        public async Task<T[]> APIComm<T>(MetadataEndpoint Endpoint, long Id)
        {
            return await APIComm<T>(_MetadataSource, Endpoint, Id);
        }

        /// <summary>
        /// Request data from the metadata API using an id
        /// </summary>
        /// <typeparam name="T">
        /// The type of object to return
        /// </typeparam>
        /// <param name="SourceType">
        /// The source of the metadata
        /// </param>
        /// <param name="Endpoint">
        /// The endpoint to access
        /// </param>
        /// <param name="Id">
        /// The Id to query for
        /// </param>
        /// <returns>
        /// The object requested
        /// </returns>
        public async Task<T[]> APIComm<T>(FileSignature.MetadataSources SourceType, MetadataEndpoint Endpoint, long Id)
        {
            switch (SourceType)
            {
                case FileSignature.MetadataSources.None:
                    return null;
                case FileSignature.MetadataSources.IGDB:
                    if (Config.IGDB.UseHasheousProxy == false)
                    {
                        string fieldList = "fields *;";
                        string query = "where id = " + Id;
                        string EndpointString = GetEndpointData<T>().Endpoint;

                        return await IGDBAPI<T>(EndpointString, fieldList, query);
                    }
                    else
                    {
                        return await HasheousAPI<T>(SourceType, Endpoint.ToString(), "id", Id.ToString());
                    }

                case FileSignature.MetadataSources.TheGamesDb:
                    switch (Endpoint)
                    {
                        case MetadataEndpoint.Game:
                            return await HasheousAPI<T>(SourceType, Endpoint.ToString(), "id", Id.ToString());

                        case MetadataEndpoint.Genre:
                            return await HasheousAPI<T>(SourceType, Endpoint.ToString(), "id", Id.ToString());

                        default:
                            return null;
                    }

                    break;
                default:
                    return null;
            }
        }

        public static EndpointDataItem GetEndpointData<T>()
        {
            // use reflection to get the endpoint for the type T. The endpoint is a public const and is the name of the type, and is under IGDBClient.Endpoints
            var typeName = typeof(T).Name;
            EndpointDataItem endpoint = new EndpointDataItem();

            switch (typeName)
            {
                case "AgeRating":
                    endpoint.Endpoint = "age_ratings";
                    break;

                case "AgeRatingCategory":
                    endpoint.Endpoint = "age_rating_categories";
                    break;

                case "AgeRatingContentDescriptionV2":
                    endpoint.Endpoint = "age_rating_content_descriptions_v2";
                    break;

                case "AgeRatingOrganization":
                    endpoint.Endpoint = "age_rating_organizations";
                    break;

                case "AlternativeName":
                    endpoint.Endpoint = "alternative_names";
                    break;

                case "Artwork":
                    endpoint.Endpoint = "artworks";
                    break;

                case "Character":
                    endpoint.Endpoint = "characters";
                    break;

                case "CharacterGender":
                    endpoint.Endpoint = "character_genders";
                    break;

                case "CharacterMugshot":
                    endpoint.Endpoint = "character_mug_shots";
                    break;

                case "CharacterSpecies":
                    endpoint.Endpoint = "character_species";
                    break;

                case "Collection":
                    endpoint.Endpoint = "collections";
                    endpoint.SupportsSlugSearch = true;
                    break;

                case "CollectionMembership":
                    endpoint.Endpoint = "collection_memberships";
                    break;

                case "CollectionMembershipType":
                    endpoint.Endpoint = "collection_membership_types";
                    break;

                case "CollectionRelation":
                    endpoint.Endpoint = "collection_relations";
                    break;

                case "CollectionRelationType":
                    endpoint.Endpoint = "collection_relation_types";
                    break;

                case "CollectionType":
                    endpoint.Endpoint = "collection_types";
                    break;

                case "Company":
                    endpoint.Endpoint = "companies";
                    endpoint.SupportsSlugSearch = true;
                    break;

                case "CompanyLogo":
                    endpoint.Endpoint = "company_logos";
                    break;

                case "CompanyStatus":
                    endpoint.Endpoint = "company_statuses";
                    break;

                case "CompanyWebsite":
                    endpoint.Endpoint = "company_websites";
                    break;

                case "Cover":
                    endpoint.Endpoint = "covers";
                    break;

                case "DateFormat":
                    endpoint.Endpoint = "date_formats";
                    break;

                case "Event":
                    endpoint.Endpoint = "events";
                    break;

                case "EventLogo":
                    endpoint.Endpoint = "event_logos";
                    break;

                case "EventNetwork":
                    endpoint.Endpoint = "event_networks";
                    break;

                case "ExternalGame":
                    endpoint.Endpoint = "external_games";
                    break;

                case "ExternalGameSource":
                    endpoint.Endpoint = "external_game_sources";
                    break;

                case "Franchise":
                    endpoint.Endpoint = "franchises";
                    endpoint.SupportsSlugSearch = true;
                    break;

                case "Game":
                    endpoint.Endpoint = "games";
                    endpoint.SupportsSlugSearch = true;
                    break;

                case "GameEngine":
                    endpoint.Endpoint = "game_engines";
                    break;

                case "GameEngineLogo":
                    endpoint.Endpoint = "game_engine_logos";
                    break;

                case "GameLocalization":
                    endpoint.Endpoint = "game_localizations";
                    break;

                case "GameMode":
                    endpoint.Endpoint = "game_modes";
                    break;

                case "GameReleaseFormat":
                    endpoint.Endpoint = "game_release_formats";
                    break;

                case "GameStatus":
                    endpoint.Endpoint = "game_statuses";
                    break;

                case "GameTimeToBeat":
                    endpoint.Endpoint = "game_time_to_beats";
                    break;

                case "GameType":
                    endpoint.Endpoint = "game_types";
                    break;

                case "GameVersion":
                    endpoint.Endpoint = "game_versions";
                    break;

                case "GameVersionFeature":
                    endpoint.Endpoint = "game_version_features";
                    break;

                case "GameVersionFeatureValue":
                    endpoint.Endpoint = "game_version_feature_values";
                    break;

                case "GameVideo":
                    endpoint.Endpoint = "game_videos";
                    break;

                case "Genre":
                    endpoint.Endpoint = "genres";
                    break;

                case "Keyword":
                    endpoint.Endpoint = "keywords";
                    break;

                case "InvolvedCompany":
                    endpoint.Endpoint = "involved_companies";
                    break;

                case "Language":
                    endpoint.Endpoint = "languages";
                    break;

                case "LanguageSupport":
                    endpoint.Endpoint = "language_supports";
                    break;

                case "LanguageSupportType":
                    endpoint.Endpoint = "language_support_types";
                    break;

                case "MultiplayerMode":
                    endpoint.Endpoint = "multiplayer_modes";
                    break;

                case "NetworkType":
                    endpoint.Endpoint = "network_types";
                    break;

                case "Platform":
                    endpoint.Endpoint = "platforms";
                    endpoint.SupportsSlugSearch = true;
                    break;

                case "PlatformFamily":
                    endpoint.Endpoint = "platform_families";
                    break;

                case "PlatformLogo":
                    endpoint.Endpoint = "platform_logos";
                    break;

                case "PlatformType":
                    endpoint.Endpoint = "platform_types";
                    break;

                case "PlatformVersion":
                    endpoint.Endpoint = "platform_versions";
                    break;

                case "PlatformVersionCompany":
                    endpoint.Endpoint = "platform_version_companies";
                    break;

                case "PlatformVersionReleaseDate":
                    endpoint.Endpoint = "platform_version_release_dates";
                    break;

                case "PlatformWebsite":
                    endpoint.Endpoint = "platform_websites";
                    break;

                case "PlayerPerspective":
                    endpoint.Endpoint = "player_perspectives";
                    break;

                case "PopularityPrimitive":
                    endpoint.Endpoint = "popularity_primitives";
                    break;

                case "PopularityType":
                    endpoint.Endpoint = "popularity_types";
                    break;

                case "Region":
                    endpoint.Endpoint = "regions";
                    break;

                case "ReleaseDate":
                    endpoint.Endpoint = "release_dates";
                    break;

                case "ReleaseDateRegion":
                    endpoint.Endpoint = "release_date_regions";
                    break;

                case "ReleaseDateStatus":
                    endpoint.Endpoint = "release_date_statuses";
                    break;

                case "Screenshot":
                    endpoint.Endpoint = "screenshots";
                    break;

                case "Search":
                    endpoint.Endpoint = "search";
                    break;

                case "Theme":
                    endpoint.Endpoint = "themes";
                    break;

                case "Website":
                    endpoint.Endpoint = "websites";
                    break;

                case "WebsiteType":
                    endpoint.Endpoint = "website_types";
                    break;

                default:
                    var endpointField = typeof(IGDBClient.Endpoints).GetField(typeName);
                    if (endpointField == null)
                    {
                        // try again with pluralized type name
                        endpointField = typeof(IGDBClient.Endpoints).GetField(typeName + "s");

                        if (endpointField == null)
                            return null;
                    }

                    endpoint.Endpoint = (string)endpointField.GetValue(null);
                    break;
            }

            return endpoint;
        }

        public class EndpointDataItem
        {
            public string Endpoint { get; set; }
            public bool SupportsSlugSearch { get; set; } = false;
        }

        /// <summary>
        /// Request data from the metadata API - this is only valid for IGDB
        /// </summary>
        /// <typeparam name="T">Type of object to return</typeparam>
        /// <param name="Endpoint">API endpoint segment to use</param>
        /// <param name="Fields">Fields to request from the API</param>
        /// <param name="Query">Selection criteria for data to request</param>
        /// <returns></returns>
        public async Task<T[]?> APIComm<T>(string Endpoint, string Fields, string Query)
        {
            switch (_MetadataSource)
            {
                case FileSignature.MetadataSources.None:
                    return null;
                case FileSignature.MetadataSources.IGDB:
                    if (Config.IGDB.UseHasheousProxy == false)
                    {
                        return await IGDBAPI<T>(Endpoint, Fields, Query);
                    }
                    else
                    {
                        return null;
                    }
                default:
                    return null;
            }
        }

        private async Task<T[]> IGDBAPI<T>(string Endpoint, string Fields, string Query)
        {
            Logging.LogKey(Logging.LogType.Debug, "process.api_connection", "apiconnection.accessing_api_for_endpoint", null, new string[] { Endpoint });

            if (RateLimitResumeTime > DateTime.UtcNow)
            {
                Logging.LogKey(Logging.LogType.Information, "process.api_connection", "apiconnection.igdb_rate_limit_hit_pausing_until", null, new string[] { RateLimitResumeTime.ToString(), RetryAttempts.ToString(), RetryAttemptsMax.ToString() });
                Thread.Sleep(RateLimitRecoveryWaitTime);
            }

            try
            {
                if (InRateLimitAvoidanceMode == true)
                {
                    // sleep for a moment to help avoid hitting the rate limiter
                    Logging.LogKey(Logging.LogType.Information, "process.api_connection", "apiconnection.igdb_rate_limit_hit_pausing_milliseconds", null, new string[] { Endpoint, RateLimitAvoidanceWait.ToString() });
                    Thread.Sleep(RateLimitAvoidanceWait);
                }

                // perform the actual API call
                string queryString = Fields + " " + Query + ";";
                var results = await igdb.QueryAsync<T>(Endpoint, query: queryString);

                // increment rate limiter avoidance call count
                RateLimitAvoidanceCallCount += 1;

                return results;
            }
            catch (ApiException apiEx)
            {
                switch (apiEx.StatusCode)
                {
                    case HttpStatusCode.TooManyRequests:
                        if (RetryAttempts >= RetryAttemptsMax)
                        {
                            Logging.LogKey(Logging.LogType.Warning, "process.api_connection", "apiconnection.igdb_rate_limiter_attempts_expired_aborting", null, null, apiEx);
                            throw;
                        }
                        else
                        {
                            Logging.LogKey(Logging.LogType.Information, "process.api_connection", "apiconnection.igdb_api_rate_limit_hit_accessing_endpoint", null, new string[] { Endpoint }, apiEx);

                            RetryAttempts += 1;

                            return await IGDBAPI<T>(Endpoint, Fields, Query);
                        }

                    case HttpStatusCode.Unauthorized:
                        Logging.LogKey(Logging.LogType.Information, "process.api_connection", "apiconnection.igdb_api_unauthorised_error_accessing_endpoint_waiting_milliseconds_resetting_client", null, new string[] { Endpoint, RateLimitAvoidanceWait.ToString() }, apiEx);

                        Thread.Sleep(RateLimitAvoidanceWait);

                        igdb = new IGDBClient(
                            // Found in Twitch Developer portal for your app
                            Config.IGDB.ClientId,
                            Config.IGDB.Secret
                        );

                        RetryAttempts += 1;

                        return await IGDBAPI<T>(Endpoint, Fields, Query);

                    default:
                        Logging.LogKey(Logging.LogType.Warning, "process.api_connection", "apiconnection.exception_accessing_endpoint", null, new string[] { Endpoint }, apiEx);
                        throw;
                }
            }
            catch (Exception ex)
            {
                Logging.LogKey(Logging.LogType.Warning, "process.api_connection", "apiconnection.exception_accessing_endpoint", null, new string[] { Endpoint }, ex);
                throw;
            }
        }

        #region Hasheous API Call
        /// <summary>
        /// Access the HasheousAPI
        /// </summary>
        /// <typeparam name="T">
        /// The type of object to return
        /// </typeparam>
        /// <param name="SourceType">
        /// The source of the metadata
        /// </param>
        /// <param name="Endpoint">
        /// The endpoint to access
        /// </param>
        /// <param name="Fields">
        /// Can be either "slug" or "id" - note not all endpoints support slug
        /// </param>
        /// <param name="Query">
        /// The "slug" or "id" to query for
        /// </param>
        /// <returns>
        /// The object requested
        /// </returns>
        private async Task<T[]> HasheousAPI<T>(FileSignature.MetadataSources SourceType, string Endpoint, string Fields, string Query)
        {
            Logging.LogKey(Logging.LogType.Debug, "process.api_connection", "apiconnection.accessing_api_for_endpoint", null, new string[] { Endpoint });

            if (RateLimitResumeTime > DateTime.UtcNow)
            {
                Logging.LogKey(Logging.LogType.Information, "process.api_connection", "apiconnection.hasheous_rate_limit_hit_pausing_until", null, new string[] { RateLimitResumeTime.ToString(), RetryAttempts.ToString(), RetryAttemptsMax.ToString() });
                Thread.Sleep(RateLimitRecoveryWaitTime);
            }

            try
            {
                if (InRateLimitAvoidanceMode == true)
                {
                    // sleep for a moment to help avoid hitting the rate limiter
                    Logging.LogKey(Logging.LogType.Information, "process.api_connection", "apiconnection.hasheous_rate_limit_hit_pausing_milliseconds", null, new string[] { Endpoint, RateLimitAvoidanceWait.ToString() });
                    Thread.Sleep(RateLimitAvoidanceWait);
                }

                // perform the actual API call
                var results1 = HasheousAPIFetch<T>(SourceType, Endpoint, Fields, Query).Result;

                // increment rate limiter avoidance call count
                RateLimitAvoidanceCallCount += 1;

                return results1;
            }
            catch (ApiException apiEx)
            {
                switch (apiEx.StatusCode)
                {
                    case HttpStatusCode.TooManyRequests:
                        if (RetryAttempts >= RetryAttemptsMax)
                        {
                            Logging.LogKey(Logging.LogType.Warning, "process.api_connection", "apiconnection.hasheous_rate_limiter_attempts_expired_aborting", null, null, apiEx);
                            throw;
                        }
                        else
                        {
                            Logging.LogKey(Logging.LogType.Information, "process.api_connection", "apiconnection.hasheous_api_rate_limit_hit_accessing_endpoint", null, new string[] { Endpoint }, apiEx);

                            RetryAttempts += 1;

                            // perform the actual API call
                            var results2 = await HasheousAPIFetch<T>(SourceType, Endpoint, Fields, Query);

                            return results2;
                        }

                    case HttpStatusCode.Unauthorized:
                        Logging.LogKey(Logging.LogType.Information, "process.api_connection", "apiconnection.hasheous_api_unauthorised_error_accessing_endpoint_waiting_milliseconds_resetting_client", null, new string[] { Endpoint, RateLimitAvoidanceWait.ToString() }, apiEx);

                        Thread.Sleep(RateLimitAvoidanceWait);

                        igdb = new IGDBClient(
                            // Found in Twitch Developer portal for your app
                            Config.IGDB.ClientId,
                            Config.IGDB.Secret
                        );

                        RetryAttempts += 1;

                        // perform the actual API call
                        var results3 = await HasheousAPIFetch<T>(SourceType, Endpoint, Fields, Query);

                        return results3;

                    default:
                        Logging.LogKey(Logging.LogType.Warning, "process.api_connection", "apiconnection.exception_accessing_endpoint", null, new string[] { Endpoint }, apiEx);
                        throw;
                }
            }
            catch (Exception ex)
            {
                Logging.LogKey(Logging.LogType.Warning, "process.api_connection", "apiconnection.exception_accessing_endpoint", null, new string[] { Endpoint }, ex);
                throw;
            }
        }
        #endregion Hasheous API Call

        private async Task<T[]> HasheousAPIFetch<T>(FileSignature.MetadataSources SourceType, string Endpoint, string Fields, object Query)
        {
            // drop out early if Fields is not valid
            if (Fields != "slug" && Fields != "id")
            {
                throw new Exception("Fields must be either 'slug' or 'id'");
            }

            // get type name of T
            string typeName = typeof(T).Name.ToLower();

            switch (SourceType)
            {
                case FileSignature.MetadataSources.IGDB:
                    switch (typeName)
                    {
                        case "agerating":
                            HasheousClient.Models.Metadata.IGDB.AgeRating ageRatingResult = new HasheousClient.Models.Metadata.IGDB.AgeRating();
                            ageRatingResult = await hasheousClient.GetMetadataProxyAsync<HasheousClient.Models.Metadata.IGDB.AgeRating>(Endpoint, HasheousClient.Hasheous.MetadataProvider.IGDB, long.Parse(Query.ToString()));

                            return new T[] { ConvertToIGDBModel<T>(ageRatingResult) };

                        case "ageratingcategory":
                            HasheousClient.Models.Metadata.IGDB.AgeRatingCategory ageRatingCategoryResult = new HasheousClient.Models.Metadata.IGDB.AgeRatingCategory();
                            ageRatingCategoryResult = await hasheousClient.GetMetadataProxyAsync<HasheousClient.Models.Metadata.IGDB.AgeRatingCategory>(Endpoint, HasheousClient.Hasheous.MetadataProvider.IGDB, long.Parse(Query.ToString()));

                            return new T[] { ConvertToIGDBModel<T>(ageRatingCategoryResult) };

                        case "ageratingcontentdescriptionv2":
                            HasheousClient.Models.Metadata.IGDB.AgeRatingContentDescriptionV2 ageRatingContentDescriptionResult = new HasheousClient.Models.Metadata.IGDB.AgeRatingContentDescriptionV2();
                            ageRatingContentDescriptionResult = await hasheousClient.GetMetadataProxyAsync<HasheousClient.Models.Metadata.IGDB.AgeRatingContentDescriptionV2>(Endpoint, HasheousClient.Hasheous.MetadataProvider.IGDB, long.Parse(Query.ToString()));

                            return new T[] { ConvertToIGDBModel<T>(ageRatingContentDescriptionResult) };

                        case "ageratingorganization":
                            HasheousClient.Models.Metadata.IGDB.AgeRatingOrganization ageRatingOrganizationResult = new HasheousClient.Models.Metadata.IGDB.AgeRatingOrganization();
                            ageRatingOrganizationResult = await hasheousClient.GetMetadataProxyAsync<HasheousClient.Models.Metadata.IGDB.AgeRatingOrganization>(Endpoint, HasheousClient.Hasheous.MetadataProvider.IGDB, long.Parse(Query.ToString()));

                            return new T[] { ConvertToIGDBModel<T>(ageRatingOrganizationResult) };

                        case "alternativename":
                            HasheousClient.Models.Metadata.IGDB.AlternativeName alternativeNameResult = new HasheousClient.Models.Metadata.IGDB.AlternativeName();
                            alternativeNameResult = await hasheousClient.GetMetadataProxyAsync<HasheousClient.Models.Metadata.IGDB.AlternativeName>(Endpoint, HasheousClient.Hasheous.MetadataProvider.IGDB, long.Parse(Query.ToString()));

                            return new T[] { ConvertToIGDBModel<T>(alternativeNameResult) };

                        case "artwork":
                            HasheousClient.Models.Metadata.IGDB.Artwork artworkResult = new HasheousClient.Models.Metadata.IGDB.Artwork();
                            artworkResult = await hasheousClient.GetMetadataProxyAsync<HasheousClient.Models.Metadata.IGDB.Artwork>(Endpoint, HasheousClient.Hasheous.MetadataProvider.IGDB, long.Parse(Query.ToString()));

                            return new T[] { ConvertToIGDBModel<T>(artworkResult) };

                        case "collection":
                            HasheousClient.Models.Metadata.IGDB.Collection collectionResult = new HasheousClient.Models.Metadata.IGDB.Collection();
                            collectionResult = await hasheousClient.GetMetadataProxyAsync<HasheousClient.Models.Metadata.IGDB.Collection>(Endpoint, HasheousClient.Hasheous.MetadataProvider.IGDB, long.Parse(Query.ToString()));

                            return new T[] { ConvertToIGDBModel<T>(collectionResult) };

                        case "company":
                            HasheousClient.Models.Metadata.IGDB.Company companyResult = new HasheousClient.Models.Metadata.IGDB.Company();
                            companyResult = await hasheousClient.GetMetadataProxyAsync<HasheousClient.Models.Metadata.IGDB.Company>(Endpoint, HasheousClient.Hasheous.MetadataProvider.IGDB, long.Parse(Query.ToString()));

                            return new T[] { ConvertToIGDBModel<T>(companyResult) };

                        case "companylogo":
                            HasheousClient.Models.Metadata.IGDB.CompanyLogo companyLogoResult = new HasheousClient.Models.Metadata.IGDB.CompanyLogo();
                            companyLogoResult = await hasheousClient.GetMetadataProxyAsync<HasheousClient.Models.Metadata.IGDB.CompanyLogo>(Endpoint, HasheousClient.Hasheous.MetadataProvider.IGDB, long.Parse(Query.ToString()));

                            return new T[] { ConvertToIGDBModel<T>(companyLogoResult) };

                        case "companywebsite":
                            HasheousClient.Models.Metadata.IGDB.CompanyWebsite companyWebsiteResult = new HasheousClient.Models.Metadata.IGDB.CompanyWebsite();
                            companyWebsiteResult = await hasheousClient.GetMetadataProxyAsync<HasheousClient.Models.Metadata.IGDB.CompanyWebsite>(Endpoint, HasheousClient.Hasheous.MetadataProvider.IGDB, long.Parse(Query.ToString()));

                            return new T[] { ConvertToIGDBModel<T>(companyWebsiteResult) };

                        case "cover":
                            HasheousClient.Models.Metadata.IGDB.Cover coverResult = new HasheousClient.Models.Metadata.IGDB.Cover();
                            coverResult = await hasheousClient.GetMetadataProxyAsync<HasheousClient.Models.Metadata.IGDB.Cover>(Endpoint, HasheousClient.Hasheous.MetadataProvider.IGDB, long.Parse(Query.ToString()));

                            return new T[] { ConvertToIGDBModel<T>(coverResult) };

                        case "externalgame":
                            HasheousClient.Models.Metadata.IGDB.ExternalGame externalGameResult = new HasheousClient.Models.Metadata.IGDB.ExternalGame();
                            externalGameResult = await hasheousClient.GetMetadataProxyAsync<HasheousClient.Models.Metadata.IGDB.ExternalGame>(Endpoint, HasheousClient.Hasheous.MetadataProvider.IGDB, long.Parse(Query.ToString()));

                            return new T[] { ConvertToIGDBModel<T>(externalGameResult) };

                        case "franchise":
                            HasheousClient.Models.Metadata.IGDB.Franchise franchiseResult = new HasheousClient.Models.Metadata.IGDB.Franchise();
                            franchiseResult = await hasheousClient.GetMetadataProxyAsync<HasheousClient.Models.Metadata.IGDB.Franchise>(Endpoint, HasheousClient.Hasheous.MetadataProvider.IGDB, long.Parse(Query.ToString()));

                            return new T[] { ConvertToIGDBModel<T>(franchiseResult) };

                        case "game":
                            HasheousClient.Models.Metadata.IGDB.Game gameResult = new HasheousClient.Models.Metadata.IGDB.Game();
                            if (Fields == "slug")
                            {
                                gameResult = await hasheousClient.GetMetadataProxyAsync<HasheousClient.Models.Metadata.IGDB.Game>(Endpoint, HasheousClient.Hasheous.MetadataProvider.IGDB, Query.ToString());
                            }
                            else if (Fields == "id")
                            {
                                gameResult = await hasheousClient.GetMetadataProxyAsync<HasheousClient.Models.Metadata.IGDB.Game>(Endpoint, HasheousClient.Hasheous.MetadataProvider.IGDB, long.Parse(Query.ToString()));
                            }

                            return new T[] { ConvertToIGDBModel<T>(gameResult) };

                        case "gameengine":
                            HasheousClient.Models.Metadata.IGDB.GameEngine gameEngineResult = new HasheousClient.Models.Metadata.IGDB.GameEngine();
                            gameEngineResult = await hasheousClient.GetMetadataProxyAsync<HasheousClient.Models.Metadata.IGDB.GameEngine>(Endpoint, HasheousClient.Hasheous.MetadataProvider.IGDB, long.Parse(Query.ToString()));

                            return new T[] { ConvertToIGDBModel<T>(gameEngineResult) };

                        case "gameenginelogo":
                            HasheousClient.Models.Metadata.IGDB.GameEngineLogo gameEngineLogoResult = new HasheousClient.Models.Metadata.IGDB.GameEngineLogo();
                            gameEngineLogoResult = await hasheousClient.GetMetadataProxyAsync<HasheousClient.Models.Metadata.IGDB.GameEngineLogo>(Endpoint, HasheousClient.Hasheous.MetadataProvider.IGDB, long.Parse(Query.ToString()));

                            return new T[] { ConvertToIGDBModel<T>(gameEngineLogoResult) };

                        case "gamelocalization":
                            HasheousClient.Models.Metadata.IGDB.GameLocalization gameLocalizationResult = new HasheousClient.Models.Metadata.IGDB.GameLocalization();
                            gameLocalizationResult = await hasheousClient.GetMetadataProxyAsync<HasheousClient.Models.Metadata.IGDB.GameLocalization>(Endpoint, HasheousClient.Hasheous.MetadataProvider.IGDB, long.Parse(Query.ToString()));

                            return new T[] { ConvertToIGDBModel<T>(gameLocalizationResult) };

                        case "gamemode":
                            HasheousClient.Models.Metadata.IGDB.GameMode gameModeResult = new HasheousClient.Models.Metadata.IGDB.GameMode();
                            gameModeResult = await hasheousClient.GetMetadataProxyAsync<HasheousClient.Models.Metadata.IGDB.GameMode>(Endpoint, HasheousClient.Hasheous.MetadataProvider.IGDB, long.Parse(Query.ToString()));

                            return new T[] { ConvertToIGDBModel<T>(gameModeResult) };

                        case "gamevideo":
                            HasheousClient.Models.Metadata.IGDB.GameVideo gameVideoResult = new HasheousClient.Models.Metadata.IGDB.GameVideo();
                            gameVideoResult = await hasheousClient.GetMetadataProxyAsync<HasheousClient.Models.Metadata.IGDB.GameVideo>(Endpoint, HasheousClient.Hasheous.MetadataProvider.IGDB, long.Parse(Query.ToString()));

                            return new T[] { ConvertToIGDBModel<T>(gameVideoResult) };

                        case "genre":
                            HasheousClient.Models.Metadata.IGDB.Genre genreResult = new HasheousClient.Models.Metadata.IGDB.Genre();
                            genreResult = await hasheousClient.GetMetadataProxyAsync<HasheousClient.Models.Metadata.IGDB.Genre>(Endpoint, HasheousClient.Hasheous.MetadataProvider.IGDB, long.Parse(Query.ToString()));

                            return new T[] { ConvertToIGDBModel<T>(genreResult) };

                        case "involvedcompany":
                            HasheousClient.Models.Metadata.IGDB.InvolvedCompany involvedCompanyResult = new HasheousClient.Models.Metadata.IGDB.InvolvedCompany();
                            involvedCompanyResult = await hasheousClient.GetMetadataProxyAsync<HasheousClient.Models.Metadata.IGDB.InvolvedCompany>(Endpoint, HasheousClient.Hasheous.MetadataProvider.IGDB, long.Parse(Query.ToString()));

                            return new T[] { ConvertToIGDBModel<T>(involvedCompanyResult) };

                        case "multiplayermode":
                            HasheousClient.Models.Metadata.IGDB.MultiplayerMode multiplayerModeResult = new HasheousClient.Models.Metadata.IGDB.MultiplayerMode();
                            multiplayerModeResult = await hasheousClient.GetMetadataProxyAsync<HasheousClient.Models.Metadata.IGDB.MultiplayerMode>(Endpoint, HasheousClient.Hasheous.MetadataProvider.IGDB, long.Parse(Query.ToString()));

                            return new T[] { ConvertToIGDBModel<T>(multiplayerModeResult) };

                        case "platformlogo":
                            HasheousClient.Models.Metadata.IGDB.PlatformLogo platformLogoResult = new HasheousClient.Models.Metadata.IGDB.PlatformLogo();
                            platformLogoResult = await hasheousClient.GetMetadataProxyAsync<HasheousClient.Models.Metadata.IGDB.PlatformLogo>(Endpoint, HasheousClient.Hasheous.MetadataProvider.IGDB, long.Parse(Query.ToString()));

                            return new T[] { ConvertToIGDBModel<T>(platformLogoResult) };

                        case "platform":
                            HasheousClient.Models.Metadata.IGDB.Platform platformResult = new HasheousClient.Models.Metadata.IGDB.Platform();
                            if (Fields == "slug")
                            {
                                platformResult = await hasheousClient.GetMetadataProxyAsync<HasheousClient.Models.Metadata.IGDB.Platform>(Endpoint, HasheousClient.Hasheous.MetadataProvider.IGDB, Query.ToString());
                            }
                            else if (Fields == "id")
                            {
                                platformResult = await hasheousClient.GetMetadataProxyAsync<HasheousClient.Models.Metadata.IGDB.Platform>(Endpoint, HasheousClient.Hasheous.MetadataProvider.IGDB, long.Parse(Query.ToString()));
                            }

                            return new T[] { ConvertToIGDBModel<T>(platformResult) };

                        case "platformversion":
                            HasheousClient.Models.Metadata.IGDB.PlatformVersion platformVersionResult = new HasheousClient.Models.Metadata.IGDB.PlatformVersion();
                            platformVersionResult = await hasheousClient.GetMetadataProxyAsync<HasheousClient.Models.Metadata.IGDB.PlatformVersion>(Endpoint, HasheousClient.Hasheous.MetadataProvider.IGDB, long.Parse(Query.ToString()));

                            return new T[] { ConvertToIGDBModel<T>(platformVersionResult) };

                        case "playerperspective":
                            HasheousClient.Models.Metadata.IGDB.PlayerPerspective playerPerspectiveResult = new HasheousClient.Models.Metadata.IGDB.PlayerPerspective();
                            playerPerspectiveResult = await hasheousClient.GetMetadataProxyAsync<HasheousClient.Models.Metadata.IGDB.PlayerPerspective>(Endpoint, HasheousClient.Hasheous.MetadataProvider.IGDB, long.Parse(Query.ToString()));

                            return new T[] { ConvertToIGDBModel<T>(playerPerspectiveResult) };

                        case "releasedate":
                            HasheousClient.Models.Metadata.IGDB.ReleaseDate releaseDateResult = new HasheousClient.Models.Metadata.IGDB.ReleaseDate();
                            releaseDateResult = await hasheousClient.GetMetadataProxyAsync<HasheousClient.Models.Metadata.IGDB.ReleaseDate>(Endpoint, HasheousClient.Hasheous.MetadataProvider.IGDB, long.Parse(Query.ToString()));

                            return new T[] { ConvertToIGDBModel<T>(releaseDateResult) };

                        case "region":
                            HasheousClient.Models.Metadata.IGDB.Region regionResult = new HasheousClient.Models.Metadata.IGDB.Region();
                            regionResult = await hasheousClient.GetMetadataProxyAsync<HasheousClient.Models.Metadata.IGDB.Region>(Endpoint, HasheousClient.Hasheous.MetadataProvider.IGDB, long.Parse(Query.ToString()));

                            return new T[] { ConvertToIGDBModel<T>(regionResult) };

                        case "screenshot":
                            HasheousClient.Models.Metadata.IGDB.Screenshot screenshotResult = new HasheousClient.Models.Metadata.IGDB.Screenshot();
                            screenshotResult = await hasheousClient.GetMetadataProxyAsync<HasheousClient.Models.Metadata.IGDB.Screenshot>(Endpoint, HasheousClient.Hasheous.MetadataProvider.IGDB, long.Parse(Query.ToString()));

                            return new T[] { ConvertToIGDBModel<T>(screenshotResult) };

                        case "theme":
                            HasheousClient.Models.Metadata.IGDB.Theme themeResult = new HasheousClient.Models.Metadata.IGDB.Theme();
                            themeResult = await hasheousClient.GetMetadataProxyAsync<HasheousClient.Models.Metadata.IGDB.Theme>(Endpoint, HasheousClient.Hasheous.MetadataProvider.IGDB, long.Parse(Query.ToString()));

                            return new T[] { ConvertToIGDBModel<T>(themeResult) };

                        default:
                            throw new Exception("Type not supported");
                    }
                    break;

                case FileSignature.MetadataSources.TheGamesDb:
                    switch (typeName)
                    {
                        case "gamesbygameid":
                            HasheousClient.Models.Metadata.TheGamesDb.GamesByGameID gameResult = await hasheousClient.GetMetadataProxyAsync<HasheousClient.Models.Metadata.TheGamesDb.GamesByGameID>(Endpoint, HasheousClient.Hasheous.MetadataProvider.TheGamesDb, long.Parse(Query.ToString()));

                            // return the game object
                            return new T[] { (T)(object)gameResult };

                        case "genres":
                            HasheousClient.Models.Metadata.TheGamesDb.Genres genreResult = await hasheousClient.GetMetadataProxyAsync<HasheousClient.Models.Metadata.TheGamesDb.Genres>(Endpoint, HasheousClient.Hasheous.MetadataProvider.TheGamesDb, long.Parse(Query.ToString()));

                            // return the genre object
                            return new T[] { (T)(object)genreResult };

                        default:
                            throw new Exception("Type not supported");
                    }

                default:
                    throw new Exception("SourceType not supported");

            }
        }

        /// <summary>
        /// Convert an input object IGDB.Models object
        /// </summary>
        /// <typeparam name="T">The type of object to convert to</typeparam>
        /// <param name="input">The object to convert</param>
        /// <returns>The converted object</returns>
        public static T ConvertToIGDBModel<T>(object input)
        {
            // loop through the properties of intput and copy all strings to an output object of type T

            object output = Activator.CreateInstance(typeof(T));
            PropertyInfo[] properties = output.GetType().GetProperties();

            foreach (PropertyInfo property in properties)
            {
                string propertyTypeName = property.PropertyType.Name.ToLower();
                if (propertyTypeName == "nullable`1")
                {
                    propertyTypeName = property.PropertyType.GenericTypeArguments[0].Name.ToLower();
                }

                propertyTypeName = propertyTypeName.Replace("?", "");

                // check if the property is an enum
                if (property.PropertyType.IsEnum)
                {
                    // check if property is null
                    if (input.GetType().GetProperty(property.Name) != null)
                    {
                        if (input.GetType().GetProperty(property.Name).GetValue(input) != null)
                        {
                            // get the enum type
                            Type enumType = property.PropertyType;
                            // get the enum value
                            object enumValue = Enum.Parse(enumType, input.GetType().GetProperty(property.Name).GetValue(input).ToString());
                            // set the enum value
                            property.SetValue(output, enumValue);
                        }
                    }
                }
                else if (Common.IsNullableEnum(property.PropertyType))
                {
                    // check if property is null
                    if (input.GetType().GetProperty(property.Name).GetValue(input) != null)
                    {
                        // get the enum type
                        Type enumType = property.PropertyType;
                        // get the enum value
                        object enumValue = Enum.Parse(enumType.GenericTypeArguments[0], input.GetType().GetProperty(property.Name).GetValue(input).ToString());
                        // set the enum value
                        property.SetValue(output, enumValue);
                    }
                }
                else
                {
                    PropertyInfo inputProperty = input.GetType().GetProperty(property.Name);
                    if (inputProperty != null)
                    {
                        switch (propertyTypeName)
                        {
                            case "identityorvalue`1":
                                // create new identityorvalue object, set the id property to the input property value

                                // get the input property value
                                object inputPropertyValue = input.GetType().GetProperty(property.Name).GetValue(input);

                                if (inputPropertyValue != null)
                                {
                                    // create a new identityorvalue object
                                    object identityOrValue = Activator.CreateInstance(property.PropertyType);

                                    // set the id property of the identityorvalue object
                                    PropertyInfo idProperty = property.PropertyType.GetProperty("Id");
                                    idProperty.SetValue(identityOrValue, inputPropertyValue);

                                    // set the output property to the identityorvalue object
                                    property.SetValue(output, identityOrValue);
                                }
                                break;

                            case "identitiesorvalues`1":
                                // create new identitiesorvalues object, set the ids property to the input property value

                                // get the input property value
                                object inputPropertyValues = input.GetType().GetProperty(property.Name).GetValue(input);

                                if (inputPropertyValues != null)
                                {
                                    // convert input property values to a list of longs
                                    List<long> ids = new List<long>();
                                    foreach (object id in (IEnumerable)inputPropertyValues)
                                    {
                                        ids.Add((long)id);
                                    }

                                    // create a new identitiesorvalues object
                                    object identitiesOrValues = Activator.CreateInstance(property.PropertyType);

                                    // set the ids property of the identitiesorvalues object
                                    PropertyInfo idsProperty = property.PropertyType.GetProperty("Ids");
                                    idsProperty.SetValue(identitiesOrValues, ids.ToArray());

                                    // set the output property to the identitiesorvalues object
                                    property.SetValue(output, identitiesOrValues);
                                }
                                break;

                            default:
                                property.SetValue(output, inputProperty.GetValue(input));
                                break;
                        }
                    }
                }
            }

            return (T)output;
        }



        /// <summary>
        /// Download from the specified uri
        /// </summary>
        /// <param name="uri">The uri to download from</param>
        /// <param name="DestinationFile">The file name and path the download should be stored as</param>
        public async Task<bool?> DownloadFile(Uri uri, string DestinationFile)
        {
            var result = await _DownloadFile(uri, DestinationFile);

            return result;
        }

        #region Download File
        private async Task<bool?> _DownloadFile(Uri uri, string DestinationFile)
        {
            Logging.LogKey(Logging.LogType.Debug, "process.communications", "communications.download_attempt_from", null, new string[] { RetryAttempts.ToString(), RetryAttemptsMax.ToString(), uri.ToString() });

            if (RateLimitResumeTime > DateTime.UtcNow)
            {
                Logging.LogKey(Logging.LogType.Information, "process.communications", "communications.hasheous_rate_limit_hit_pausing_until", null, new string[] { RateLimitResumeTime.ToString(), RetryAttempts.ToString(), RetryAttemptsMax.ToString() });
                Thread.Sleep(RateLimitRecoveryWaitTime);
            }

            try
            {
                if (InRateLimitAvoidanceMode == true)
                {
                    // sleep for a moment to help avoid hitting the rate limiter
                    Logging.LogKey(Logging.LogType.Information, "process.communications", "communications.hasheous_rate_limit_hit_pausing_milliseconds", null, new string[] { uri.ToString(), RateLimitAvoidanceWait.ToString() });
                    Thread.Sleep(RateLimitAvoidanceWait);
                }

                string DestinationDirectory = new FileInfo(DestinationFile).Directory.FullName;
                if (!Directory.Exists(DestinationDirectory))
                {
                    Directory.CreateDirectory(DestinationDirectory);
                }

                Logging.LogKey(Logging.LogType.Information, "process.communications", "communications.downloading_from_to", null, new string[] { uri.ToString(), DestinationFile });

                using (HttpResponseMessage response = client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead).Result)
                {
                    response.EnsureSuccessStatusCode();

                    using (Stream contentStream = await response.Content.ReadAsStreamAsync(), fileStream = new FileStream(DestinationFile, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                    {
                        var totalRead = 0L;
                        var totalReads = 0L;
                        var buffer = new byte[8192];
                        var isMoreToRead = true;

                        do
                        {
                            var read = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                            if (read == 0)
                            {
                                isMoreToRead = false;
                            }
                            else
                            {
                                await fileStream.WriteAsync(buffer, 0, read);

                                totalRead += read;
                                totalReads += 1;

                                if (totalReads % 2000 == 0)
                                {
                                    Console.WriteLine(string.Format("total bytes downloaded so far: {0:n0}", totalRead));
                                }
                            }
                        }
                        while (isMoreToRead);
                    }
                }

                // increment rate limiter avoidance call count
                RateLimitAvoidanceCallCount += 1;

                return true;
            }
            catch (HttpRequestException ex)
            {
                switch (ex.StatusCode)
                {
                    case HttpStatusCode.TooManyRequests:
                        if (RetryAttempts >= RetryAttemptsMax)
                        {
                            Logging.LogKey(Logging.LogType.Warning, "process.communications", "communications.hasheous_rate_limiter_attempts_expired_aborting", null, null, ex);
                            throw;
                        }
                        else
                        {
                            Logging.LogKey(Logging.LogType.Information, "process.communications", "communications.hasheous_api_rate_limit_hit_while_downloading", null, new string[] { uri.ToString() }, ex);

                            RetryAttempts += 1;

                            // attempt to download again
                            var results2 = await _DownloadFile(uri, DestinationFile);

                            return results2;
                        }

                    case HttpStatusCode.NotFound:
                        if (File.Exists(DestinationFile))
                        {
                            FileInfo fi = new FileInfo(DestinationFile);
                            if (fi.Length == 0)
                            {
                                File.Delete(DestinationFile);
                            }
                        }
                        break;

                    default:
                        Logging.LogKey(Logging.LogType.Warning, "process.communications", "communications.error_downloading_file_from_uri", null, new string[] { uri.ToString() }, ex);
                        throw;
                }
            }
            catch (Exception ex)
            {
                Logging.LogKey(Logging.LogType.Warning, "process.communications", "communications.error_downloading_file_from_uri", null, new string[] { uri.ToString() }, ex);
                throw;
            }

            return false;
        }
        #endregion Download File

        public async Task<string> GetSpecificImageFromServer(FileSignature.MetadataSources SourceType, string ImagePath, string ImageId, IGDBAPI_ImageSize size, List<IGDBAPI_ImageSize>? FallbackSizes = null)
        {
            string originalPath = Path.Combine(ImagePath, SourceType.ToString(), IGDBAPI_ImageSize.original.ToString());
            string originalFilePath = Path.Combine(originalPath, ImageId);
            string requestedPath = Path.Combine(ImagePath, SourceType.ToString(), size.ToString());
            string requestedFilePath = Path.Combine(requestedPath, ImageId);

            switch (SourceType)
            {
                case FileSignature.MetadataSources.TheGamesDb:
                    originalPath = Path.GetDirectoryName(originalFilePath);
                    requestedPath = Path.GetDirectoryName(requestedFilePath);
                    break;
            }

            // create the directory if it doesn't exist
            if (!Directory.Exists(originalPath))
            {
                Directory.CreateDirectory(originalPath);
            }
            if (!Directory.Exists(requestedPath))
            {
                Directory.CreateDirectory(requestedPath);
            }

            // get the resolution attribute for enum size
            Point resolution = Common.GetResolution(size);

            // check if the original image exists
            if (!File.Exists(originalFilePath))
            {
                // sleep if the rate limiter is active
                if (RateLimitResumeTime > DateTime.UtcNow)
                {
                    Logging.LogKey(Logging.LogType.Information, "process.api_connection", "apiconnection.metadata_source_rate_limit_hit_pausing_until", null, new string[] { RateLimitResumeTime.ToString(), RetryAttempts.ToString(), RetryAttemptsMax.ToString() });
                    Thread.Sleep(RateLimitRecoveryWaitTime);
                }

                if (InRateLimitAvoidanceMode == true)
                {
                    // sleep for a moment to help avoid hitting the rate limiter
                    Logging.LogKey(Logging.LogType.Information, "process.api_connection", "apiconnection.metadata_source_rate_limit_hit_pausing_milliseconds", null, new string[] { RateLimitAvoidanceWait.ToString() });
                    Thread.Sleep(RateLimitAvoidanceWait);
                }

                // get the original image
                Communications comms = new Communications();
                switch (SourceType)
                {
                    case FileSignature.MetadataSources.None:
                        await comms.API_GetURL(ImageId, originalPath);

                        return originalFilePath;

                    case FileSignature.MetadataSources.IGDB:
                        originalFilePath = originalFilePath + ".jpg";
                        requestedFilePath = requestedFilePath + ".jpg";
                        if (Config.IGDB.UseHasheousProxy == false)
                        {
                            await comms.IGDBAPI_GetImage(ImageId, originalPath);
                        }
                        else
                        {
                            await comms.HasheousProxyAPI_GetImage(SourceType, ImageId, originalPath);
                        }
                        break;

                    case FileSignature.MetadataSources.TheGamesDb:
                        await comms.HasheousProxyAPI_GetImage(SourceType, ImageId, originalPath);
                        break;

                    default:
                        break;
                }
            }

            // check if the requested image exists
            if (!File.Exists(requestedFilePath))
            {
                // get the original image
                using (var image = new ImageMagick.MagickImage(originalFilePath))
                {
                    image.Resize((uint)resolution.X, (uint)resolution.Y);
                    image.Strip();
                    await image.WriteAsync(requestedFilePath);
                }
            }

            return requestedFilePath;
        }

        public static T? GetSearchCache<T>(string SearchFields, string SearchString)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "SELECT * FROM SearchCache WHERE SearchFields = @searchfields AND SearchString = @searchstring;";
            Dictionary<string, object> dbDict = new Dictionary<string, object>
            {
                { "searchfields", SearchFields },
                { "searchstring", SearchString }
            };
            DataTable data = db.ExecuteCMD(sql, dbDict);
            if (data.Rows.Count > 0)
            {
                // cache hit
                string rawString = data.Rows[0]["Content"].ToString();
                T ReturnValue = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(rawString);
                if (ReturnValue != null)
                {
                    Logging.LogKey(Logging.LogType.Information, "process.search_cache", "searchcache.found_search_result_in_cache", null, new string[] { SearchString });
                    return ReturnValue;
                }
                else
                {
                    Logging.LogKey(Logging.LogType.Information, "process.search_cache", "searchcache.search_result_not_found_in_cache");
                    return default;
                }
            }
            else
            {
                // cache miss
                Logging.LogKey(Logging.LogType.Information, "process.search_cache", "searchcache.search_result_not_found_in_cache");
                return default;
            }
        }

        public static void SetSearchCache<T>(string SearchFields, string SearchString, T SearchResult)
        {
            Logging.LogKey(Logging.LogType.Information, "process.search_cache", "searchcache.storing_search_results_in_cache", null, new string[] { SearchString });

            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "INSERT INTO SearchCache (SearchFields, SearchString, Content, LastSearch) VALUES (@searchfields, @searchstring, @content, @lastsearch);";
            Dictionary<string, object> dbDict = new Dictionary<string, object>
            {
                { "searchfields", SearchFields },
                { "searchstring", SearchString },
                { "content", Newtonsoft.Json.JsonConvert.SerializeObject(SearchResult) },
                { "lastsearch", DateTime.UtcNow }
            };
            db.ExecuteNonQuery(sql, dbDict);
        }

        static List<HasheousClient.Models.DataObjectItem> hasheousPlatforms = new List<HasheousClient.Models.DataObjectItem>();
        public static async Task PopulateHasheousPlatformData(long Id)
        {
            // get the platform object from the cache
            Platform? platform = await Platforms.GetPlatform(Id);
            if (platform == null)
            {
                Logging.LogKey(Logging.LogType.Warning, "process.populate_hasheous_platform_data", "populatehasheousplatformdata.platform_with_id_not_found_in_cache", null, new string[] { Id.ToString() });
                return;
            }

            // fetch all platforms
            if (hasheousPlatforms.Count == 0)
            {
                hasheousPlatforms = hasheousClient.GetPlatforms();
            }

            // loop through the platforms and check if the metadata matches the IGDB platform id
            if (hasheousPlatforms == null || hasheousPlatforms.Count == 0)
            {
                Logging.LogKey(Logging.LogType.Warning, "process.populate_hasheous_platform_data", "populatehasheousplatformdata.no_platforms_found_in_hasheous");
                return;
            }

            // search through hasheousPlatforms for a match where the metadata source is IGDB and the immutable id matches the platform id, or the metadata source is IGDB and the id matches the platform slug
            HasheousClient.Models.DataObjectItem? hasheousPlatform = hasheousPlatforms.FirstOrDefault(p =>
                p.Metadata != null &&
                p.Metadata.Any(m => m.Source == FileSignature.MetadataSources.IGDB.ToString() && (
                    (m.ImmutableId != null && m.ImmutableId.Length > 0 && long.TryParse(m.ImmutableId, out long objId) && objId == Id) ||
                    (m.Id != null && m.Id.Equals(platform.Slug, StringComparison.OrdinalIgnoreCase))
                ))
            );

            if (hasheousPlatform == null)
            {
                Logging.LogKey(Logging.LogType.Warning, "process.populate_hasheous_platform_data", "populatehasheousplatformdata.no_matching_platform_found_for_id", null, new string[] { Id.ToString() });
                return;
            }

            // check the attributes for a logo
            HasheousClient.Models.Metadata.IGDB.PlatformLogo? platformLogo = null;

            if (hasheousPlatform.Attributes != null && hasheousPlatform.Attributes.Count > 0)
            {
                foreach (var hasheousPlatformAttribute in hasheousPlatform.Attributes)
                {
                    if (
                        hasheousPlatformAttribute.attributeType == HasheousClient.Models.AttributeItem.AttributeType.ImageId &&
                        hasheousPlatformAttribute.attributeName == HasheousClient.Models.AttributeItem.AttributeName.Logo &&
                        hasheousPlatformAttribute.Value != null
                        )
                    {
                        Uri logoUrl = new Uri(
                            new Uri(HasheousClient.WebApp.HttpHelper.BaseUri, UriKind.Absolute),
                            new Uri("/api/v1/images/" + hasheousPlatformAttribute.Value, UriKind.Relative));

                        // generate a platform logo object
                        platformLogo = new HasheousClient.Models.Metadata.IGDB.PlatformLogo
                        {
                            AlphaChannel = false,
                            Animated = false,
                            ImageId = (string)hasheousPlatformAttribute.Value,
                            Url = logoUrl.ToString()
                        };

                        // generate a long id from the value
                        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(platformLogo.ImageId);
                        long longId = BitConverter.ToInt64(bytes, 0);
                        platformLogo.Id = longId;

                        // store the platform logo object
                        Storage.CacheStatus cacheStatus = await Storage.GetCacheStatusAsync(FileSignature.MetadataSources.None, "PlatformLogo", longId);
                        switch (cacheStatus)
                        {
                            case Storage.CacheStatus.NotPresent:
                                await Storage.NewCacheValue<PlatformLogo>(FileSignature.MetadataSources.None, platformLogo, false);
                                break;
                        }

                        break;
                    }
                }
            }

            // update the platform object with the name and logo id
            if (platform != null)
            {
                platform.Name = hasheousPlatform.Name;
                if (platformLogo != null && platformLogo.Id.HasValue)
                {
                    platform.PlatformLogo = platformLogo.Id.Value;
                }
                await Storage.NewCacheValue<Platform>(FileSignature.MetadataSources.None, platform, true);
            }
            Logging.LogKey(Logging.LogType.Information, "process.populate_hasheous_platform_data", "populatehasheousplatformdata.platform_data_populated_for_id", null, new string[] { Id.ToString() });
        }

        /// <summary>
        /// See https://api-docs.igdb.com/?javascript#images for more information about the image url structure
        /// </summary>
        /// <param name="ImageId"></param>
        /// <param name="outputPath">The path to save the downloaded files to
        public async Task IGDBAPI_GetImage(string ImageId, string OutputPath)
        {
            string urlTemplate = "https://images.igdb.com/igdb/image/upload/t_{size}/{hash}.jpg";

            string url = urlTemplate.Replace("{size}", "original").Replace("{hash}", ImageId);
            string OutputFile = ImageId + ".jpg";
            string fullPath = Path.Combine(OutputPath, OutputFile);

            await _DownloadFile(new Uri(url), fullPath);
        }

        /// <summary>
        /// Get an image from the Hasheous proxy API
        /// </summary>
        /// <param name="SourceType">
        /// The source of the metadata
        /// </param>
        /// <param name="ImageId">
        /// The image id to fetch
        /// </param>
        /// <param name="OutputPath">
        /// The path to save the downloaded files to
        /// </param>
        /// <returns>
        /// The path to the downloaded file
        /// </returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task HasheousProxyAPI_GetImage(FileSignature.MetadataSources SourceType, string ImageId, string OutputPath)
        {
            string urlTemplate;

            string url;
            string OutputFile;
            string fullPath;

            switch (SourceType)
            {
                case FileSignature.MetadataSources.IGDB:
                    urlTemplate = HasheousClient.WebApp.HttpHelper.BaseUri + "api/v1/MetadataProxy/IGDB/Image/{hash}.jpg";
                    url = urlTemplate.Replace("{hash}", ImageId);
                    OutputFile = ImageId + ".jpg";
                    fullPath = Path.Combine(OutputPath, OutputFile);
                    break;

                case FileSignature.MetadataSources.TheGamesDb:
                    urlTemplate = HasheousClient.WebApp.HttpHelper.BaseUri + "api/v1/MetadataProxy/TheGamesDB/Images/original/{FileName}";
                    url = urlTemplate.Replace("{FileName}", ImageId);
                    OutputFile = Path.GetFileName(ImageId);
                    fullPath = Path.Combine(OutputPath, OutputFile);
                    break;

                default:
                    throw new NotImplementedException();
            }

            await _DownloadFile(new Uri(url), fullPath);
        }

        public async Task API_GetURL(string FileName, string OutputPath)
        {
            string urlTemplate = HasheousClient.WebApp.HttpHelper.BaseUri + "api/v1/images/{imageid}";

            string url = urlTemplate.Replace("{imageid}", FileName);
            string OutputFile = FileName;
            string fullPath = Path.Combine(OutputPath, OutputFile);

            await _DownloadFile(new Uri(url), fullPath);
        }

        public enum IGDBAPI_ImageSize
        {
            /// <summary>
            /// 90x128 Fit
            /// </summary>
            [Description("cover_small")]
            [Resolution(90, 128)]
            cover_small,

            /// <summary>
            /// 264x374 Fit
            /// </summary>
            [Description("cover_big")]
            [Resolution(264, 374)]
            cover_big,

            /// <summary>
            /// 165x90 Lfill, Centre gravity - resized by Gaseous and is not a real IGDB size
            /// </summary>
            [Description("screenshot_thumb")]
            [Resolution(165, 90)]
            screenshot_thumb,

            /// <summary>
            /// 235x128 Lfill, Centre gravity - resized by Gaseous and is not a real IGDB size
            /// </summary>
            [Description("screenshot_small")]
            [Resolution(235, 128)]
            screenshot_small,

            /// <summary>
            /// 589x320 Lfill, Centre gravity
            /// </summary>
            [Description("screenshot_med")]
            [Resolution(589, 320)]
            screenshot_med,

            /// <summary>
            /// 889x500 Lfill, Centre gravity
            /// </summary>
            [Description("screenshot_big")]
            [Resolution(889, 500)]
            screenshot_big,

            /// <summary>
            /// 1280x720 Lfill, Centre gravity
            /// </summary>
            [Description("screenshot_huge")]
            [Resolution(1280, 720)]
            screenshot_huge,

            /// <summary>
            /// 284x160 Fit
            /// </summary>
            [Description("logo_med")]
            [Resolution(284, 160)]
            logo_med,

            /// <summary>
            /// 90x90 Thumb, Centre gravity
            /// </summary>
            [Description("thumb")]
            [Resolution(90, 90)]
            thumb,

            /// <summary>
            /// 35x35 Thumb, Centre gravity
            /// </summary>
            [Description("micro")]
            [Resolution(35, 35)]
            micro,

            /// <summary>
            /// 1280x720 Fit, Centre gravity
            /// </summary>
            [Description("720p")]
            [Resolution(1280, 720)]
            r720p,

            /// <summary>
            /// 1920x1080 Fit, Centre gravity
            /// </summary>
            [Description("1080p")]
            [Resolution(1920, 1080)]
            r1080p,

            /// <summary>
            /// The originally uploaded image
            /// </summary>
            [Description("original")]
            [Resolution(0, 0)]
            original
        }


        /// <summary>
        /// Specifies a resolution for an image size enum
        /// </summary>
        [AttributeUsage(AttributeTargets.All)]
        public class ResolutionAttribute : Attribute
        {
            public static readonly ResolutionAttribute Default = new ResolutionAttribute();

            public ResolutionAttribute() : this(0, 0)
            {
            }

            public ResolutionAttribute(int width, int height)
            {
                ResolutionWidth = width;
                ResolutionHeight = height;
            }

            public virtual int width => ResolutionWidth;
            public virtual int height => ResolutionHeight;

            protected int ResolutionWidth { get; set; }
            protected int ResolutionHeight { get; set; }
        }
    }
}