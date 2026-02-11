using System.Data;
using System.Data.SqlTypes;
using System.Threading.Tasks;
using gaseous_server.Classes.Plugins.MetadataProviders;
using gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static gaseous_server.Classes.Metadata.ImageHandling;

namespace gaseous_server.Classes.Metadata
{
    public class Metadata
    {
        #region Exception Handling
        public class InvalidMetadataId : Exception
        {
            public InvalidMetadataId(long Id) : base("Invalid Metadata id: " + Id + " from source: " + FileSignature.MetadataSources.IGDB + " (default)")
            {
            }

            public InvalidMetadataId(FileSignature.MetadataSources SourceType, long Id) : base("Invalid Metadata id: " + Id + " from source: " + SourceType)
            {
            }

            public InvalidMetadataId(string Id) : base("Invalid Metadata id: " + Id + " from source: " + FileSignature.MetadataSources.IGDB + " (default)")
            {
            }

            public InvalidMetadataId(FileSignature.MetadataSources SourceType, string Id) : base("Invalid Metadata id: " + Id + " from source: " + SourceType)
            {
            }
        }

        public class NoMetadataProvidersConfigured : Exception
        {
            public NoMetadataProvidersConfigured() : base("No metadata providers configured")
            {
            }
        }
        #endregion

        #region Metadata Sources
        /// <summary>
        /// A list of metadata providers that can be used to retrieve metadata from various sources. Each provider in the list implements the IMetadataProvider interface and can be configured with specific settings, such as API credentials or proxy settings, to facilitate the retrieval of metadata for games, platforms, and other related information.
        /// When GetMetadata is called without a specified source, the system will use the default metadata provider, which is the first provider in this list. This allows for flexibility in choosing different metadata sources based on user preferences or specific requirements for certain types of metadata.
        /// </summary>
        public static List<Plugins.MetadataProviders.IMetadataProvider> MetadataProviders { get; set; } = new List<Plugins.MetadataProviders.IMetadataProvider>
        {
            new Plugins.MetadataProviders.IGDBProvider.Provider
            {
                Settings = new Dictionary<string, object>
                {
                    { "ClientID", Config.IGDB.ClientId },
                    { "ClientSecret", Config.IGDB.Secret }
                },
                ProxyProvider = new Plugins.MetadataProviders.HasheousIGDBProxyProvider()
            },
            new Plugins.MetadataProviders.TheGamesDBProvider.Provider(),
            new Plugins.MetadataProviders.NoneProvider.Provider()
        };

        #endregion Metadata Sources

        #region Get Metadata
        /// <summary>
        /// Get metadata from the default source (the first provider in the MetadataProviders list)
        /// </summary>
        /// <typeparam name="T">
        /// The type of metadata to get
        /// </typeparam>
        /// <param name="Id">
        /// The id of the metadata to get
        /// </param>
        /// <param name="ForceRefresh">
        /// Whether to force refresh the metadata from the server, bypassing any local cache. Default is false, which allows the system to return cached metadata if it is available and not expired.
        /// </param>
        /// <returns>
        /// The metadata object
        /// </returns>
        /// <exception cref="InvalidMetadataId">
        /// Thrown when the id is invalid
        /// </exception>
        public static T? GetMetadata<T>(long Id, Boolean ForceRefresh = false) where T : class
        {
            if (MetadataProviders.Count == 0)
            {
                throw new NoMetadataProvidersConfigured();
            }

            if (Id < 0)
            {
                throw new InvalidMetadataId(MetadataProviders[0].SourceType, Id);
            }

            return _GetMetadataAsync<T>(MetadataProviders[0].SourceType, Id, ForceRefresh).Result;
        }

