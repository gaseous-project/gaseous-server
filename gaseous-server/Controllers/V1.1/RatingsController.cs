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
    public class RatingsController : ControllerBase
    {
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Authorize]
        [Route("Images/{RatingBoard}/{RatingId}/image.svg")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult RatingsImageById(string RatingBoard, int RatingId)
        {
            IGDB.Models.AgeRatingTitle RatingTitle = (AgeRatingTitle)RatingId;

            string resourceName = "gaseous_server.Assets.Ratings." + RatingBoard + "." + RatingTitle.ToString() + ".svg";

            var assembly = Assembly.GetExecutingAssembly();
            string[] resources = assembly.GetManifestResourceNames();
            if (resources.Contains(resourceName))
            {
                using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
                    if (stream != null)
                    {
                        // Read the stream
                        // and convert it to a byte array
                        // and return it as a file
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            byte[] filedata = new byte[stream.Length];
                            int bytesRead = stream.Read(filedata, 0, filedata.Length);
                            if (bytesRead != filedata.Length)
                            {
                                return StatusCode(StatusCodes.Status500InternalServerError, "Error reading the file data.");
                            }

                            string filename = RatingBoard + "-" + RatingTitle.ToString() + ".svg";
                            string contentType = "image/svg+xml";

                            var cd = new System.Net.Mime.ContentDisposition
                            {
                                FileName = filename,
                                Inline = true,
                            };

                            Response.Headers.Add("Content-Disposition", cd.ToString());
                            Response.Headers.Add("Cache-Control", "public, max-age=604800");

                            return File(filedata, contentType);
                        }
                    }
            }
            return NotFound();
        }
    }
}