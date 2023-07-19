using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace gaseous_server.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class BiosController : Controller
    {
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public List<Classes.Bios.BiosItem> GetBios()
        {
            return Classes.Bios.GetBios();
        }

        [HttpGet]
        [Route("{PlatformId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public List<Classes.Bios.BiosItem> GetBios(long PlatformId, bool AvailableOnly = true)
        {
            return Classes.Bios.GetBios(PlatformId, AvailableOnly);
        }

        [HttpGet]
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

