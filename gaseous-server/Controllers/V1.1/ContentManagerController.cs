using System.ComponentModel.DataAnnotations;
using Asp.Versioning;
using Authentication;
using gaseous_server.Classes;
using gaseous_server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using static gaseous_server.Classes.Content.ContentManager;

namespace gaseous_server.Controllers.v1_1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.1")]
    [ApiController]
    public class ContentManagerController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public ContentManagerController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager
        )
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        /// <summary>
        /// Receives a content upload
        /// </summary>
        /// <returns></returns>
        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpPost]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("{metadataid}")]
        public async Task<ActionResult> UploadContentAsync(ContentModel model, long metadataid)
        {
            // get user
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found");
            }

            if (model == null || model.ByteArrayBase64.Length == 0)
            {
                return BadRequest("Invalid file data");
            }

            // store the content
            try
            {
                long attachmentId = await AddMetadataItemContent(metadataid, model, user);
                return Ok(new { Message = $"{model.ContentType.ToString()} uploaded successfully", AttachmentId = attachmentId });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Retrieves a list of content attachments associated with the specified metadata item ID that the user has access to.
        /// </summary>
        /// <param name="metadataid">The metadata item ID to retrieve content for.</param>
        /// <param name="contentTypes">Comma-separated list of content types to filter by; if empty, all types are returned.</param>
        /// <returns>List of ContentViewModel representing the accessible content attachments.</returns>
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("{metadataid}")]
        public async Task<ActionResult> GetContentsAsync(long metadataid, [Required] string contentTypes)
        {
            // get user
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found");
            }

            List<ContentType>? contentTypeList = null;
            if (!string.IsNullOrEmpty(contentTypes))
            {
                contentTypeList = new List<ContentType>();
                foreach (var ct in contentTypes.Split(','))
                {
                    if (Enum.TryParse<ContentType>(ct, true, out var parsedCt))
                    {
                        contentTypeList.Add(parsedCt);
                    }
                }
            }

            try
            {
                var contents = await GetMetadataItemContents(new List<long> { metadataid }, user, contentTypeList);
                return Ok(contents);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Retrieves a specific content attachment by its ID if the user has access to it.
        /// </summary>
        /// <param name="metadataid">The metadata item ID the attachment is associated with (for routing purposes).</param>
        /// <param name="attachmentId">The ID of the content attachment to retrieve.</param>
        /// <returns>The ContentViewModel representing the content attachment.</returns>
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("{metadataid}/{attachmentId}")]
        public async Task<ActionResult> GetContentAsync(long metadataid, long attachmentId)
        {
            // get user
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found");
            }

            try
            {
                var content = await GetMetadataItemContent(attachmentId, user);
                return Ok(content);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Deletes a specific content attachment by its ID if the user has permission to delete it.
        /// </summary>
        /// <param name="metadataid">The metadata item ID the attachment is associated with (for routing purposes).</param>
        /// <param name="attachmentId">The ID of the content attachment to delete.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [MapToApiVersion("1.1")]
        [HttpDelete]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("{metadataid}/{attachmentId}")]
        public async Task<ActionResult> DeleteContentAsync(long metadataid, long attachmentId)
        {
            // get user
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found");
            }

            try
            {
                await DeleteMetadataItemContent(attachmentId, user);
                return Ok(new { Message = "Content deleted successfully" });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Updates properties of a specific content attachment if the user has permission to update it.
        /// </summary>
        /// <param name="metadataid">The metadata item ID the attachment is associated with (for routing purposes).</param>
        /// <param name="attachmentId">The ID of the content attachment to update.</param>
        /// <param name="model">The update model containing properties to update.</param>
        /// <returns>The updated ContentViewModel representing the content attachment.</returns>
        [MapToApiVersion("1.1")]
        [HttpPatch]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("{metadataid}/{attachmentId}")]
        public async Task<ActionResult> UpdateContentAsync(long metadataid, long attachmentId, [FromBody] ContentUpdateModel model)
        {
            // get user
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found");
            }

            try
            {
                var updatedContent = await UpdateMetadataItem(attachmentId, user, model.IsShared, model.Content);
                return Ok(updatedContent);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}