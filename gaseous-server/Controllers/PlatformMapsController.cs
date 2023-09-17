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
    public class PlatformMapsController : Controller
    {
        [HttpGet]
        [ProducesResponseType(typeof(List<PlatformMapping.PlatformMapItem>), StatusCodes.Status200OK)]
        public ActionResult GetPlatformMap()
        {
            return Ok(PlatformMapping.PlatformMap);
        }

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

        [HttpPost]
        [Route("{PlatformId}")]
        [ProducesResponseType(typeof(PlatformMapping.PlatformMapItem), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public ActionResult NewPlatformMap(long PlatformId, PlatformMapping.PlatformMapItem Map)
        {
            try
            {
                PlatformMapping.PlatformMapItem platformMapItem = PlatformMapping.GetPlatformMap(PlatformId);

                if (platformMapItem != null)
                {
                    return Conflict();
                }
                else
                {
                    PlatformMapping.WritePlatformMap(Map, false);
                    return Ok(PlatformMapping.GetPlatformMap(PlatformId));
                }
            }
            catch
            {
                return NotFound();
            }
        }

        [HttpPatch]
        [Route("{PlatformId}")]
        [ProducesResponseType(typeof(PlatformMapping.PlatformMapItem), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult EditPlatformMap(long PlatformId, PlatformMapping.PlatformMapItem Map)
        {
            try
            {
                PlatformMapping.PlatformMapItem platformMapItem = PlatformMapping.GetPlatformMap(PlatformId);

                if (platformMapItem != null)
                {
                    PlatformMapping.WritePlatformMap(Map, true);
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