        /// <summary>
        /// Get metadata from the specified source
        /// </summary>
        /// <typeparam name="T">
        /// The type of metadata to get
        /// </typeparam>
        /// <param name="SourceType">
        /// The source of the metadata
        /// </param>
        /// <param name="Id">
        /// The id of the metadata to get
        /// </param>
        /// <param name="ForceRefresh">
        /// Whether to force refresh the metadata from the server, bypassing any local cache. Default is false, which allows the system to return cached metadata if it is available and not expired.
        /// </param>
        /// <returns>
        /// The metadata object
        /// </returns>
        /// <exception cref="InvalidMetadataId">
        /// Thrown when the id is invalid
        /// </exception>
        public static T? GetMetadata<T>(FileSignature.MetadataSources SourceType, long Id, Boolean ForceRefresh = false) where T : class
        {
            if (Id < 0)
            {
                throw new InvalidMetadataId(SourceType, Id);
            }

            return _GetMetadataAsync<T>(SourceType, Id, ForceRefresh).Result;
        }

        /// <summary>
        /// Get metadata from the default source (the first provider in the MetadataProviders list)
        /// </summary>
        /// <typeparam name="T">
        /// The type of metadata to get
        /// </typeparam>
        /// <param name="Id">
        /// The id of the metadata to get
        /// </param>
        /// <param name="ForceRefresh">
        /// Whether to force refresh the metadata from the server, bypassing any local cache. Default is false, which allows the system to return cached metadata if it is available and not expired.
        /// </param>
        /// <returns>
        /// The metadata object
        /// </returns>
        /// <exception cref="InvalidMetadataId">
        /// Thrown when the id is invalid
        /// </exception>
        public static async Task<T?> GetMetadataAsync<T>(long Id, Boolean ForceRefresh = false) where T : class
        {
            if (MetadataProviders.Count == 0)
            {
                throw new NoMetadataProvidersConfigured();
            }

            if (Id < 0)
            {
                throw new InvalidMetadataId(MetadataProviders[0].SourceType, Id);
            }

            return await _GetMetadataAsync<T>(MetadataProviders[0].SourceType, Id, ForceRefresh);
        }

        /// <summary>
        /// Get metadata from the specified source
        /// </summary>
        /// <typeparam name="T">
        /// The type of metadata to get
        /// </typeparam>
        /// <param name="SourceType">
        /// The source of the metadata
        /// </param>
        /// <param name="Id">
        /// The id of the metadata to get
        /// </param>
        /// <param name="ForceRefresh">
        /// Whether to force refresh the metadata from the server, bypassing any local cache. Default is false, which allows the system to return cached metadata if it is available and not expired.
        /// </param>
        /// <returns>
        /// The metadata object
        /// </returns>
        /// <exception cref="InvalidMetadataId">
        /// Thrown when the id is invalid
        /// </exception>
        public static async Task<T?> GetMetadataAsync<T>(FileSignature.MetadataSources SourceType, long Id, Boolean ForceRefresh = false) where T : class
        {
            if (Id < 0)
            {
                throw new InvalidMetadataId(SourceType, Id);
            }

            return await _GetMetadataAsync<T>(SourceType, Id, ForceRefresh);
        }

