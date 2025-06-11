using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace gaseous_server.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0", Deprecated = true)]
    [ApiVersion("1.1")]
    [Authorize(Roles = "Admin")]
    public class LibraryController : Controller
    {
        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [ProducesResponseType(typeof(List<GameLibrary.LibraryItem>), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetLibrariesAsync(bool GetStorageInfo = false)
        {
            return Ok(await GameLibrary.GetLibraries(GetStorageInfo));
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet("{LibraryId}")]
        [ProducesResponseType(typeof(GameLibrary.LibraryItem), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetLibrary(int LibraryId, bool GetStorageInfo = false)
        {
            try
            {
                return Ok(await GameLibrary.GetLibrary(LibraryId, GetStorageInfo));
            }
            catch
            {
                return NotFound();
            }
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpPost]
        [ProducesResponseType(typeof(GameLibrary.LibraryItem), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> AddLibraryAsync(string Name, string Path, long DefaultPlatformId)
        {
            try
            {
                return Ok(await GameLibrary.AddLibrary(Name, Path, DefaultPlatformId));
            }
            catch (GameLibrary.PathExists exPE)
            {
                return Conflict("Path already used in another library");
            }
            catch (GameLibrary.PathNotFound exPNF)
            {
                return NotFound("Path not found");
            }
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpDelete("{LibraryId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DelLibrary(int LibraryId)
        {
            try
            {
                await GameLibrary.DeleteLibrary(LibraryId);
                return Ok();
            }
            catch (GameLibrary.CannotDeleteDefaultLibrary exCDDL)
            {
                return BadRequest(exCDDL.ToString());
            }
            catch (GameLibrary.LibraryNotFound exLNF)
            {
                return NotFound(exLNF.ToString());
            }
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpPost("{LibraryId}/Scan")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> ScanLibrary(int LibraryId)
        {
            try
            {
                await GameLibrary.ScanLibrary(LibraryId);
                return Ok();
            }
            catch (GameLibrary.LibraryNotFound exLNF)
            {
                return NotFound(exLNF.ToString());
            }
        }
    }
}