using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using gaseous_server.Classes.Metadata;
using gaseous_server.Models;
using gaseous_server.Classes;
using IGDB.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using Asp.Versioning;

namespace gaseous_server.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0", Deprecated = true)]
    [ApiVersion("1.1")]
    [ApiController]
    [Authorize]
    public class PlatformMapsController : Controller
    {
        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [ProducesResponseType(typeof(List<PlatformMapping.PlatformMapItem>), StatusCodes.Status200OK)]
        public ActionResult GetPlatformMap(bool ResetToDefault = false)
        {
            if (ResetToDefault == true)
            {
                PlatformMapping.ExtractPlatformMap(true);
            }

            return Ok(PlatformMapping.PlatformMap);
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("PlatformMap.json")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult DownloadPlatformMap()
        {
            string srcJson = Newtonsoft.Json.JsonConvert.SerializeObject(PlatformMapping.PlatformMap, Newtonsoft.Json.Formatting.Indented);

            string filename = "PlatformMap.json";
            byte[] bytes = Encoding.UTF8.GetBytes(srcJson);
            string contentType = "application/json";

            var cd = new System.Net.Mime.ContentDisposition
            {
                FileName = filename,
                Inline = true,
                DispositionType = "attachment"
            };

            Response.Headers.Add("Content-Disposition", cd.ToString());

            return File(bytes, contentType);
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("{PlatformId}")]
        [ProducesResponseType(typeof(PlatformMapping.PlatformMapItem), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult PlatformMap(long PlatformId)
        {
            try
            {
                PlatformMapping.PlatformMapItem platformMapItem = PlatformMapping.GetPlatformMap(PlatformId);

                if (platformMapItem != null)
                {
                    return Ok(platformMapItem);
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
        [HttpPost]
        [ProducesResponseType(typeof(List<IFormFile>), StatusCodes.Status200OK)]
        [RequestSizeLimit(long.MaxValue)]
        [DisableRequestSizeLimit, RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue, ValueLengthLimit = int.MaxValue)]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UploadPlatformMap(List<IFormFile> files)
        {
            Guid sessionid = Guid.NewGuid();

            string workPath = Path.Combine(Config.LibraryConfiguration.LibraryUploadDirectory, sessionid.ToString());

            long size = files.Sum(f => f.Length);

            List<Dictionary<string, object>> UploadedFiles = new List<Dictionary<string, object>>();

            foreach (IFormFile formFile in files)
            {
                if (formFile.Length > 0)
                {
                    Guid FileId = Guid.NewGuid();

                    string filePath = Path.Combine(workPath, Path.GetFileName(formFile.FileName));

                    if (!Directory.Exists(workPath))
                    {
                        Directory.CreateDirectory(workPath);
                    }

                    using (var stream = System.IO.File.Create(filePath))
                    {
                        await formFile.CopyToAsync(stream);

                        Dictionary<string, object> UploadedFile = new Dictionary<string, object>();
                        UploadedFile.Add("id", FileId.ToString());
                        UploadedFile.Add("originalname", Path.GetFileName(formFile.FileName));
                        UploadedFile.Add("fullpath", filePath);
                        UploadedFiles.Add(UploadedFile);
                    }
                }
            }

            // Process uploaded files
            foreach (Dictionary<string, object> UploadedFile in UploadedFiles)
            {
                Models.PlatformMapping.ExtractPlatformMap((string)UploadedFile["fullpath"]);
            }

            if (Directory.Exists(workPath))
            {
                Directory.Delete(workPath, true);
            }

            return Ok(new { count = files.Count, size });
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpPatch]
        [Route("{PlatformId}")]
        [ProducesResponseType(typeof(PlatformMapping.PlatformMapItem), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Roles = "Admin")]
        public ActionResult EditPlatformMap(long PlatformId, PlatformMapping.PlatformMapItem Map)
        {
            try
            {
                PlatformMapping.PlatformMapItem platformMapItem = PlatformMapping.GetPlatformMap(PlatformId);

                if (platformMapItem != null)
                {
                    PlatformMapping.WritePlatformMap(Map, true, false);
                    return Ok(PlatformMapping.GetPlatformMap(PlatformId));
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