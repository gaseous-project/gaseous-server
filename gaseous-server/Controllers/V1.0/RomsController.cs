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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Scripting;
using static gaseous_server.Classes.Metadata.AgeRatings;
using Asp.Versioning;
using HasheousClient.Models.Metadata.IGDB;
using Microsoft.AspNetCore.Identity;
using Authentication;

namespace gaseous_server.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0", Deprecated = true)]
    [ApiVersion("1.1")]
    [Authorize]
    [ApiController]
    public class RomsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public RomsController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager
        )
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }



        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpPost]
        [Authorize(Roles = "Admin,Gamer")]
        [ProducesResponseType(typeof(List<IFormFile>), StatusCodes.Status200OK)]
        [RequestSizeLimit(long.MaxValue)]
        [Consumes("multipart/form-data")]
        [DisableRequestSizeLimit, RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue, ValueLengthLimit = int.MaxValue)]
        /// <summary>
        /// Uploads a ROM file to the server.
        /// </summary>
        /// <param name="file">The ROM file to upload.</param>
        /// <param name="OverridePlatformId">Optional platform ID to override the default platform.</param>
        /// <param name="SessionId">Optional session ID for tracking the upload.</param>
        /// <returns>A session ID for tracking the upload.</returns>
        /// <remarks>
        /// This endpoint allows users to upload ROM files to the server. The uploaded file is saved in a temporary directory, and the platform override can be specified if needed.
        /// </remarks>
        /// <response code="200">File uploaded successfully.</response>
        /// <response code="400">Bad request if the file is empty.</response>
        /// <response code="500">Internal server error if the file upload fails.</response>
        public async Task<IActionResult> UploadRom(IFormFile file, long? OverridePlatformId = null, string SessionId = null)
        {
            Guid sessionid = Guid.NewGuid();
            if (SessionId != null)
            {
                sessionid = Guid.Parse(SessionId);
            }
            // Create a unique directory for the uploaded file
            string workPath = Path.Combine(Config.LibraryConfiguration.LibraryUploadDirectory, sessionid.ToString());

            // Check if the file is not empty
            if (file == null || file.Length == 0)
            {
                return BadRequest("File is empty.");
            }

            if (file.Length > 0)
            {
                Guid FileId = Guid.NewGuid();

                string filePath = Path.Combine(workPath, Path.GetFileName(file.FileName));

                if (!Directory.Exists(workPath))
                {
                    Directory.CreateDirectory(workPath);
                }

                Dictionary<string, object> UploadedFile = new Dictionary<string, object>();

                using (var stream = System.IO.File.Create(filePath))
                {
                    await file.CopyToAsync(stream);

                    UploadedFile.Add("id", FileId.ToString());
                    UploadedFile.Add("originalname", Path.GetFileName(file.FileName));
                    UploadedFile.Add("fullpath", filePath);
                }

                // store the platform override if provided
                if (OverridePlatformId != null)
                {
                    await System.IO.File.WriteAllTextAsync(Path.Combine(workPath, ".platformoverride"), OverridePlatformId.ToString());
                }

                // get the user id
                var user = await _userManager.GetUserAsync(User);

                // create an import state item
                ImportGame.AddImportState(sessionid, filePath, Models.ImportStateItem.ImportMethod.WebUpload, user.Id, OverridePlatformId);

                return Ok(sessionid.ToString());
            }

            return Ok(sessionid.ToString());
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpPost]
        [Authorize]
        [Route("Imports")]
        [ProducesResponseType(typeof(List<Models.ImportStateItem>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Gets the import state for the current user.
        /// </summary>
        /// <param name="itemStatuses">Optional list of import state items to filter by.</param>
        /// <returns>A list of import state items for the current user.</returns>
        /// <remarks>
        /// This endpoint retrieves the import state for the current user. It returns a list of import state items, including their status and other relevant information.
        /// </remarks>
        /// <response code="200">Import state retrieved successfully.</response>
        /// <response code="400">Bad request if the user is not authenticated.</response>
        /// <response code="401">Unauthorized if the user is not authenticated.</response>
        /// <response code="404">Not found if no import state items are found.</response>
        /// <response code="500">Internal server error if the import state retrieval fails.</response>
        /// <example>
        /// {
        ///  "itemStatuses": [
        ///   "Pending",
        ///  "Queued",
        ///  "Processing",
        ///  "Completed",
        ///  "Skipped",
        ///  "Failed"
        ///  ]
        /// }
        /// </example>
        public async Task<IActionResult> GetImportState(List<Models.ImportStateItem.ImportState>? itemStatuses)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return NotFound();
            }

            List<Models.ImportStateItem> importState = ImportGame.ImportStates
                    .Where(x => x.UserId == user.Id)
                    .ToList();

            if (itemStatuses != null && itemStatuses.Count > 0)
            {
                // filter out any import states that are not in the itemStatuses list
                importState = ImportGame.ImportStates
                    .Where(x => itemStatuses.Contains(x.State))
                    .ToList();
            }

            if (importState == null || importState.Count == 0)
            {
                return Ok(new List<Models.ImportStateItem>());
            }

            return Ok(importState);
        }
    }
}