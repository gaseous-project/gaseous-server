using System;
using System.Threading.Tasks;
using gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes;
using static gaseous_server.Models.PlatformMapping;


namespace gaseous_server.Classes.Metadata
{
    public class PlatformLogos
    {
        public PlatformLogos()
        {
        }

        public static async Task<PlatformLogo?> GetPlatformLogo(long? Id, FileSignature.MetadataSources SourceType = FileSignature.MetadataSources.IGDB, bool GetImage = false)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                PlatformLogo? RetVal = await Metadata.GetMetadataAsync<PlatformLogo>(SourceType, (long)Id, false);
                return RetVal;
            }
        }

        public static async Task<string> GetPlatformLogoImage(Platform platform, PlatformLogo platformLogo, FileSignature.MetadataSources metadataSources)
        {
            string basePath = Path.Combine(Config.LibraryConfiguration.LibraryMetadataDirectory_Platform(platform), metadataSources.ToString());
            string imagePath = Path.Combine(basePath, Classes.Plugins.PluginManagement.ImageResize.ImageSize.original.ToString(), platformLogo.ImageId);

            // get the original size if the requested size doesn't exist
            if (!System.IO.File.Exists(imagePath))
            {
                try
                {
                    string imageDirectory = Path.GetDirectoryName(imagePath);
                    if (!Directory.Exists(imageDirectory))
                    {
                        Directory.CreateDirectory(imageDirectory);
                    }

                    Dictionary<string, string> headers = new Dictionary<string, string>
                        {
                            { "X-Client-API-Key", Config.MetadataConfiguration.HasheousClientAPIKey }
                        };
                    HTTPComms comms = new HTTPComms();
                    var response = await comms.DownloadToFileAsync(new Uri(platformLogo.Url), imagePath, headers);
                }
                catch
                {
                    // if the download fails, return a dummy image
                    return "";
                }
            }

            return imagePath;
        }
    }
}

