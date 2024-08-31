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
                if (path.Contains(".."))
                {
                    return NotFound();
                }

                if (Directory.Exists(path))
                {
                    Dictionary<string, List<Dictionary<string, string>>> allFiles = new Dictionary<string, List<Dictionary<string, string>>>();
                    List<Dictionary<string, string>> directories = new List<Dictionary<string, string>>();
                    string[] dirs = Directory.GetDirectories(path);
                    Array.Sort(dirs);
                    foreach (string dir in dirs)
                    {
                        DirectoryInfo directoryInfo = new DirectoryInfo(dir);
                        directories.Add(new Dictionary<string, string> { { "name", directoryInfo.Name }, { "path", directoryInfo.FullName } });
                    }
                    allFiles.Add("directories", directories);

                    if (showFiles == true)
                    {
                        List<Dictionary<string, string>> files = new List<Dictionary<string, string>>();
                        string[] filePaths = Directory.GetFiles(path);
                        Array.Sort(filePaths);
                        foreach (string file in filePaths)
                        {
                            FileInfo fileInfo = new FileInfo(file);
                            files.Add(new Dictionary<string, string> { { "name", fileInfo.Name }, { "path", fileInfo.FullName } });
                        }
                        allFiles.Add("files", files);
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