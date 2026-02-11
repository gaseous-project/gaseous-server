using System.Threading.Tasks;
using gaseous_server.Models;
using gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes;
using Microsoft.CodeAnalysis.Elfie.Model.Strings;

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

                string? imageId = null;
                string? imageTypePath = null;

                switch (imageType)
                {
                    case ImageType.Cover:
                        if (game.Cover != null)
                        {
                            // Cover cover = Classes.Metadata.Covers.GetCover(game.MetadataSource, (long?)game.Cover);
                            Cover cover = await Classes.Metadata.Covers.GetCover(game.MetadataSource, (long?)ImageId);
                            imageId = cover.ImageId;
                            imageTypePath = "Covers";
                        }
                        break;

                    case ImageType.Screenshot:
                        if (game.Screenshots != null)
                        {
                            if (game.Screenshots.Contains(ImageId))
                            {
                                Screenshot imageObject = await Screenshots.GetScreenshotAsync(game.MetadataSource, ImageId);

                                imageId = imageObject.ImageId;
                                imageTypePath = "Screenshots";
                            }
                        }
                        break;

                    case ImageType.Artwork:
                        if (game.Artworks != null)
                        {
                            if (game.Artworks.Contains(ImageId))
                            {
                                Artwork imageObject = await Artworks.GetArtwork(game.MetadataSource, ImageId);

                                imageId = imageObject.ImageId;
                                imageTypePath = "Artwork";
                            }
                        }
                        break;

                    case ImageType.ClearLogo:
                        if (game.ClearLogos != null)
                        {
                            if (game.ClearLogos.ContainsKey(MetadataSource))
                            {
                                ClearLogo? imageObject = await ClearLogos.GetClearLogo(game.MetadataSource, ImageId);

                                if (imageObject != null)
                                {
                                    imageId = imageObject.ImageId;
                                    imageTypePath = "ClearLogo";
                                }
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

                string basePath = Path.Combine(Config.LibraryConfiguration.LibraryMetadataDirectory_Game(game), imageTypePath, metadataMap.SourceType.ToString(), size.ToString());
                string imagePath = Path.Combine(basePath, imageId + ".jpg");

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
}