        private static async Task<T?> _GetMetadataAsync<T>(FileSignature.MetadataSources SourceType, long Id, Boolean ForceRefresh) where T : class
        {
            var provider = MetadataProviders.FirstOrDefault(x => x.SourceType == SourceType);
            if (provider == null)
            {
                Console.WriteLine("No metadata provider found for source type: " + SourceType);
                throw new NoMetadataProvidersConfigured();
            }

            // execute the metadata retrieval command based on T
            return typeof(T) switch
            {
                Type t when t == typeof(AgeRating) => await provider.GetAgeRatingAsync(Id, ForceRefresh) as T,
                Type t when t == typeof(AgeRatingCategory) => await provider.GetAgeRatingCategoryAsync(Id, ForceRefresh) as T,
                Type t when t == typeof(AgeRatingContentDescription) => await provider.GetAgeRatingContentDescriptionAsync(Id, ForceRefresh) as T,
                Type t when t == typeof(AgeRatingOrganization) => await provider.GetAgeRatingOrganizationAsync(Id, ForceRefresh) as T,
                Type t when t == typeof(AlternativeName) => await provider.GetAlternativeNameAsync(Id, ForceRefresh) as T,
                Type t when t == typeof(Artwork) => await provider.GetArtworkAsync(Id, ForceRefresh) as T,
                Type t when t == typeof(ClearLogo) => await provider.GetClearLogoAsync(Id, ForceRefresh) as T,
                Type t when t == typeof(Collection) => await provider.GetCollectionAsync(Id, ForceRefresh) as T,
                Type t when t == typeof(Company) => await provider.GetCompanyAsync(Id, ForceRefresh) as T,
                Type t when t == typeof(CompanyLogo) => await provider.GetCompanyLogoAsync(Id, ForceRefresh) as T,
                Type t when t == typeof(Cover) => await provider.GetCoverAsync(Id, ForceRefresh) as T,
                Type t when t == typeof(ExternalGame) => await provider.GetExternalGameAsync(Id, ForceRefresh) as T,
                Type t when t == typeof(Franchise) => await provider.GetFranchiseAsync(Id, ForceRefresh) as T,
                Type t when t == typeof(GameLocalization) => await provider.GetGameLocalizationAsync(Id, ForceRefresh) as T,
                Type t when t == typeof(GameMode) => await provider.GetGameModeAsync(Id, ForceRefresh) as T,
                Type t when t == typeof(Game) => await _GetGameAsync(Id, ForceRefresh) as T,
                Type t when t == typeof(GameVideo) => await provider.GetGameVideoAsync(Id, ForceRefresh) as T,
                Type t when t == typeof(Genre) => await provider.GetGenreAsync(Id, ForceRefresh) as T,
                Type t when t == typeof(InvolvedCompany) => await provider.GetInvolvedCompanyAsync(Id, ForceRefresh) as T,
                Type t when t == typeof(MultiplayerMode) => await provider.GetMultiplayerModeAsync(Id, ForceRefresh) as T,
                Type t when t == typeof(PlatformLogo) => await provider.GetPlatformLogoAsync(Id, ForceRefresh) as T,
                Type t when t == typeof(Platform) => await provider.GetPlatformAsync(Id, ForceRefresh) as T,
                Type t when t == typeof(PlatformVersion) => await provider.GetPlatformVersionAsync(Id, ForceRefresh) as T,
                Type t when t == typeof(PlayerPerspective) => await provider.GetPlayerPerspectiveAsync(Id, ForceRefresh) as T,
                Type t when t == typeof(Region) => await provider.GetRegionAsync(Id, ForceRefresh) as T,
                Type t when t == typeof(ReleaseDate) => await provider.GetReleaseDateAsync(Id, ForceRefresh) as T,
                Type t when t == typeof(Screenshot) => await provider.GetScreenshotAsync(Id, ForceRefresh) as T,
                Type t when t == typeof(Theme) => await provider.GetThemeAsync(Id, ForceRefresh) as T,
                _ => throw new NotSupportedException("Unsupported metadata type: " + typeof(T).FullName)
            };
        }

        private static async Task<Game?> _GetGameAsync(long Id, bool ForceRefresh)
        {
            var provider = MetadataProviders.FirstOrDefault(x => x.SourceType == FileSignature.MetadataSources.IGDB);
            if (provider == null)
            {
                throw new NoMetadataProvidersConfigured();
            }

            Game? game = await provider.GetGameAsync(Id, ForceRefresh);
            if (game == null)
            {
                return null;
            }

            // get all clear logos for the game and add them to the game object
            game.ClearLogos = new Dictionary<FileSignature.MetadataSources, List<long>>();
            List<FileSignature.MetadataSources> sourcesToCheck = new List<FileSignature.MetadataSources>
            {
                FileSignature.MetadataSources.TheGamesDb
            };
            foreach (var metadataProvider in MetadataProviders)
            {
                // get the game for each metadata provider and check if it has clear logos, if so add them to the game object
                if (sourcesToCheck.Contains(metadataProvider.SourceType))
                {
                    List<long> clearLogoIds = new List<long>();
                    Game? providerGame = await metadataProvider.GetGameAsync(Id, ForceRefresh);
                    if (providerGame != null && providerGame.ClearLogo != null && providerGame.ClearLogo.Count > 0)
                    {
                        clearLogoIds.AddRange(providerGame.ClearLogo);
                    }

                    if (clearLogoIds != null && clearLogoIds.Count > 0)
                    {
                        game.ClearLogos[metadataProvider.SourceType] = clearLogoIds;
                    }
                }
            }

            return game;
        }

