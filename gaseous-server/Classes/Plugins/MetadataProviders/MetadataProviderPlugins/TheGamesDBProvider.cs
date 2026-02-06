using gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes;
using HasheousClient.Models;
using SharpCompress.Archives;
using SharpCompress.Common;
using System.Reflection;

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

        /// <summary>
        /// The Hasheous client instance - only use via the hasheous property.
        /// </summary>
        private HasheousClient.Hasheous? _hasheous;
        /// <summary>
        /// Gets the Hasheous client instance, initializing it if necessary.
        /// </summary>
        private HasheousClient.Hasheous hasheous
        {
            get
            {
                if (_hasheous == null)
                {
                    _hasheous = new HasheousClient.Hasheous();

                    // Configure the Hasheous client
                    HasheousClient.WebApp.HttpHelper.BaseUri = Config.MetadataConfiguration.HasheousHost;

                    // Set the API key for Hasheous Proxy
                    if (HasheousClient.WebApp.HttpHelper.ClientKey == null || HasheousClient.WebApp.HttpHelper.ClientKey != Config.MetadataConfiguration.HasheousClientAPIKey)
                    {
                        HasheousClient.WebApp.HttpHelper.ClientKey = Config.MetadataConfiguration.HasheousClientAPIKey;
                    }
                }
                return _hasheous;
            }
        }

        /// <summary>
        /// HTTP communications handler for making API requests.
        /// </summary>
        private readonly HTTPComms comms = new HTTPComms();

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
        public async Task<byte[]?> GetGameImageAsync(long gameId, string url, ImageType imageType, ImageSize imageSize)
        {
            return null;
        }

        private async Task<T?> GetGameBundleAsync<T>(long id) where T : class
        {
            // we can only handle bundles for games - check T type
            // abort if T is not Game
            if (typeof(T) != typeof(Game))
            {
                return null;
            }

            // check if bundle exists
            string bundlePath = Config.LibraryConfiguration.LibraryMetadataDirectory_GameBundles(SourceType, id);
            bool forceDownloadBundle = false;
            if (Directory.Exists(bundlePath))
            {
                // open the game.json file
                string gameJsonPath = Path.Combine(bundlePath, "game.json");
                if (!File.Exists(gameJsonPath))
                {
                    forceDownloadBundle = true;
                }
                else
                {
                    // check last modified time of the bundle - if it's older than 30 days, we should refresh it
                    DateTime lastModified = File.GetLastWriteTime(gameJsonPath);
                    if ((DateTime.UtcNow - lastModified).TotalDays > 30)
                    {
                        forceDownloadBundle = true;
                    }
                }
            }
            else
            {
                forceDownloadBundle = true;
            }

            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "X-Client-API-Key", Config.MetadataConfiguration.HasheousClientAPIKey }
            };

            if (forceDownloadBundle)
            {
                // download the bundle
                Uri bundleUrl = new Uri($"{Config.MetadataConfiguration.HasheousHost}api/v1/MetadataProxy/Bundles/TheGamesDB/{id}.bundle");
                string downloadDirectory = Path.Combine(Config.LibraryConfiguration.LibraryTempDirectory, Path.GetRandomFileName());
                string downloadedBundlePath = Path.Combine(downloadDirectory, $"{id}.bundle.zip");
                if (Directory.Exists(downloadDirectory))
                {
                    Directory.Delete(downloadDirectory, true);
                }
                Directory.CreateDirectory(downloadDirectory);
                if (!Directory.Exists(bundlePath))
                {
                    Directory.CreateDirectory(bundlePath);
                }
                var response = await comms.DownloadToFileAsync(bundleUrl, downloadedBundlePath, headers);
                if (response.StatusCode == 200)
                {
                    // extract the bundle
                    using (var zip = SharpCompress.Archives.Zip.ZipArchive.Open(downloadedBundlePath))
                    {
                        foreach (var entry in zip.Entries.Where(entry => !entry.IsDirectory))
                        {
                            await entry.WriteToDirectoryAsync(bundlePath, new ExtractionOptions()
                            {
                                ExtractFullPath = true,
                                Overwrite = true
                            });
                        }
                    }
                    // delete the temp file
                    try
                    {
                        File.Delete(downloadedBundlePath);
                    }
                    catch
                    {
                        // ignore - should get cleaned up later by the temp file cleaner anyway
                    }
                }
                else
                {
                    // failed to download bundle
                    return null;
                }
            }

            // get TheGamesDB genres if needed - these are required to properly map the genre IDs in the game bundle to genre names, which are what we want to store in our database
            string genresJsonFilePath = Path.Combine(Config.LibraryConfiguration.LibraryMetadataDirectory_TheGamesDB(), "genres.json");
            if (!Directory.Exists(Config.LibraryConfiguration.LibraryMetadataDirectory_TheGamesDB()))
            {
                Directory.CreateDirectory(Config.LibraryConfiguration.LibraryMetadataDirectory_TheGamesDB());
            }
            if (!File.Exists(genresJsonFilePath) || (DateTime.UtcNow - File.GetLastWriteTime(genresJsonFilePath)).TotalDays > 30)
            {
                Uri genresUrl = new Uri($"{Config.MetadataConfiguration.HasheousHost}api/v1/MetadataProxy/TheGamesDB/Genres");
                var genresResponse = await comms.SendRequestAsync<Dictionary<string, object>>(HTTPComms.HttpMethod.GET, genresUrl, headers);
                if (genresResponse.StatusCode == 200 && genresResponse.Body != null)
                {
                    string genresJson = Newtonsoft.Json.JsonConvert.SerializeObject(genresResponse.Body);
                    await File.WriteAllTextAsync(genresJsonFilePath, genresJson);

                    // extract the dictionary of genre's from "data/genres" in the response and store in the database for later use when mapping genre IDs to names in the game bundle
                    // each item in the dictionary has the key of the genre ID and the value is a dictionary containing the genre data where the name is stored under the "name" key
                    if (genresResponse.Body.ContainsKey("data") && genresResponse.Body["data"] is Dictionary<string, object> genreDict && genreDict.ContainsKey("genres") && genreDict["genres"] is Dictionary<string, object> genresDict)
                    {
                        foreach (var genreItem in genresDict)
                        {
                            if (genreItem.Value is Dictionary<string, object> genreData && genreData.ContainsKey("name"))
                            {
                                long genreId = Convert.ToInt64(genreItem.Key);
                                string genreName = genreData["name"].ToString() ?? "";

                                // store the genre in the database
                                var genreItemToStore = new Genre
                                {
                                    Id = genreId,
                                    Name = genreName,
                                    SourceType = SourceType
                                };
                                await Storage.StoreCacheValue<Genre>(genreItemToStore);
                            }
                        }
                    }
                }
            }

            // bundle should now be present for loading - extract the requested entity from the Game.json file
            // open the game.json file and deserialise to Dictionary<string, object>
            string gameJsonFilePath = Path.Combine(bundlePath, "Game.json");
            if (!File.Exists(gameJsonFilePath))
            {
                return null;
            }
            string gameJson = await File.ReadAllTextAsync(gameJsonFilePath);
            Dictionary<string, object>? gameEntity = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(gameJson);

            // now build T from the deserialized data and return
            // game content should be the first element under data/games
            if (
                gameEntity != null &&
                gameEntity.ContainsKey("data") && gameEntity["data"] is Dictionary<string, object> dataDict &&
                dataDict.ContainsKey("games") && dataDict["games"] is Dictionary<string, object> gamesDict &&
                gamesDict.ContainsKey(id.ToString()) && gamesDict[id.ToString()] is Dictionary<string, object> gameData)
            {
                // create a new instance of Game - TheGamesDB objects do not match the objects Gaseous uses, so we'll need to manually map the properties we want to keep from the bundle to our Game object
                T? game = Activator.CreateInstance(typeof(T)) as T;
                game.GetType().GetProperty("SourceType")?.SetValue(game, SourceType);
                game.GetType().GetProperty("Id")?.SetValue(game, Convert.ToInt64(gameData["id"]));
                game.GetType().GetProperty("Name")?.SetValue(game, gameData["game_title"].ToString());
                game.GetType().GetProperty("FirstReleaseDate")?.SetValue(game, gameData.ContainsKey("release_date") && DateTimeOffset.TryParse(gameData["release_date"].ToString(), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTimeOffset releaseDate) ? releaseDate : null);
                game.GetType().GetProperty("Summary")?.SetValue(game, gameData.ContainsKey("overview") ? gameData["overview"].ToString() : null);
                game.GetType().GetProperty("UpdatedAt")?.SetValue(game, gameData.ContainsKey("last_updated") && DateTimeOffset.TryParse(gameData["last_updated"].ToString(), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTimeOffset updatedAt) ? updatedAt : null);
                return game;
            }

            return null;
        }
    }
}