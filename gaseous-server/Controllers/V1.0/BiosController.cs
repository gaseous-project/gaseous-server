using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using gaseous_server.Classes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace gaseous_server.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0", Deprecated = true)]
    [ApiVersion("1.1")]
    [Authorize]
    public class BiosController : Controller
    {
        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public List<Classes.Bios.BiosItem> GetBios()
        {
            return Classes.Bios.GetBios();
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("{PlatformId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public List<Classes.Bios.BiosItem> GetBios(long PlatformId, bool AvailableOnly = true)
        {
            return Classes.Bios.GetBios(PlatformId, AvailableOnly);
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpHead]
        [Route("zip/{PlatformId}")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult GetBiosCompressed(long PlatformId)
        {
            try
            {
                IGDB.Models.Platform platform = Classes.Metadata.Platforms.GetPlatform(PlatformId);

                string biosPath = Path.Combine(Config.LibraryConfiguration.LibraryBIOSDirectory, platform.Slug);

                string tempFile = Path.GetTempFileName();

                using (FileStream zipFile = System.IO.File.Create(tempFile))
                using (var zipArchive = new ZipArchive(zipFile, ZipArchiveMode.Create))
                {
                    foreach (string file in Directory.GetFiles(biosPath))
                    {
                        zipArchive.CreateEntryFromFile(file, Path.GetFileName(file));
                    }
                }

                var stream = new FileStream(tempFile, FileMode.Open);
                return File(stream, "application/zip", platform.Slug + ".zip");
            }
            catch
            {
                return NotFound();
            }
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpHead]
        [Route("{PlatformId}/{BiosName}")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult BiosFile(long PlatformId, string BiosName)
        {
            try
            {
                foreach (Classes.Bios.BiosItem biosItem in Classes.Bios.GetBios(PlatformId, true))
                {
                    if (biosItem.filename == BiosName)
                    {
                        if (System.IO.File.Exists(biosItem.biosPath))
                        {
                            string filename = Path.GetFileName(biosItem.biosPath);
                            string filepath = biosItem.biosPath;
                            byte[] filedata = System.IO.File.ReadAllBytes(filepath);
                            string contentType = "application/octet-stream";

                            var cd = new System.Net.Mime.ContentDisposition
                            {
                                FileName = filename,
                                Inline = false,
                            };

                            Response.Headers.Add("Content-Disposition", cd.ToString());
                            Response.Headers.Add("Cache-Control", "public, max-age=604800");

                            return File(filedata, contentType);
                        }
                        else
                        {
                            return NotFound();
                        }
                    }
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

