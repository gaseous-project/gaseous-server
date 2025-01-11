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
using HasheousClient.Models.Metadata.IGDB;

namespace gaseous_server.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
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

        public static List<Platform> GetPlatforms()
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);

            string sql = "SELECT * FROM Platform WHERE Id IN (SELECT DISTINCT PlatformId FROM view_Games_Roms) ORDER BY `Name` ASC;";

            List<Platform> RetVal = new List<Platform>();

            DataTable dbResponse = db.ExecuteCMD(sql);
            foreach (DataRow dr in dbResponse.Rows)
            {
                RetVal.Add(Classes.Metadata.Platforms.GetPlatform((long)dr["id"]));
            }

            return RetVal;
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("{PlatformId}")]
        [ProducesResponseType(typeof(Platform), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult Platform(long PlatformId)
        {
            try
            {
                Platform platformObject = Classes.Metadata.Platforms.GetPlatform(PlatformId);

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

        // [MapToApiVersion("1.0")]
        // [MapToApiVersion("1.1")]
        // [HttpGet]
        // [Route("{PlatformId}/platformlogo")]
        // [ProducesResponseType(typeof(PlatformLogo), StatusCodes.Status200OK)]
        // [ProducesResponseType(StatusCodes.Status404NotFound)]
        // public ActionResult PlatformLogo(long PlatformId)
        // {
        //     try
        //     {
        //         Platform platformObject = Classes.Metadata.Platforms.GetPlatform(PlatformId);
        //         if (platformObject != null)
        //         {
        //             PlatformLogo logoObjectParent = (PlatformLogo)platformObject.PlatformLogo;
        //             PlatformLogo logoObject = PlatformLogos.GetPlatformLogo(logoObjectParent.Id);
        //             if (logoObject != null)
        //             {
        //                 return Ok(logoObject);
        //             }
        //             else
        //             {
        //                 return NotFound();
        //             }
        //         }
        //         else
        //         {
        //             return NotFound();
        //         }
        //     }
        //     catch
        //     {
        //         return NotFound();
        //     }
        // }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("{PlatformId}/platformlogo/{size}/")]
        [Route("{PlatformId}/platformlogo/{size}/logo.png")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GameImage(long PlatformId, Communications.IGDBAPI_ImageSize size)
        {
            try
            {
                HasheousClient.Models.MetadataSources metadataSources = HasheousClient.Models.MetadataSources.None;

                Platform platformObject = Classes.Metadata.Platforms.GetPlatform(PlatformId, metadataSources);
                PlatformLogo? logoObject = null;

                logoObject = PlatformLogos.GetPlatformLogo((long)platformObject.PlatformLogo, metadataSources);

                if (logoObject == null)
                {
                    // getting the logo failed, so we'll try a platform variant if available
                    if (platformObject.Versions != null)
                    {
                        if (platformObject.Versions.Count > 0)
                        {
                            PlatformVersion platformVersion = Classes.Metadata.PlatformVersions.GetPlatformVersion(metadataSources, (long)platformObject.Versions[0]);
                            logoObject = PlatformLogos.GetPlatformLogo((long)platformVersion.PlatformLogo);
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

                string basePath = Path.Combine(Config.LibraryConfiguration.LibraryMetadataDirectory_Platform(platformObject), metadataSources.ToString());
                string imagePath = Path.Combine(basePath, size.ToString(), logoObject.ImageId);

                if (!System.IO.File.Exists(imagePath))
                {
                    Communications comms = new Communications();
                    Task<string> ImgFetch = comms.GetSpecificImageFromServer(metadataSources, Path.Combine(Config.LibraryConfiguration.LibraryMetadataDirectory_Platform(platformObject)), logoObject.ImageId, size, new List<Communications.IGDBAPI_ImageSize> { Communications.IGDBAPI_ImageSize.cover_big, Communications.IGDBAPI_ImageSize.original });

                    imagePath = ImgFetch.Result;
                }

                if (!System.IO.File.Exists(imagePath))
                {
                    Communications comms = new Communications();
                    Task<string> ImgFetch = comms.GetSpecificImageFromServer(metadataSources, basePath, logoObject.ImageId, size, new List<Communications.IGDBAPI_ImageSize> { Communications.IGDBAPI_ImageSize.cover_big, Communications.IGDBAPI_ImageSize.original });

                    imagePath = ImgFetch.Result;
                }

                if (System.IO.File.Exists(imagePath))
                {
                    // get image info
                    var info = new ImageMagick.MagickImageInfo(imagePath);
                    string extension = ".jpg";
                    string mimeType = "image/jpg";
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

                    Response.Headers.Add("Content-Disposition", cd.ToString());
                    Response.Headers.Add("Cache-Control", "public, max-age=604800");

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
            catch
            {
                return NotFound();
            }
        }

        private ActionResult GetDummyImage()
        {
            // return resource named DefaultPlatformLogo.svg
            var assembly = Assembly.GetExecutingAssembly();
            string resourceName = "gaseous_server.Support.DefaultPlatformLogo.svg";
            string[] resources = Assembly.GetExecutingAssembly().GetManifestResourceNames();
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

