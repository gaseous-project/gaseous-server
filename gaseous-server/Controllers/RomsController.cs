using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using gaseous_server.Classes;
using gaseous_server.Classes.Metadata;
using IGDB.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Scripting;
using static gaseous_server.Classes.Metadata.AgeRatings;

namespace gaseous_server.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class RomsController : ControllerBase
    {
        [HttpPost]
        [ProducesResponseType(typeof(List<IFormFile>), StatusCodes.Status200OK)]
        [RequestSizeLimit(long.MaxValue)]
        [DisableRequestSizeLimit, RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue, ValueLengthLimit = int.MaxValue)]
        public async Task<IActionResult> UploadRom(List<IFormFile> files, long? OverridePlatformId = null)
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

            // get override platform if specified
            IGDB.Models.Platform? OverridePlatform = null;
            if (OverridePlatformId != null)
            {
                OverridePlatform = Platforms.GetPlatform((long)OverridePlatformId);
            }

            // Process uploaded files
            foreach (Dictionary<string, object> UploadedFile in UploadedFiles)
            {
                Classes.ImportGame.ImportGameFile((string)UploadedFile["fullpath"], OverridePlatform);
            }

            if (Directory.Exists(workPath))
            {
                Directory.Delete(workPath, true);
            }

            return Ok(new { count = files.Count, size });
        }
    }
}