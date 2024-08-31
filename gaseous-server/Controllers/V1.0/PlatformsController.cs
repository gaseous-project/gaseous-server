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
using IGDB.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Scripting;
using Asp.Versioning;

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

            string sql = "SELECT * FROM Platform WHERE Id IN (SELECT DISTINCT PlatformId FROM Games_Roms) ORDER BY `Name` ASC;";

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
                IGDB.Models.Platform platformObject = Classes.Metadata.Platforms.GetPlatform(PlatformId);

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
        [Route("{PlatformId}/platformlogo")]
        [ProducesResponseType(typeof(PlatformLogo), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult PlatformLogo(long PlatformId)
        {
            try
            {
                IGDB.Models.Platform platformObject = Classes.Metadata.Platforms.GetPlatform(PlatformId);
                if (platformObject != null)
                {
                    IGDB.Models.PlatformLogo logoObject = PlatformLogos.GetPlatformLogo(platformObject.PlatformLogo.Id, Config.LibraryConfiguration.LibraryMetadataDirectory_Platform(platformObject));
                    if (logoObject != null)
                    {
                        return Ok(logoObject);
                    }
                    else
                    {
                        return NotFound();
                    }
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
        [Route("{PlatformId}/platformlogo/{size}/logo.png")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GameImage(long PlatformId, Communications.IGDBAPI_ImageSize size)
        {
            try
            {
                IGDB.Models.Platform platformObject = Classes.Metadata.Platforms.GetPlatform(PlatformId);
                IGDB.Models.PlatformLogo? logoObject = null;
                try
                {
                    logoObject = PlatformLogos.GetPlatformLogo(platformObject.PlatformLogo.Id, Config.LibraryConfiguration.LibraryMetadataDirectory_Platform(platformObject));
                }
                catch
                {
                    // getting the logo failed, so we'll try a platform variant if available
                    if (platformObject.Versions != null)
                    {
                        if (platformObject.Versions.Ids.Length > 0)
                        {
                            IGDB.Models.PlatformVersion platformVersion = Classes.Metadata.PlatformVersions.GetPlatformVersion(platformObject.Versions.Ids[0], platformObject);
                            logoObject = PlatformLogos.GetPlatformLogo(platformVersion.PlatformLogo.Id, Config.LibraryConfiguration.LibraryMetadataDirectory_Platform(platformObject));
                        }
                        else
                        {
                            return NotFound();
                        }
                    }
                    else
                    {
                        return NotFound();
                    }
                }

                string basePath = Path.Combine(Config.LibraryConfiguration.LibraryMetadataDirectory_Platform(platformObject));
                string imagePath = Path.Combine(basePath, size.ToString(), logoObject.ImageId + ".jpg");

                if (!System.IO.File.Exists(imagePath))
                {
                    Communications comms = new Communications();
                    Task<string> ImgFetch = comms.GetSpecificImageFromServer(Path.Combine(Config.LibraryConfiguration.LibraryMetadataDirectory_Platform(platformObject)), logoObject.ImageId, size, new List<Communications.IGDBAPI_ImageSize> { Communications.IGDBAPI_ImageSize.cover_big, Communications.IGDBAPI_ImageSize.original });

                    imagePath = ImgFetch.Result;
                }

                if (!System.IO.File.Exists(imagePath))
                {
                    Communications comms = new Communications();
                    Task<string> ImgFetch = comms.GetSpecificImageFromServer(basePath, logoObject.ImageId, size, new List<Communications.IGDBAPI_ImageSize> { Communications.IGDBAPI_ImageSize.cover_big, Communications.IGDBAPI_ImageSize.original });

                    imagePath = ImgFetch.Result;
                }

                if (System.IO.File.Exists(imagePath))
                {
                    string filename = logoObject.ImageId + ".jpg";
                    string filepath = imagePath;
                    string contentType = "image/jpg";

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

                return NotFound();
            }
            catch
            {
                return NotFound();
            }
        }
    }
}

