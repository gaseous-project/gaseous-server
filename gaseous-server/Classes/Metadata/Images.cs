using System.Threading.Tasks;
using gaseous_server.Models;
using gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes;
using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using gaseous_server.Classes.Plugins.MetadataProviders.IGDBProvider;

namespace gaseous_server.Classes.Metadata
{
    public class ImageHandling
    {
        public static async Task<Dictionary<string, string>?> GameImage(long MetadataMapId, FileSignature.MetadataSources MetadataSource, ImageType imageType, long ImageId, Plugins.PluginManagement.ImageResize.ImageSize size, string imagename = "")
        {
            // validate imagename is not dangerous
            if (imagename.Contains("..") || imagename.Contains("/") || imagename.Contains("\\"))
            {
                imagename = ImageId.ToString();
            }

            try
            {
                MetadataMap.MetadataMapItem metadataMap = null;
                Game game = null;

                if (imageType == ImageType.ClearLogo)
                {
                    // search for the first metadata map item that has a clear logo
                    List<long> metadataMapItemIds = await MetadataManagement.GetAssociatedMetadataMapIds(MetadataMapId);

                    foreach (long metadataMapItemId in metadataMapItemIds)
                    {
                        metadataMap = (await MetadataManagement.GetMetadataMap(metadataMapItemId)).MetadataMapItems.FirstOrDefault(x => x.SourceType == MetadataSource);
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
                    metadataMap = (await Classes.MetadataManagement.GetMetadataMap(MetadataMapId)).MetadataMapItems.FirstOrDefault(x => x.SourceType == MetadataSource);
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
                    if (!Directory.Exists(Path.GetDirectoryName(imagePath)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(imagePath));
                    }
                    await System.IO.File.WriteAllBytesAsync(imagePath, imageBytes);
                }

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