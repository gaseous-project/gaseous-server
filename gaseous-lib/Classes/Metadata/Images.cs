using System.Threading.Tasks;
using gaseous_server.Models;
using gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes;
using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using gaseous_server.Classes.Plugins.MetadataProviders.IGDBProvider;
using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace gaseous_server.Classes.Metadata
{
    public class ImageHandling
    {
        /// <summary>
        /// A static class responsible for caching image paths for game metadata, providing efficient retrieval of image paths while minimizing disk access. The cache is implemented using a concurrent dictionary for thread-safe access and includes mechanisms for cache expiration based on access patterns and age of the cache entries. The class also handles the storage of cache entries on disk to ensure persistence across application restarts, with proper error handling to log any issues that occur during caching operations without disrupting the overall functionality of the application.
        /// </summary>
        private static class ImageCache
        {
            // Limits: 50,000 entries in memory (small metadata items), 2GB on disk
            private const int MaxMemoryCacheEntries = 50000;
            private const long MaxDiskCacheSizeBytes = 2L * 1024 * 1024 * 1024; // 2 GB

            /// <summary>
            /// A concurrent dictionary to cache image paths for quick retrieval, reducing the need for repeated disk access and improving performance when fetching images. The key is a combination of MetadataMapId, MetadataSource, ImageType, ImageId, and size to ensure uniqueness for each image variant.
            /// </summary>
            private static ConcurrentDictionary<string, CacheEntry> imagePathCache = new ConcurrentDictionary<string, CacheEntry>();

            private static string DiskCacheDirectory
            {
                get
                {
                    string cacheDir = Path.Combine(Config.LibraryConfiguration.LibraryMetadataDirectory_Cache(), "imageIndex");
                    if (!Directory.Exists(cacheDir))
                    {
                        Directory.CreateDirectory(cacheDir);
                    }
                    return cacheDir;
                }
            }

            /// <summary>
            /// Represents a cache entry for an image path, including metadata such as the time it was added to the cache, last accessed time, access count, and frequency of access. This information can be used to implement cache expiration strategies based on usage patterns and age of the cache entries.
            /// </summary>
            public class CacheEntry
            {
                /// <summary>
                /// Gets or sets the path to the cached image file. This is the actual file path on disk where the image is stored, which can be used to quickly retrieve the image without needing to query the metadata providers again. The cache entry also includes metadata for managing cache expiration and access patterns.
                /// </summary>
                public required string ImagePath { get; set; }
                /// <summary>
                /// Gets or sets the timestamp when the image path was added to the cache. This information is crucial for implementing cache expiration strategies, allowing the system to determine how long an entry has been in the cache and whether it should be removed based on its age and access patterns.
                /// </summary>
                public DateTime Timestamp { get; set; }
                /// <summary>
                /// Gets or sets the last accessed time for the cache entry. This is updated each time the cache entry is accessed, allowing the system to track how recently an image has been used and make informed decisions about which entries to expire based on usage patterns.
                /// </summary>
                public DateTime LastAccessed { get; set; }
                /// <summary>
                /// Gets or sets the number of times the cache entry has been accessed. This metric can be used to identify frequently accessed images that should be kept in memory for faster retrieval, as well as rarely accessed images that may be candidates for expiration to free up resources.
                /// </summary>
                public int AccessCount { get; set; }
                /// <summary>
                /// Gets or sets the access frequency for the cache entry, calculated as the number of accesses divided by the age of the entry in seconds. This metric provides insight into how often an image is accessed relative to how long it has been in the cache, allowing for more nuanced expiration strategies that consider both recency and frequency of access.
                /// </summary>
                public int AccessFrequency { get; set; }
                /// <summary>
                /// Gets or sets the size of the cached image file in bytes. This information can be used to manage disk space usage for the cache, allowing the system to prioritize keeping smaller, frequently accessed images in memory while potentially expiring larger, less frequently accessed images to free up space.
                /// </summary>
                public int Size { get; set; }
                /// <summary>
                /// Gets or sets the ID of the metadata map associated with the image. This allows the cache entry to be linked back to the specific game and metadata source it belongs to, facilitating efficient retrieval and management of cached images based on their associated metadata.
                /// </summary>
                public long MetadataMapId { get; set; }
                /// <summary>
                /// Gets or sets the metadata source for the image, such as IGDB or TheGamesDB. This information is important for identifying the origin of the image and can be used to manage cache entries based on their source, allowing for source-specific expiration strategies if needed.
                /// </summary>
                public FileSignature.MetadataSources MetadataSource { get; set; }
                /// <summary>
                /// Gets or sets the type of image (e.g., Cover, Screenshot, Artwork, ClearLogo) for the cache entry. This allows the cache to differentiate between different types of images and manage them accordingly, such as prioritizing certain types of images for caching based on their importance or usage patterns.
                /// </summary>
                public ImageType ImageType { get; set; }
                /// <summary>
                /// Gets or sets the ID of the image associated with the cache entry. This allows the cache to uniquely identify each image and manage cache entries based on their specific image ID, facilitating efficient retrieval and expiration of cached images.
                /// </summary>
                public long ImageId { get; set; }
                /// <summary>
                /// Gets or sets the source image ID provided by the metadata source.
                /// </summary>
                public string SourceImageId { get; set; }
                /// <summary>
                /// Gets or sets the size of the image for the cache entry. This allows the cache to manage images based on their size, enabling strategies such as prioritizing smaller images for faster access or managing memory usage more effectively.
                /// </summary>
                public Plugins.PluginManagement.ImageResize.ImageSize ImageSize { get; set; }
                /// <summary>
                /// Gets the cache entry and updates access metadata such as last accessed time, access count, and access frequency. The access frequency is calculated as accesses-per-second, avoiding division errors and representing true usage rate. Frequently accessed items have higher AccessFrequency and should be kept in memory longer.
                /// </summary>
                [JsonIgnore]
                public CacheEntry Entry
                {
                    get
                    {
                        LastAccessed = DateTime.UtcNow;
                        AccessCount++;
                        // Calculate accesses per second, avoiding division by zero
                        long ageSeconds = (long)(DateTime.UtcNow - Timestamp).TotalSeconds;
                        AccessFrequency = ageSeconds > 0 ? (int)(AccessCount / (double)ageSeconds * 100) : AccessCount;
                        return this;
                    }
                }
            }

            public static string GetCacheKey(long MetadataMapId, FileSignature.MetadataSources MetadataSource, ImageType imageType, long ImageId, Plugins.PluginManagement.ImageResize.ImageSize size)
            {
                return $"{MetadataMapId}{Path.DirectorySeparatorChar}{MetadataSource}{Path.DirectorySeparatorChar}{imageType}{Path.DirectorySeparatorChar}{size}{Path.DirectorySeparatorChar}{ImageId}";
            }

            /// <summary>
            /// Adds an image path to the cache with associated metadata, including the time it was added, last accessed time, access count, and frequency. The method stores the cache entry in both an in-memory concurrent dictionary for fast access and on disk as a backup to ensure persistence across application restarts. Proper error handling is implemented to log any issues that occur during the caching process without disrupting the overall functionality of the application.
            /// </summary>
            /// <param name="MetadataMapId">The ID of the metadata map associated with the image.</param>
            /// <param name="MetadataSource">The source of the metadata.</param>
            /// <param name="imageType">The type of the image.</param>
            /// <param name="ImageId">The ID of the image.</param>
            /// <param name="size">The size of the image.</param>
            /// <param name="imagePath">The path to the image file.</param>
            /// <param name="sourceImageId">The ID of the source image.</param>
            /// <returns>A task representing the asynchronous operation.</returns>
            public static async Task AddToCache(long MetadataMapId, FileSignature.MetadataSources MetadataSource, ImageType imageType, long ImageId, Plugins.PluginManagement.ImageResize.ImageSize size, string imagePath, string sourceImageId)
            {
                string cacheKey = GetCacheKey(MetadataMapId, MetadataSource, imageType, ImageId, size);

                if (!File.Exists(imagePath))
                {
                    Logging.LogKey(Logging.LogType.Warning, "ImageCache", $"Attempted to add image path to cache for key {cacheKey}, but the file does not exist at path: {imagePath}");
                    return;
                }

                FileInfo fileInfo = new FileInfo(imagePath);
                int fileSize = (int)fileInfo.Length;

                CacheEntry newEntry = new CacheEntry
                {
                    ImagePath = imagePath,
                    Timestamp = DateTime.UtcNow,
                    LastAccessed = DateTime.UtcNow,
                    AccessCount = 0,
                    AccessFrequency = 0,
                    Size = fileSize,
                    MetadataMapId = MetadataMapId,
                    MetadataSource = MetadataSource,
                    ImageType = imageType,
                    ImageId = ImageId,
                    SourceImageId = sourceImageId,
                    ImageSize = size
                };

                // Enforce memory cache entry limit before adding
                if (imagePathCache.Count >= MaxMemoryCacheEntries)
                {
                    // Evict least frequently used entry (lowest AccessFrequency)
                    var lruEntry = imagePathCache.OrderBy(x => x.Value.AccessFrequency).ThenBy(x => x.Value.LastAccessed).FirstOrDefault();
                    if (!string.IsNullOrEmpty(lruEntry.Key))
                    {
                        imagePathCache.TryRemove(lruEntry.Key, out _);
                    }
                }

                // store in memory cache
                imagePathCache[cacheKey] = newEntry;

                // also store in disk cache as a backup in case the memory cache is cleared
                string diskCachePath = Path.Combine(DiskCacheDirectory, $"{cacheKey}.cache");
                string diskCacheDir = Path.GetDirectoryName(diskCachePath) ?? DiskCacheDirectory;
                try
                {
                    if (!Directory.Exists(diskCacheDir))
                    {
                        Directory.CreateDirectory(diskCacheDir);
                    }
                    string cacheContent = JsonSerializer.Serialize(newEntry);
                    await System.IO.File.WriteAllTextAsync(diskCachePath, cacheContent);
                }
                catch (Exception ex)
                {
                    Logging.LogKey(Logging.LogType.Warning, "ImageCache", $"Failed to write image path to disk cache for key {cacheKey}: {ex.Message}");
                }
            }

            /// <summary>
            /// Retrieves an image path from the cache based on the provided parameters. The method first checks the in-memory cache for a valid entry, and if not found, it looks for a corresponding file in the disk cache. If a valid cache entry is found, it returns the image path; otherwise, it returns null. The method also updates access metadata such as last accessed time and access count to facilitate cache expiration strategies.
            /// </summary>
            /// <param name="MetadataMapId">The ID of the metadata map associated with the image.</param>
            /// <param name="MetadataSource">The source of the metadata.</param>
            /// <param name="imageType">The type of the image.</param>
            /// <param name="ImageId">The ID of the image.</param>
            /// <param name="size">The size of the image.</param>
            /// <returns>The cache entry if found; otherwise, null.</returns>
            public static async Task<CacheEntry?> GetFromCache(long MetadataMapId, FileSignature.MetadataSources MetadataSource, ImageType imageType, long ImageId, Plugins.PluginManagement.ImageResize.ImageSize size)
            {
                string cacheKey = GetCacheKey(MetadataMapId, MetadataSource, imageType, ImageId, size);

                // check memory cache first
                if (imagePathCache.TryGetValue(cacheKey, out CacheEntry? cachedEntry))
                {
                    return cachedEntry?.Entry;
                }

                // if not in memory cache, check disk cache
                string diskCachePath = Path.Combine(DiskCacheDirectory, $"{cacheKey}.cache");
                if (System.IO.File.Exists(diskCachePath))
                {
                    try
                    {
                        string cacheContent = await System.IO.File.ReadAllTextAsync(diskCachePath);
                        CacheEntry? diskCacheEntry = JsonSerializer.Deserialize<CacheEntry>(cacheContent);
                        if (diskCacheEntry != null)
                        {
                            // add back to memory cache for faster access next time
                            imagePathCache[cacheKey] = diskCacheEntry;
                            // verify the target file exists before returning the cache entry
                            if (System.IO.File.Exists(diskCacheEntry.ImagePath))
                            {
                                return diskCacheEntry.Entry;
                            }
                            else
                            {
                                // if the target file doesn't exist, remove from cache
                                await RemoveFromCache(MetadataMapId, MetadataSource, imageType, ImageId, size);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logging.LogKey(Logging.LogType.Warning, "ImageCache", $"Failed to read image path from disk cache for key {cacheKey}: {ex.Message}");
                    }
                }

                return null;
            }

            /// <summary>
            /// Removes an image path from both memory and disk cache based on the provided parameters. This method is used to clear cache entries when they are no longer valid or needed, ensuring that stale data does not consume resources. It first attempts to remove the entry from the in-memory cache and then deletes the corresponding file from the disk cache if it exists. Proper error handling is implemented to log any issues that occur during the removal process without disrupting the overall functionality of the application.
            /// </summary>
            /// <param name="MetadataMapId">The ID of the metadata map associated with the image.</param>
            /// <param name="MetadataSource">The source of the metadata.</param>
            /// <param name="imageType">The type of the image.</param>
            /// <param name="ImageId">The ID of the image.</param>
            /// <param name="size">The size of the image.</param>
            /// <returns></returns>
            public static async Task RemoveFromCache(long MetadataMapId, FileSignature.MetadataSources MetadataSource, ImageType imageType, long ImageId, Plugins.PluginManagement.ImageResize.ImageSize size)
            {
                string cacheKey = GetCacheKey(MetadataMapId, MetadataSource, imageType, ImageId, size);

                // remove from memory cache if present
                imagePathCache.TryRemove(cacheKey, out _);

                // also remove from disk cache
                string diskCachePath = Path.Combine(DiskCacheDirectory, $"{cacheKey}.cache");
                if (System.IO.File.Exists(diskCachePath))
                {
                    try
                    {
                        System.IO.File.Delete(diskCachePath);
                    }
                    catch (Exception ex)
                    {
                        Logging.LogKey(Logging.LogType.Warning, "ImageCache", $"Failed to delete image path from disk cache for key {cacheKey}: {ex.Message}");
                    }
                }
            }

            /// <summary>
            /// Expires cache entries based on access patterns and age to free up memory and disk space. This method should be called periodically (every 5 minutes) to ensure the cache remains efficient and doesn't exceed size limits. Strategy: keep frequently accessed items in memory, move stale/old items to disk, delete ancient items entirely.
            /// </summary>
            /// <returns></returns>
            public static async Task ExpireCacheEntries()
            {
                DateTime now = DateTime.UtcNow;
                var entriesToRemove = new List<string>();
                var entriesToEvictFromMemory = new List<string>();

                // First pass: identify entries for removal or memory eviction
                foreach (var kvp in imagePathCache.ToList())
                {
                    CacheEntry entry = kvp.Value;
                    TimeSpan ageSpan = now - entry.LastAccessed;

                    // Stage 1: Remove entirely if image file no longer exists (safe cleanup)
                    if (!System.IO.File.Exists(entry.ImagePath))
                    {
                        entriesToRemove.Add(kvp.Key);
                    }
                    // Stage 2: Remove from memory if not accessed in 14 days (keep on disk)
                    else if (ageSpan.TotalDays > 14)
                    {
                        entriesToEvictFromMemory.Add(kvp.Key);
                    }
                    // Stage 3: Remove from memory if low frequency and old (>7 days, frequency < 50 accesses/sec*100)
                    else if (ageSpan.TotalDays > 7 && entry.AccessFrequency < 50)
                    {
                        entriesToEvictFromMemory.Add(kvp.Key);
                    }
                    // Stage 4: Remove from memory if very low frequency (< 10 accesses/sec*100) and older than 1 day
                    else if (ageSpan.TotalDays > 1 && entry.AccessFrequency < 10)
                    {
                        entriesToEvictFromMemory.Add(kvp.Key);
                    }
                }

                // Execute removals
                foreach (var key in entriesToRemove)
                {
                    if (imagePathCache.TryGetValue(key, out var entry))
                    {
                        await RemoveFromCache(entry.MetadataMapId, entry.MetadataSource, entry.ImageType, entry.ImageId, entry.ImageSize);
                    }
                }

                // Evict from memory only (keep on disk)
                foreach (var key in entriesToEvictFromMemory)
                {
                    imagePathCache.TryRemove(key, out _);
                }

                // Cleanup old disk cache files (>30 days untouched) and enforce size limit
                CleanupDiskCache(now);
            }

            /// <summary>
            /// Cleans up old disk cache files that haven't been accessed in 30 days, and enforces the 2GB disk cache size limit.
            /// </summary>
            private static void CleanupDiskCache(DateTime now)
            {
                try
                {
                    var diskCacheDir = new DirectoryInfo(DiskCacheDirectory);
                    if (!diskCacheDir.Exists)
                        return;

                    var cacheFiles = diskCacheDir.GetFiles("*.cache").ToList();
                    long totalSize = 0;

                    // Calculate total size and remove very old files
                    foreach (var file in cacheFiles.ToList())
                    {
                        if ((now - file.LastAccessTime).TotalDays > 30)
                        {
                            file.Delete();
                            cacheFiles.Remove(file);
                        }
                        else
                        {
                            totalSize += file.Length;
                        }
                    }

                    // If still over limit, delete oldest files by access time
                    if (totalSize > MaxDiskCacheSizeBytes)
                    {
                        var sortedByAge = cacheFiles.OrderBy(f => f.LastAccessTime).ToList();
                        foreach (var file in sortedByAge)
                        {
                            if (totalSize <= MaxDiskCacheSizeBytes)
                                break;

                            totalSize -= file.Length;
                            file.Delete();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logging.LogKey(Logging.LogType.Warning, "ImageCache", $"Failed to cleanup disk cache: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Triggers expiration of old and infrequently accessed entries from the image cache. Called periodically by the ImageCacheExpiryService (every 5 minutes).
        /// Enforces memory limit (50K entries), disk limit (2GB), removes stale entries, and prioritizes keeping frequently accessed items in memory.
        /// </summary>
        /// <returns>A task representing the asynchronous expiration operation.</returns>
        public static async Task ExpireImageCache()
        {
            await ImageCache.ExpireCacheEntries();
        }

        public static async Task<Dictionary<string, string>?> GameImage(long MetadataMapId, FileSignature.MetadataSources MetadataSource, ImageType imageType, long ImageId, Plugins.PluginManagement.ImageResize.ImageSize size, string imagename = "")
        {
            // validate imagename is not dangerous
            if (imagename.Contains("..") || imagename.Contains("/") || imagename.Contains("\\"))
            {
                imagename = ImageId.ToString();
            }

            // check image cache first
            ImageCache.CacheEntry? cacheEntry = await ImageCache.GetFromCache(MetadataMapId, MetadataSource, imageType, ImageId, size);
            if (cacheEntry != null)
            {
                return new Dictionary<string, string>
                {
                    { "imageId", cacheEntry.SourceImageId },
                    { "imagePath", cacheEntry.ImagePath },
                    { "imageType", imageType.ToString() },
                    { "imageSize", size.ToString() },
                    { "imageName", imagename }
                };
            }

            // if not in cache, proceed to fetch the image and add to cache
            try
            {
                MetadataMap.MetadataMapItem? metadataMap = null;
                Game? game = null;

                if (imageType == ImageType.ClearLogo)
                {
                    // search for the first metadata map item that has a clear logo
                    List<long> metadataMapItemIds = await MetadataManagement.GetAssociatedMetadataMapIds(MetadataMapId);

                    // Batch fetch all metadata maps to avoid N+1 queries
                    var metadataMapTasks = metadataMapItemIds.Select(id => MetadataManagement.GetMetadataMap(id)).ToList();
                    var metadataMaps = await Task.WhenAll(metadataMapTasks);

                    foreach (var metadataMapResult in metadataMaps)
                    {
                        metadataMap = metadataMapResult.MetadataMapItems.FirstOrDefault(x => x.SourceType == MetadataSource);
                        if (metadataMap != null)
                        {
                            game = await Games.GetGame(metadataMap.SourceType, metadataMap.SourceId);
                            if (game.ClearLogos != null && game.ClearLogos.ContainsKey(MetadataSource))
                            {
                                break;
                            }
                        }
                    }

                    if (metadataMap == null || game == null)
                    {
                        return null;
                    }
                }
                else
                {
                    var metadataMapResult = await MetadataManagement.GetMetadataMap(MetadataMapId);
                    if (metadataMapResult == null)
                    {
                        return null;
                    }
                    metadataMap = metadataMapResult.MetadataMapItems.FirstOrDefault(x => x.SourceType == MetadataSource);
                    if (metadataMap == null)
                    {
                        return null;
                    }
                    game = await Classes.Metadata.Games.GetGame(metadataMap.SourceType, metadataMap.SourceId);
                }

                if (game == null)
                {
                    return null;
                }

                string? imageId = null;
                var imagePaths = new Dictionary<gaseous_server.Classes.Plugins.PluginManagement.ImageResize.ImageSize, string>();

                switch (imageType)
                {
                    case ImageType.Cover:
                        if (game.Cover != null)
                        {
                            // Cover cover = Classes.Metadata.Covers.GetCover(game.MetadataSource, (long?)game.Cover);
                            Cover cover = await Classes.Metadata.Covers.GetCover(game.MetadataSource, (long?)ImageId);
                            if (cover == null)
                            {
                                return null;
                            }
                            imageId = cover.ImageId;
                            imagePaths = cover.Paths.FilePaths;
                        }
                        break;

                    case ImageType.Screenshot:
                        if (game.Screenshots != null)
                        {
                            if (game.Screenshots.Contains(ImageId))
                            {
                                Screenshot imageObject = await Screenshots.GetScreenshotAsync(game.MetadataSource, ImageId);
                                if (imageObject == null)
                                {
                                    return null;
                                }
                                imageId = imageObject.ImageId;
                                imagePaths = imageObject.Paths.FilePaths;
                            }
                        }
                        break;

                    case ImageType.Artwork:
                        if (game.Artworks != null)
                        {
                            if (game.Artworks.Contains(ImageId))
                            {
                                Artwork imageObject = await Artworks.GetArtwork(game.MetadataSource, ImageId);
                                if (imageObject == null)
                                {
                                    return null;
                                }
                                imageId = imageObject.ImageId;
                                imagePaths = imageObject.Paths.FilePaths;
                            }
                        }
                        break;

                    case ImageType.ClearLogo:
                        if (game.ClearLogos != null)
                        {
                            if (game.ClearLogos.ContainsKey(MetadataSource))
                            {
                                ClearLogo? imageObject = await ClearLogos.GetClearLogo(game.MetadataSource, ImageId);
                                if (imageObject == null)
                                {
                                    return null;
                                }
                                imageId = imageObject.ImageId;
                                imagePaths = imageObject.Paths.FilePaths;
                            }
                        }
                        break;

                    default:
                        return null;
                }

                if (imageId == null)
                {
                    return null;
                }

                string imagePath = imagePaths[size];

                if (!System.IO.File.Exists(imagePath))
                {
                    // "download" the image by writing the bytes to disk
                    byte[]? imageBytes = await Metadata.GetImageAsync(MetadataSource, (long)game.Id, imageType, imageId, size);
                    if (imageBytes == null)
                    {
                        return null;
                    }
                    string? imageDir = Path.GetDirectoryName(imagePath);
                    if (imageDir != null && !Directory.Exists(imageDir))
                    {
                        Directory.CreateDirectory(imageDir);
                    }
                    await System.IO.File.WriteAllBytesAsync(imagePath, imageBytes);
                }

                // add to cache for future requests
                await ImageCache.AddToCache(MetadataMapId, MetadataSource, imageType, ImageId, size, imagePath, imageId);

                return new Dictionary<string, string>
                {
                    { "imageId", imageId },
                    { "imagePath", imagePath },
                    { "imageType", imageType.ToString() },
                    { "imageSize", size.ToString() },
                    { "imageName", imagename }
                };
            }
            catch (Exception ex)
            {
                Logging.LogKey(Logging.LogType.Warning, "ImageHandling", $"Failed to get image for MetadataMapId {MetadataMapId}, Source {MetadataSource}, ImageType {imageType}, ImageId {ImageId}: {ex.Message}");
                return null;
            }
        }
    }

    /// <summary>
    /// Represents the path and metadata for an image associated with a game.
    /// </summary>
    public class ImagePath
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImagePath"/> class.
        /// </summary>
        /// <param name="SourceType">The metadata source type.</param>
        /// <param name="ProviderName">The name of the metadata provider.</param>
        /// <param name="gameId">The ID of the game.</param>
        /// <param name="imageType">The type of image.</param>
        /// <param name="imagename">The name of the image file.</param>
        public ImagePath(FileSignature.MetadataSources SourceType, string ProviderName, long gameId, ImageType imageType, string imagename)
        {
            this._SourceType = SourceType;
            this._ProviderName = ProviderName;
            this._gameId = gameId;
            this._imageType = imageType;
            this._imagename = imagename;
            if (!this._imagename.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
            {
                this._imagename += ".jpg";
            }
        }

        private FileSignature.MetadataSources _SourceType = FileSignature.MetadataSources.None;
        /// <summary>
        /// Gets the metadata source type.
        /// </summary>
        public FileSignature.MetadataSources SourceType
        {
            get { return _SourceType; }
        }
        private string _ProviderName = string.Empty;
        /// <summary> Gets the name of the metadata provider.
        /// </summary> <returns>The name of the metadata provider.</returns>
        public string ProviderName
        {
            get { return _ProviderName; }
        }
        private long _gameId = 0;
        /// <summary>
        /// Gets the ID of the game.
        /// </summary>
        public long GameId
        {
            get { return _gameId; }
        }
        private ImageType _imageType = ImageType.Cover;
        /// <summary>
        /// Gets the type of image.
        /// </summary>
        public ImageType imageType
        {
            get { return _imageType; }
        }
        private string _imagename = string.Empty;
        /// <summary>
        /// Gets the name of the image file.
        /// </summary>
        public string ImageName
        {
            get { return _imagename; }
        }

        private string ProviderImageType(FileSignature.MetadataSources sourceType, ImageType imageType)
        {
            switch (sourceType)
            {
                case FileSignature.MetadataSources.IGDB:
                    return imageType switch
                    {
                        ImageType.Cover => "cover",
                        ImageType.Screenshot => "screenshots",
                        ImageType.Artwork => "artworks",
                        ImageType.ClearLogo => "clearlogo",
                        _ => throw new Exception("Invalid image type")
                    };
                case FileSignature.MetadataSources.TheGamesDb:
                    return imageType switch
                    {
                        ImageType.Cover => "boxart",
                        ImageType.Screenshot => "screenshot",
                        ImageType.Artwork => "fanart",
                        ImageType.ClearLogo => "clearlogo",
                        _ => throw new Exception("Invalid image type")
                    };
                default:
                    return imageType.ToString().ToLower();
            }
        }

        /// <summary>
        /// Gets the file paths for all image sizes, including the original and cached resized versions.
        /// </summary>
        public Dictionary<Plugins.PluginManagement.ImageResize.ImageSize, string> FilePaths
        {
            get
            {
                Dictionary<Plugins.PluginManagement.ImageResize.ImageSize, string> filePaths = new Dictionary<Plugins.PluginManagement.ImageResize.ImageSize, string>();
                foreach (Plugins.PluginManagement.ImageResize.ImageSize size in Enum.GetValues(typeof(Plugins.PluginManagement.ImageResize.ImageSize)))
                {
                    if (size == Plugins.PluginManagement.ImageResize.ImageSize.original)
                    {
                        filePaths[size] = Path.Combine(Config.LibraryConfiguration.LibraryMetadataDirectory_GameBundles(SourceType, ProviderName, GameId), ProviderImageType(SourceType, imageType), ImageName);
                    }
                    else
                    {
                        filePaths[size] = Path.Combine(Config.LibraryConfiguration.LibraryMetadataDirectory_Cache(), "images", SourceType.ToString(), ProviderName, GameId.ToString(), ProviderImageType(SourceType, imageType), size.ToString(), ImageName);
                    }
                }
                return filePaths;
            }
        }
    }
}