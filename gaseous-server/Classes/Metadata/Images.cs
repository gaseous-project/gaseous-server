using System.Threading.Tasks;
using gaseous_server.Models;
using HasheousClient.Models.Metadata.IGDB;
using Microsoft.CodeAnalysis.Elfie.Model.Strings;

namespace gaseous_server.Classes.Metadata
{
    public class ImageHandling
    {
        public static async Task<Dictionary<string, string>?> GameImage(long MetadataMapId, HasheousClient.Models.MetadataSources MetadataSource, MetadataImageType imageType, long ImageId, Communications.IGDBAPI_ImageSize size, string imagename = "")
        {
            // validate imagename is not dangerous
            if (imagename.Contains("..") || imagename.Contains("/") || imagename.Contains("\\"))
            {
                imagename = ImageId.ToString();
            }

            try
            {
                MetadataMap.MetadataMapItem metadataMap = null;
                gaseous_server.Models.Game game = null;

                if (imageType == MetadataImageType.clearlogo)
                {
                    // search for the first metadata map item that has a clear logo
                    List<long> metadataMapItemIds = await Classes.MetadataManagement.GetAssociatedMetadataMapIds(MetadataMapId);

                    foreach (long metadataMapItemId in metadataMapItemIds)
                    {
                        metadataMap = (await Classes.MetadataManagement.GetMetadataMap(metadataMapItemId)).MetadataMapItems.FirstOrDefault(x => x.SourceType == MetadataSource);
                        if (metadataMap != null)
                        {
                            game = await Classes.Metadata.Games.GetGame(metadataMap.SourceType, metadataMap.SourceId);
                            if (game.ClearLogo != null && game.ClearLogo.ContainsKey(MetadataSource))
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
                    case MetadataImageType.cover:
                        if (game.Cover != null)
                        {
                            // Cover cover = Classes.Metadata.Covers.GetCover(game.MetadataSource, (long?)game.Cover);
                            Cover cover = await Classes.Metadata.Covers.GetCover(game.MetadataSource, (long?)ImageId);
                            imageId = cover.ImageId;
                            imageTypePath = "Covers";
                        }
                        break;

                    case MetadataImageType.screenshots:
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

                    case MetadataImageType.artwork:
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

                    case MetadataImageType.clearlogo:
                        if (game.ClearLogo != null)
                        {
                            if (game.ClearLogo.ContainsKey(MetadataSource))
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

                string basePath = Path.Combine(Config.LibraryConfiguration.LibraryMetadataDirectory_Game(game), imageTypePath, metadataMap.SourceType.ToString());
                string imagePath = Path.Combine(basePath, size.ToString(), imageId + ".jpg");

                if (!System.IO.File.Exists(imagePath))
                {
                    Communications comms = new Communications();
                    imagePath = await comms.GetSpecificImageFromServer(game.MetadataSource, Path.Combine(Config.LibraryConfiguration.LibraryMetadataDirectory_Game(game), imageTypePath), imageId, size, new List<Communications.IGDBAPI_ImageSize> { Communications.IGDBAPI_ImageSize.cover_big, Communications.IGDBAPI_ImageSize.original });
                }

                if (!System.IO.File.Exists(imagePath))
                {
                    Communications comms = new Communications();
                    imagePath = await comms.GetSpecificImageFromServer(game.MetadataSource, basePath, imageId, size, new List<Communications.IGDBAPI_ImageSize> { Communications.IGDBAPI_ImageSize.cover_big, Communications.IGDBAPI_ImageSize.original });
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
            catch
            {
                return null;
            }
        }

        public enum MetadataImageType
        {
            cover,
            screenshots,
            artwork,
            clearlogo
        }
    }
}