        /// <summary>
        /// Search for games based on the provided search type, platform ID, and search candidates. This method utilizes the configured metadata providers to perform the search and retrieve relevant game information. The search type determines the criteria used for searching, such as exact match or partial match, while the platform ID specifies the gaming platform to filter the results. The search candidates are a list of potential game names or identifiers that will be used in the search process. If no metadata providers are configured, an exception will be thrown.
        /// </summary>
        /// <param name="searchType">
        /// The type of search to perform (e.g., exact match, partial match).
        /// </param>
        /// <param name="platformId">
        /// The ID of the platform to filter the search results.
        /// </param>
        /// <param name="searchCandidates">
        /// A list of potential game names or identifiers to search for.
        /// </param>
        /// <returns>
        /// An array of games that match the search criteria, or null if no games are found.
        /// </returns>
        /// <exception cref="NoMetadataProvidersConfigured"></exception>
        public static async Task<Game[]?> SearchGamesAsync(gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes.SearchType searchType, long platformId, List<string> searchCandidates)
        {
            if (MetadataProviders.Count == 0)
            {
                throw new NoMetadataProvidersConfigured();
            }

            var provider = MetadataProviders.FirstOrDefault(x => x.SourceType == FileSignature.MetadataSources.IGDB);
            if (provider == null)
            {
                throw new NoMetadataProvidersConfigured();
            }

            return await provider.SearchGamesAsync(searchType, platformId, searchCandidates);
        }

        /// <summary>
        /// Retrieves an image from the specified metadata provider based on the given image type, ID, and desired size.
        /// </summary>
        /// <param name="SourceType">
        /// The metadata source from which to retrieve the image.
        /// </param>
        /// <param name="GameId">
        /// The ID of the game for which to retrieve the image.
        /// </param>
        /// <param name="imageType">
        /// The type of image to retrieve (e.g., artwork, cover, screenshot).
        /// </param>
        /// <param name="Url">
        /// The URL of the image to retrieve.
        /// </param>
        /// <param name="size">
        /// The desired size of the image to retrieve.
        /// </param>
        /// <returns>
        /// A byte array containing the image data, or null if the image cannot be retrieved.
        /// </returns>
        /// <exception cref="NoMetadataProvidersConfigured">
        /// Thrown when no metadata provider is configured for the specified source type.
        /// </exception>
        public static async Task<byte[]?> GetImageAsync(FileSignature.MetadataSources SourceType, long GameId, ImageType imageType, string Url, Plugins.PluginManagement.ImageResize.ImageSize size)
        {
            var provider = MetadataProviders.FirstOrDefault(x => x.SourceType == SourceType);
            if (provider == null)
            {
                throw new NoMetadataProvidersConfigured();
            }

            byte[]? result = await provider.GetGameImageAsync(GameId, Url, imageType);
            if (result == null)
            {
                return null;
            }

            if (size == Plugins.PluginManagement.ImageResize.ImageSize.original)
            {
                return result;
            }

            // check the image type, if it's SVG, return the original image as SVG files cannot be resized
            var info = new ImageMagick.MagickImageInfo(result);
            if (info.Format == ImageMagick.MagickFormat.Svg)
            {
                return result;
            }

            // use Magick.Net to resize the image to the desired size
            using (var image = new ImageMagick.MagickImage(result))
            {
                // get the resolution attribute for the ImageSize enum value
                var resolutionAttribute = Common.GetResolution(size);
                if (resolutionAttribute == null)
                {
                    return result;
                }

                // if the resolution attribute is the default (0, 0), return the original image
                if (resolutionAttribute.X == 0 && resolutionAttribute.Y == 0)
                {
                    return result;
                }

                // otherwise, resize the image to the desired resolution
                image.Resize((uint)resolutionAttribute.X, (uint)resolutionAttribute.Y);
                return image.ToByteArray();
            }
        }
        #endregion
    }
}