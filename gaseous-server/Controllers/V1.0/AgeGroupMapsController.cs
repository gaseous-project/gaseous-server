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
    public class AgeGroupMapsController : Controller
    {
        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("AgeGroupMap.json")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult DownloadAgeGroupMap()
        {
            try
            {
                string srcJson = Newtonsoft.Json.JsonConvert.SerializeObject(AgeGroups.AgeGroupMap, Newtonsoft.Json.Formatting.Indented);

                string filename = "AgeGroupMap.json";
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
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }
    }
}
