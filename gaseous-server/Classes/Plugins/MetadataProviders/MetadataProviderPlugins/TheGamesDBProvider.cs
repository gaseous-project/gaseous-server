using gaseous_server.Classes.Metadata;
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
        public async Task<AgeRating?> GetAgeRatingAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<AgeRating>(id, false);
        }

        /// <inheritdoc/>
        public async Task<AgeRatingCategory?> GetAgeRatingCategoryAsync(long id, bool forceRefresh = false)
        {
            // only ESRB ratings are supported by TheGamesDB, so we can return a hardcoded category object for ESRB categories
            AgeGroups.AgeGroupMapModel.RatingBoardModel esrbRatingBoard = AgeGroups.AgeGroupMap.RatingBoards["ESRB"];

            if (esrbRatingBoard == null)
            {
                return null;
            }

            // search the ESRB ratings for a rating with a matching IGDB ID to the requested category ID - if found return an AgeRatingCategory object with the name of the rating as the category name
            foreach (var rating in esrbRatingBoard.Ratings)
            {
                if (rating.Value.IGDBId == id && !string.IsNullOrEmpty(rating.Value.Name))
                {
                    return new AgeRatingCategory
                    {
                        Id = id,
                        Rating = rating.Value.Name,
                        Organization = 1,
                        SourceType = FileSignature.MetadataSources.TheGamesDb
                    };
                }
            }

            return null;
        }

        /// <inheritdoc/>
        public async Task<AgeRatingContentDescription?> GetAgeRatingContentDescriptionAsync(long id, bool forceRefresh = false)
        {
            return null;
        }

        /// <inheritdoc/>
        public async Task<AgeRatingOrganization?> GetAgeRatingOrganizationAsync(long id, bool forceRefresh = false)
        {
            // only ESRB ratings are supported by TheGamesDB, so we can return a hardcoded organization object for ESRB
            if (id == 1)
            {
                AgeGroups.AgeGroupMapModel.RatingBoardModel esrbRatingBoard = AgeGroups.AgeGroupMap.RatingBoards["ESRB"];
                if (esrbRatingBoard == null || esrbRatingBoard.IGDBId == null || string.IsNullOrEmpty(esrbRatingBoard.Name))
                {
                    return null;
                }
                return new AgeRatingOrganization
                {
                    Id = (long)esrbRatingBoard.IGDBId,
                    Name = esrbRatingBoard.Name,
                    SourceType = FileSignature.MetadataSources.TheGamesDb
                };
            }
            return null;
        }

        /// <inheritdoc/>
        public async Task<AlternativeName?> GetAlternativeNameAsync(long id, bool forceRefresh = false)
        {
            return null;
        }

        /// <inheritdoc/>
        public async Task<Artwork?> GetArtworkAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<Artwork>(id, false);
        }

        /// <inheritdoc/>
        public async Task<ClearLogo?> GetClearLogoAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<ClearLogo>(id, false);
        }

        /// <inheritdoc/>
        public async Task<Collection?> GetCollectionAsync(long id, bool forceRefresh = false)
        {
            return null;
        }

        /// <inheritdoc/>
        public async Task<Company?> GetCompanyAsync(long id, bool forceRefresh = false)
        {
            return null;
        }

        /// <inheritdoc/>
        public async Task<CompanyLogo?> GetCompanyLogoAsync(long id, bool forceRefresh = false)
        {
            return null;
        }

        /// <inheritdoc/>
        public async Task<Cover?> GetCoverAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<Cover>(id, false);
        }

        /// <inheritdoc/>
        public async Task<ExternalGame?> GetExternalGameAsync(long id, bool forceRefresh = false)
        {
            return null;
        }

        /// <inheritdoc/>
        public async Task<Franchise?> GetFranchiseAsync(long id, bool forceRefresh = false)
        {
            return null;
        }

        /// <inheritdoc/>
        public async Task<Game?> GetGameAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<Game>(id, forceRefresh);
        }

        /// <inheritdoc/>
        public async Task<GameLocalization?> GetGameLocalizationAsync(long id, bool forceRefresh = false)
        {
            return null;
        }

        /// <inheritdoc/>
        public async Task<GameMode?> GetGameModeAsync(long id, bool forceRefresh = false)
        {
            return null;
        }

        /// <inheritdoc/>
        public async Task<GameVideo?> GetGameVideoAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<GameVideo>(id, false);
        }

        /// <inheritdoc/>
        public async Task<Genre?> GetGenreAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<Genre>(id, false);
        }

        /// <inheritdoc/>
        public async Task<InvolvedCompany?> GetInvolvedCompanyAsync(long id, bool forceRefresh = false)
        {
            return null;
        }

        /// <inheritdoc/>
        public async Task<MultiplayerMode?> GetMultiplayerModeAsync(long id, bool forceRefresh = false)
        {
            return null;
        }

        /// <inheritdoc/>
        public async Task<Platform?> GetPlatformAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<Platform>(id, false);
        }

        /// <inheritdoc/>
        public async Task<PlatformLogo?> GetPlatformLogoAsync(long id, bool forceRefresh = false)
        {
            return null;
        }

        /// <inheritdoc/>
        public async Task<PlatformVersion?> GetPlatformVersionAsync(long id, bool forceRefresh = false)
        {
            return null;
        }

        /// <inheritdoc/>
        public async Task<PlayerPerspective?> GetPlayerPerspectiveAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<PlayerPerspective>(id, forceRefresh);
        }

        /// <inheritdoc/>
        public async Task<Region?> GetRegionAsync(long id, bool forceRefresh = false)
        {
            return null;
        }

        /// <inheritdoc/>
        public async Task<ReleaseDate?> GetReleaseDateAsync(long id, bool forceRefresh = false)
        {
            return null;
        }

        /// <inheritdoc/>
        public async Task<Screenshot?> GetScreenshotAsync(long id, bool forceRefresh = false)
        {
            return await GetEntityAsync<Screenshot>(id, false);
        }

        /// <inheritdoc/>
        public async Task<Theme?> GetThemeAsync(long id, bool forceRefresh = false)
        {
            return null;
        }

        /// <inheritdoc/>
        public async Task<Game[]?> SearchGamesAsync(SearchType searchType, long platformId, List<string> searchCandidates)
        {
            return null;
        }

        /// <inheritdoc/>
        public async Task<byte[]?> GetGameImageAsync(long gameId, string url, ImageType imageType)
        {
            // supplied url is a partial file name and path in unix path format - we need to break it up so that we can combine it with the base path in a host OS compliant way
            List<string> pathSegments =
            [
                Config.LibraryConfiguration.LibraryMetadataDirectory_GameBundles(FileSignature.MetadataSources.TheGamesDb, gameId),
                .. url.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries),
            ];

            string combinedPath = Path.Combine(pathSegments.ToArray());

            if (File.Exists(combinedPath))
            {
                return await File.ReadAllBytesAsync(combinedPath);
            }

            // fallback to downloading the image directly from Hasheous if it's not present in the game bundle - this can happen if the image was added to TheGamesDB after the bundle was downloaded and hasn't been refreshed yet
            string imageDirectory = Path.Combine(Config.LibraryConfiguration.LibraryMetadataDirectory_GameBundles(SourceType, gameId), imageType.ToString());
            string directImagePath = Path.Combine(imageDirectory, url);

            if (File.Exists(directImagePath))
            {
                return await File.ReadAllBytesAsync(directImagePath);
            }

            if (!Directory.Exists(Path.GetDirectoryName(directImagePath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(directImagePath));
            }

            Uri imageUri = new Uri($"https://hasheous.org/api/v1/MetadataProxy/TheGamesDB/Images/original/{url}");
            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "X-Client-API-Key", Config.MetadataConfiguration.HasheousClientAPIKey }
            };

            var response = await comms.DownloadToFileAsync(imageUri, directImagePath, headers);
            if (response.StatusCode == 200)
            {
                return await File.ReadAllBytesAsync(directImagePath);
            }

            return null;
        }

        private async Task<T?> GetEntityAsync<T>(long id, bool forceRefresh = false) where T : class
        {
            if (id == 0)
            {
                return null;
            }

            T? metadata = Activator.CreateInstance(typeof(T)) as T;

            // get content from database - if not present fetch it from the server, but only if T is Game
            var typeName = typeof(T).Name;
            var cacheStatus = await Storage.GetCacheStatusAsync(typeName, id);
            if (cacheStatus == Storage.CacheStatus.Current && forceRefresh == false)
            {
                T? cachedItem = await Storage.GetCacheValue<T>(metadata, "id", id);

                if (cachedItem != null)
                {
                    return cachedItem;
                }
            }
            else
            {
                // fetch from server
                // if T is not game return null
                if (typeof(T) != typeof(Game))
                {
                    return null;
                }

                // fetch the bundle - this process will populate/update the database for other supported types
                try
                {
                    var game = await GetGameBundleAsync(id);
                    metadata = game as T;
                }
                catch (Exception ex)
                {
                    // Log the exception or handle it as needed
                }

                return metadata;
            }

            return null;
        }

        private async Task<Game?> GetGameBundleAsync(long id)
        {
            // check if bundle exists
            string bundlePath = Config.LibraryConfiguration.LibraryMetadataDirectory_GameBundles(SourceType, id);
            bool forceDownloadBundle = false;
            if (Directory.Exists(bundlePath))
            {
                // open the Game.json file
                string gameJsonPath = Path.Combine(bundlePath, "Game.json");
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
                    throw new Exception($"Failed to download bundle from {bundleUrl}. Status code: {response.StatusCode}");
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
            // open the game.json file and deserialise to HasheousClient.Models.Metadata.TheGamesDb.GamesByGameID
            string gameJsonFilePath = Path.Combine(bundlePath, "Game.json");
            if (!File.Exists(gameJsonFilePath))
            {
                throw new Exception($"Game.json file not found at {gameJsonFilePath}");
            }
            string gameJson = await File.ReadAllTextAsync(gameJsonFilePath);
            HasheousClient.Models.Metadata.TheGamesDb.GamesByGameID? gameEntity = Newtonsoft.Json.JsonConvert.DeserializeObject<HasheousClient.Models.Metadata.TheGamesDb.GamesByGameID>(gameJson);

            // game content should be the first element under data/games
            if (gameEntity != null && gameEntity.data != null && gameEntity.data.games != null && gameEntity.data.games.Count > 0)
            {
                var game = gameEntity.data.games[0];

                // create new IGDB game objects
                Game? igdbGame = null;
                AgeRating? igdbAgeRating = null;
                Cover? igdbCover = null;
                GameVideo? igdbVideo = null;
                List<Artwork> igdbArtwork = new List<Artwork>();
                List<ClearLogo> igdbClearLogo = new List<ClearLogo>();
                List<Screenshot> igdbScreenshot = new List<Screenshot>();

                // create new age rating object - all TheGamesDb age ratings are ESRB ratings
                if (game.rating != null)
                {
                    string tgdbRatingName = game.rating.Split(" - ")[0];
                    long? igdbAgeRatingTitle = null;
                    if (AgeGroups.AgeGroupMap.RatingBoards["ESRB"].Ratings.ContainsKey(tgdbRatingName))
                    {
                        igdbAgeRatingTitle = AgeGroups.AgeGroupMap.RatingBoards["ESRB"].Ratings[tgdbRatingName].IGDBId;
                    }
                    if (igdbAgeRatingTitle.HasValue)
                    {
                        igdbAgeRating = new AgeRating
                        {
                            Id = game.id,
                            Organization = 1,
                            RatingCategory = (long)igdbAgeRatingTitle,
                            SourceType = FileSignature.MetadataSources.TheGamesDb
                        };
                        // update the cache
                        await Storage.StoreCacheValue<AgeRating>(igdbAgeRating);
                    }
                }

                // process images
                if (
                    gameEntity.include != null &&
                    gameEntity.include.boxart != null &&
                    gameEntity.include.boxart.data != null &&
                    gameEntity.include.boxart.data.ContainsKey(game.id.ToString())
                )
                {
                    string originalUrl = gameEntity.include.boxart.base_url.original;

                    List<HasheousClient.Models.Metadata.TheGamesDb.GameImage> imageDict = gameEntity.include.boxart.data[game.id.ToString()];
                    foreach (var image in imageDict)
                    {
                        int width = 0;
                        int height = 0;

                        if (image.resolution == null || image.resolution == "")
                        {
                            image.resolution = "0x0";
                        }

                        width = int.TryParse(image.resolution.Split("x")[0].Trim(), out width) ? width : 0;
                        height = int.TryParse(image.resolution.Split("x")[1].Trim(), out height) ? height : 0;

                        switch (image.type)
                        {
                            case "boxart":
                                // detect cover art
                                if (igdbCover == null && image.side == "front")
                                {
                                    igdbCover = new Cover
                                    {
                                        Id = image.id,
                                        ImageId = image.filename,
                                        Width = width,
                                        Height = height,
                                        Url = new Uri(originalUrl + image.filename).ToString(),
                                        AlphaChannel = false,
                                        Animated = false,
                                        Game = game.id,
                                        SourceType = FileSignature.MetadataSources.TheGamesDb
                                    };
                                    await Storage.StoreCacheValue<Cover>(igdbCover);
                                }
                                break;

                            case "fanart":
                                // check that igdbArtwork doesn't include an item with this filename already
                                if (igdbArtwork.Any(a => a.ImageId == image.filename))
                                {
                                    continue;
                                }

                                Artwork igdbArtworkItem = new Artwork
                                {
                                    Id = image.id,
                                    ImageId = image.filename,
                                    Width = width,
                                    Height = height,
                                    Url = new Uri(originalUrl + image.filename).ToString(),
                                    AlphaChannel = false,
                                    Animated = false,
                                    Game = game.id,
                                    SourceType = FileSignature.MetadataSources.TheGamesDb
                                };
                                igdbArtwork.Add(igdbArtworkItem);
                                await Storage.StoreCacheValue<Artwork>(igdbArtworkItem);
                                break;

                            case "clearlogo":
                                // check that igdbClearLogo doesn't include an item with this filename already
                                if (igdbClearLogo.Any(a => a.ImageId == image.filename))
                                {
                                    continue;
                                }

                                ClearLogo igdbClearLogoItem = new ClearLogo
                                {
                                    Id = image.id,
                                    ImageId = image.filename,
                                    Width = width,
                                    Height = height,
                                    Url = new Uri(originalUrl + image.filename).ToString(),
                                    AlphaChannel = false,
                                    Animated = false,
                                    Game = game.id,
                                    SourceType = FileSignature.MetadataSources.TheGamesDb
                                };
                                igdbClearLogo.Add(igdbClearLogoItem);
                                await Storage.StoreCacheValue<ClearLogo>(igdbClearLogoItem);
                                break;

                            case "screenshot":
                                // check that igdbScreenshot doesn't include an item with this filename already
                                if (igdbScreenshot.Any(a => a.ImageId == image.filename))
                                {
                                    continue;
                                }

                                Screenshot igdbScreenshotItem = new Screenshot
                                {
                                    Id = image.id,
                                    ImageId = image.filename,
                                    Width = width,
                                    Height = height,
                                    Url = new Uri(originalUrl + image.filename).ToString(),
                                    AlphaChannel = false,
                                    Animated = false,
                                    Game = game.id,
                                    SourceType = FileSignature.MetadataSources.TheGamesDb
                                };
                                igdbScreenshot.Add(igdbScreenshotItem);
                                await Storage.StoreCacheValue<Screenshot>(igdbScreenshotItem);
                                break;
                        }
                    }
                }

                // create video object
                if (!String.IsNullOrEmpty(game.youtube))
                {
                    igdbVideo = new GameVideo
                    {
                        Id = game.id,
                        Name = game.game_title,
                        VideoId = game.youtube,
                        Game = game.id,
                        SourceType = FileSignature.MetadataSources.TheGamesDb
                    };
                    await Storage.StoreCacheValue<GameVideo>(igdbVideo);
                }

                // create new game object
                igdbGame = new Game
                {
                    Id = game.id,
                    Name = game.game_title,
                    FirstReleaseDate = DateTimeOffset.TryParse(game.release_date, out var releaseDate) ? releaseDate : (DateTimeOffset?)null,
                    Summary = game.overview,
                    Slug = string.Join("-", game.game_title.Trim().ToLower().Replace(" ", "-").Split(Common.GetInvalidFileNameChars())) + "-" + game.id,
                    Genres = game.genres?.Select(g => (long)g).ToList() ?? new List<long>(),
                    SourceType = FileSignature.MetadataSources.TheGamesDb
                };

                if (igdbAgeRating != null && igdbAgeRating.Id != null)
                {
                    igdbGame.AgeRatings = new List<long> { (long)igdbAgeRating.Id };
                }

                if (igdbCover != null && igdbCover.Id != null)
                {
                    igdbGame.Cover = (long)igdbCover.Id;
                }

                if (igdbClearLogo != null && igdbClearLogo.Count > 0)
                {
                    // add all the id's from the igdbClearLogo list to the igdbGame.ClearLogos list
                    igdbGame.ClearLogo = igdbClearLogo.Where(cl => cl.Id.HasValue).Select(cl => (long)cl.Id!).ToList();
                }

                if (igdbVideo != null && igdbVideo.Id.HasValue)
                {
                    igdbGame.Videos = new List<long> { (long)igdbVideo.Id };
                }

                if (igdbArtwork != null)
                {
                    igdbGame.Artworks = new List<long>();
                    foreach (HasheousClient.Models.Metadata.IGDB.Artwork artwork in igdbArtwork)
                    {
                        if (artwork.Id.HasValue && !igdbGame.Artworks.Contains((long)artwork.Id))
                        {
                            igdbGame.Artworks.Add((long)artwork.Id);
                        }
                    }
                }

                if (igdbScreenshot != null)
                {
                    igdbGame.Screenshots = new List<long>();
                    foreach (HasheousClient.Models.Metadata.IGDB.Screenshot screenshot in igdbScreenshot)
                    {
                        if (screenshot.Id.HasValue && !igdbGame.Screenshots.Contains((long)screenshot.Id))
                        {
                            igdbGame.Screenshots.Add((long)screenshot.Id);
                        }
                    }
                }

                await Storage.StoreCacheValue<Game>(igdbGame);
                return igdbGame;
            }

            return null;
        }
    }
}