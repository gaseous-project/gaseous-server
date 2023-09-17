using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using gaseous_server.Classes.Metadata;
using gaseous_server.Models;
using gaseous_tools;
using IGDB.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Scripting;

namespace gaseous_server.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class PlatformsController : Controller
    {
        [HttpGet]
        [ProducesResponseType(typeof(List<Platform>), StatusCodes.Status200OK)]
        public ActionResult Platform()
        {
            return Ok(PlatformsController.GetPlatforms());
        }

        public static List<Platform> GetPlatforms()
        {
            Database db = new gaseous_tools.Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);

            string sql = "SELECT * FROM gaseous.Platform WHERE Id IN (SELECT DISTINCT PlatformId FROM Games_Roms) ORDER BY `Name` ASC;";

            List<Platform> RetVal = new List<Platform>();

            DataTable dbResponse = db.ExecuteCMD(sql);
            foreach (DataRow dr in dbResponse.Rows)
            {
                RetVal.Add(Classes.Metadata.Platforms.GetPlatform((long)dr["id"]));
            }

            return RetVal;
        }

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

        [HttpGet]
        [Route("{PlatformId}/platformlogo/image")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult PlatformLogoImage(long PlatformId)
        {
            try
            {
                IGDB.Models.Platform platformObject = Classes.Metadata.Platforms.GetPlatform(PlatformId);

                string logoFilePath = Path.Combine(Config.LibraryConfiguration.LibraryMetadataDirectory_Platform(platformObject), "Logo_Medium.png");
                if (System.IO.File.Exists(logoFilePath))
                {
                    string filename = "Logo.png";
                    string filepath = logoFilePath;
                    byte[] filedata = System.IO.File.ReadAllBytes(filepath);
                    string contentType = "image/png";

                    var cd = new System.Net.Mime.ContentDisposition
                    {
                        FileName = filename,
                        Inline = true,
                    };

                    Response.Headers.Add("Content-Disposition", cd.ToString());

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
    }
}

