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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Scripting;
using static gaseous_server.Classes.Metadata.AgeRatings;
using Asp.Versioning;

namespace gaseous_server.Controllers.v1_1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.1")]
    [ApiController]
    public class FileSystemController : ControllerBase
    {
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult GetFileSystem(string path, bool showFiles = false)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    string[] directories = Directory.GetDirectories(path);
                    Array.Sort(directories);
                    string[] files = Directory.GetFiles(path);
                    Array.Sort(files);

                    Dictionary<string, List<string>> allFiles = new Dictionary<string, List<string>>
                    {
                        { "directories", directories.ToList() }
                    };

                    if (showFiles)
                    {
                        allFiles.Add("files", files.ToList());
                    }
                    return Ok(allFiles);
                }
                else
                {
                    return NotFound();

                }
            }
            catch (Exception ex)
            {
                return NotFound();
            }
        }
    }
}