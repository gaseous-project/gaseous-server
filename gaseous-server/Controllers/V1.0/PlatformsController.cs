using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using gaseous_server.Classes;
using gaseous_server.Classes.Metadata;
using gaseous_server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Scripting;
using Asp.Versioning;
using gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes;

namespace gaseous_server.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0", Deprecated = true)]
    [ApiVersion("1.1")]
    [Authorize]
    [ApiController]
    public class PlatformsController : Controller
    {
        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [ProducesResponseType(typeof(List<Platform>), StatusCodes.Status200OK)]
        public ActionResult Platform()
        {
            return Ok(PlatformsController.GetPlatforms());
        }

        public static async Task<List<Platform>> GetPlatforms()
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);

            string sql = "SELECT * FROM Platform WHERE Id IN (SELECT DISTINCT PlatformId FROM view_Games_Roms) ORDER BY `Name` ASC;";

            List<Platform> RetVal = new List<Platform>();

            DataTable dbResponse = await db.ExecuteCMDAsync(sql);
            foreach (DataRow dr in dbResponse.Rows)
            {
                RetVal.Add(await Classes.Metadata.Platforms.GetPlatform((long)dr["id"]));
            }

            return RetVal;
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("{PlatformId}")]
        [ProducesResponseType(typeof(Platform), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Platform(long PlatformId)
        {
            try
            {
                Platform platformObject = await Classes.Metadata.Platforms.GetPlatform(PlatformId);

                if (platformObject != null)
                {
                    return Ok(platformObject);
                }
                else
                {
                    return NotFound();
                }
            }
            catch
            {
                return NotFound();
            }
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("{PlatformId}/platformlogo/{size}/")]
        [Route("{PlatformId}/platformlogo/{size}/logo.png")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GameImage(long PlatformId, Classes.Plugins.PluginManagement.ImageResize.ImageSize size)
        {
            try
            {
                FileSignature.MetadataSources metadataSources = FileSignature.MetadataSources.None;

                Platform platformObject = await Classes.Metadata.Platforms.GetPlatform(PlatformId, metadataSources);
                PlatformLogo? logoObject = null;

                logoObject = await PlatformLogos.GetPlatformLogo((long)platformObject.PlatformLogo, metadataSources);

                if (logoObject == null || logoObject.ImageId == null || logoObject.ImageId == "")
                {
                    // getting the logo failed, so we'll try a platform variant if available
                    if (platformObject.Versions != null)
                    {
                        if (platformObject.Versions.Count > 0)
                        {
                            PlatformVersion platformVersion = await Classes.Metadata.PlatformVersions.GetPlatformVersion(metadataSources, (long)platformObject.Versions[0]);
                            if (platformVersion == null)
                            {
                                return GetDummyImage();
                            }
                            logoObject = await PlatformLogos.GetPlatformLogo((long)platformVersion.PlatformLogo);

                            if (logoObject == null || logoObject.ImageId == null || logoObject.ImageId == "")
                            {
                                // no image found, return a dummy image
                                return GetDummyImage();
                            }
                        }
                        else
                        {
                            return GetDummyImage();
                        }
                    }
                    else
                    {
                        return GetDummyImage();
                    }
                }

                // get the original image path (this will download the image if it doesn't exist) - we'll resize it later
                string imagePath = await PlatformLogos.GetPlatformLogoImage(platformObject, logoObject, metadataSources);

                if (String.IsNullOrEmpty(imagePath))
                {
                    // no image found, return a dummy image
                    return GetDummyImage();
                }

                if (System.IO.File.Exists(imagePath))
                {
                    string extension = ".jpg";
                    string mimeType = "image/jpg";

                    // Check if file is SVG (text-based format) by reading first few bytes
                    bool isSvg = false;
                    try
                    {
                        byte[] headerBytes = new byte[512];
                        using (FileStream fs = System.IO.File.OpenRead(imagePath))
                        {
                            int bytesRead = fs.Read(headerBytes, 0, Math.Min(512, (int)fs.Length));
                            string headerText = System.Text.Encoding.UTF8.GetString(headerBytes, 0, bytesRead).ToLower();
                            isSvg = headerText.Contains("<?xml") || headerText.Contains("<svg");

                            if (isSvg)
                            {
                                // SVG files are text-based and cannot be resized - return immediately
                                string svgfilename = logoObject.ImageId + ".svg";
                                var svgcd = new System.Net.Mime.ContentDisposition
                                {
                                    FileName = svgfilename,
                                    Inline = true,
                                };

                                Response.Headers.Append("Content-Disposition", svgcd.ToString());
                                Response.Headers.Append("Cache-Control", "public, max-age=604800");

                                byte[] svgfiledata = null;
                                using (FileStream svgfs = System.IO.File.OpenRead(imagePath))
                                {
                                    using (BinaryReader binaryReader = new BinaryReader(svgfs))
                                    {
                                        svgfiledata = binaryReader.ReadBytes((int)svgfs.Length);
                                    }
                                }

                                return File(svgfiledata, "image/svg+xml");
                            }
                        }
                    }
                    catch
                    {
                        // If unable to check, assume it's not SVG and continue with normal processing
                    }

                    // get image info from binary files
                    var info = new ImageMagick.MagickImageInfo(imagePath);
                    switch (info.Format)
                    {
                        case ImageMagick.MagickFormat.Jpeg:
                            extension = ".jpg";
                            mimeType = "image/jpg";
                            break;

                        case ImageMagick.MagickFormat.Png:
                            extension = ".png";
                            mimeType = "image/png";
                            break;

                        case ImageMagick.MagickFormat.Gif:
                            extension = ".gif";
                            mimeType = "image/gif";
                            break;

                        case ImageMagick.MagickFormat.Bmp:
                            extension = ".bmp";
                            mimeType = "image/bmp";
                            break;

                        case ImageMagick.MagickFormat.Tiff:
                            extension = ".tiff";
                            mimeType = "image/tiff";
                            break;

                        case ImageMagick.MagickFormat.Unknown:
                            extension = ".jpg";
                            mimeType = "image/jpg";
                            break;

                        case ImageMagick.MagickFormat.WebP:
                            extension = ".webp";
                            mimeType = "image/webp";
                            break;

                        case ImageMagick.MagickFormat.Heic:
                            extension = ".heic";
                            mimeType = "image/heic";
                            break;

                        case ImageMagick.MagickFormat.Heif:
                            extension = ".heif";
                            mimeType = "image/heif";
                            break;

                        case ImageMagick.MagickFormat.Svg:
                            extension = ".svg";
                            mimeType = "image/svg+xml";
                            break;

                        default:
                            extension = ".jpg";
                            mimeType = "image/jpg";
                            break;
                    }

                    string filename = logoObject.ImageId + extension;
                    string filepath = imagePath;
                    string contentType = mimeType;

                    var cd = new System.Net.Mime.ContentDisposition
                    {
                        FileName = filename,
                        Inline = true,
                    };

                    Response.Headers.Append("Content-Disposition", cd.ToString());
                    Response.Headers.Append("Cache-Control", "public, max-age=604800");

                    // TODO: Resize image according to size parameter
                    byte[] filedata = null;
                    using (FileStream fs = System.IO.File.OpenRead(filepath))
                    {
                        using (BinaryReader binaryReader = new BinaryReader(fs))
                        {
                            filedata = binaryReader.ReadBytes((int)fs.Length);
                        }
                    }

                    return File(filedata, contentType);
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                Logging.LogKey(Logging.LogType.Critical, "PlatformsController", $"An error occurred while trying to get the platform logo for platform ID {PlatformId}: {ex.Message}", null, null, ex);
                return NotFound();
            }
        }

        private ActionResult GetDummyImage()
        {
            // return resource named DefaultPlatformLogo.svg
            var assembly = Assembly.Load("gaseous-lib");
            string resourceName = "gaseous_lib.Support.DefaultPlatformLogo.svg";
            string[] resources = assembly.GetManifestResourceNames();
            if (resources.Contains(resourceName))
            {
                string svgData = "";
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    svgData = reader.ReadToEnd();
                }

                var cd = new System.Net.Mime.ContentDisposition
                {
                    FileName = "DefaultPlatformLogo.svg",
                    Inline = true,
                };

                Response.Headers.Add("Content-Disposition", cd.ToString());
                Response.Headers.Add("Cache-Control", "public, max-age=604800");

                byte[] filedata = null;
                using (MemoryStream ms = new MemoryStream())
                {
                    using (StreamWriter writer = new StreamWriter(ms))
                    {
                        writer.Write(svgData);
                        writer.Flush();
                        ms.Position = 0;
                        filedata = ms.ToArray();
                    }
                }

                return File(filedata, "image/svg+xml");
            }
            else
            {
                return NotFound();
            }
        }
    }
}

