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
        private const string UserNotFoundMessage = "User not found";
        private const long MaxUploadBytes = 50 * 1024 * 1024; // 50 MB overall per file

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentManagerController"/> class.
        /// </summary>
        /// <param name="userManager">ASP.NET Identity user manager.</param>
        /// <param name="signInManager">ASP.NET Identity sign-in manager.</param>
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
        [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
        [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
        [Route("{metadataid}")]
        public async Task<ActionResult> UploadContentAsync(ContentModel model, long metadataid)
        {
            // get user
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound(UserNotFoundMessage);

            if (model == null || model.ByteArrayBase64.Length == 0)
            {
                return BadRequest("Invalid file data");
            }

            byte[] rawBytes;
            try
            {
                rawBytes = model.ByteArray; // decode base64
            }
            catch (FormatException)
            {
                return BadRequest("Invalid base64 content");
            }
            if (rawBytes.LongLength > MaxUploadBytes)
            {
                return StatusCode(StatusCodes.Status413PayloadTooLarge, $"File exceeds maximum size of {MaxUploadBytes / (1024 * 1024)} MB");
            }

            // store the content
            try
            {
                long attachmentId = await AddMetadataItemContent(metadataid, model, user);
                return Ok(new { Message = $"{model.ContentType.ToString()} uploaded successfully", AttachmentId = attachmentId });
            }
            catch (InvalidOperationException ex)
            {
                // map validation failures to 415 if they originate from content validation
                if (ex.Message.StartsWith("Content validation failed", StringComparison.OrdinalIgnoreCase))
                {
                    return StatusCode(StatusCodes.Status415UnsupportedMediaType, ex.Message);
                }
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Receives a content upload via multipart/form-data (raw file). Retains existing base64 JSON upload method.
        /// </summary>
        /// <remarks>
        /// Form fields:
        ///  - file: (IFormFile) The binary file to upload (required)
        ///  - contentType: (enum) The ContentType value (required) e.g. Screenshot, Video, GlobalManual, etc.
        ///  - description: (string) Optional description
        /// </remarks>
        /// <param name="metadataid">Metadata item id the content is associated with.</param>
        /// <param name="file">The uploaded file.</param>
        /// <param name="contentType">The logical content type classification.</param>
        /// <param name="description">Optional description.</param>
        /// <returns>Attachment id and success message.</returns>
        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpPost]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
        [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
        [Route("{metadataid}/fileupload")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult> UploadContentFileAsync(long metadataid, [FromForm] SingleContentUploadForm form)
        {
            // get user
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound(UserNotFoundMessage);

            if (form == null || form.File == null || form.File.Length == 0)
            {
                return BadRequest("Invalid file data");
            }
            if (form.File.Length > MaxUploadBytes)
            {
                return StatusCode(StatusCodes.Status413PayloadTooLarge, $"File exceeds maximum size of {MaxUploadBytes / (1024 * 1024)} MB");
            }

            // read file into memory
            byte[] data;
            using (var ms = new MemoryStream())
            {
                await form.File.CopyToAsync(ms);
                data = ms.ToArray();
            }

            var model = new ContentModel
            {
                ContentType = form.ContentType,
                ByteArrayBase64 = Convert.ToBase64String(data),
                Filename = form.File.FileName,
                Description = form.Description
            };

            try
            {
                long attachmentId = await AddMetadataItemContent(metadataid, model, user);
                return Ok(new { Message = $"{model.ContentType.ToString()} uploaded successfully", AttachmentId = attachmentId });
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.StartsWith("Content validation failed", StringComparison.OrdinalIgnoreCase))
                {
                    return StatusCode(StatusCodes.Status415UnsupportedMediaType, ex.Message);
                }
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Receives multiple content uploads (same logical content type) via multipart/form-data.
        /// </summary>
        /// <remarks>
        /// Form fields:
        ///  - files: (IFormFile[]) One or more files (required)
        ///  - contentType: (enum) The ContentType value applied to all files (required)
        ///  - description: (string) Optional description applied to all files
        /// Returns an array of results with original filename and new attachment id.
        /// </remarks>
        [MapToApiVersion("1.1")]
        [HttpPost]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
        [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
        [Route("{metadataid}/fileupload/multiple")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult> UploadMultipleContentFilesAsync(long metadataid, [FromForm] MultiContentUploadForm form)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound(UserNotFoundMessage);
            if (form == null || form.Files == null || form.Files.Count == 0) return BadRequest("No files provided");

            var results = new List<object>();
            foreach (var file in form.Files)
            {
                if (file.Length == 0) return BadRequest($"File '{file.FileName}' is empty");
                if (file.Length > MaxUploadBytes)
                {
                    return StatusCode(StatusCodes.Status413PayloadTooLarge, $"File '{file.FileName}' exceeds maximum size of {MaxUploadBytes / (1024 * 1024)} MB");
                }
                byte[] data;
                using (var ms = new MemoryStream())
                {
                    await file.CopyToAsync(ms);
                    data = ms.ToArray();
                }
                var model = new ContentModel
                {
                    ContentType = form.ContentType,
                    ByteArrayBase64 = Convert.ToBase64String(data),
                    Filename = file.FileName,
                    Description = form.Description
                };
                try
                {
                    long attachmentId = await AddMetadataItemContent(metadataid, model, user);
                    results.Add(new { File = file.FileName, AttachmentId = attachmentId, ContentType = model.ContentType.ToString() });
                }
                catch (InvalidOperationException ex)
                {
                    if (ex.Message.StartsWith("Content validation failed", StringComparison.OrdinalIgnoreCase))
                    {
                        return StatusCode(StatusCodes.Status415UnsupportedMediaType, $"File '{file.FileName}': {ex.Message}");
                    }
                    return BadRequest($"File '{file.FileName}' failed: {ex.Message}");
                }
            }
            return Ok(results);
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
            if (user == null) return NotFound(UserNotFoundMessage);

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
        /// Retrieves the raw binary data of a specific content attachment by its ID if the user has access to it.
        /// </summary>
        /// <param name="metadatadaId">The metadata item ID the attachment is associated with (for routing purposes).</param>
        /// <param name="attachmentId">The ID of the content attachment to retrieve.</param>
        /// <returns>The raw binary data of the content attachment.</returns>
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("{metadataid}/{attachmentId}/data")]
        public async Task<ActionResult> GetContentDataAsync(long metadataid, long attachmentId)
        {
            // get user
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound(UserNotFoundMessage);

            try
            {
                // get the metadata item content metadata
                var contentMeta = await GetMetadataItemContent(attachmentId, user);
                if (contentMeta == null) return NotFound("Content not found");

                // return the raw data
                Response.Headers["Content-Disposition"] = $"attachment; filename=\"{contentMeta.FileName}{contentMeta.FileExtension}\"";
                Response.Headers["Content-Length"] = contentMeta.Size.ToString();

                var content = await GetMetadataItemContentData(attachmentId, user);
                return File(content, contentMeta.FileMimeType);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
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
            if (user == null) return NotFound(UserNotFoundMessage);

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
            if (user == null) return NotFound(UserNotFoundMessage);

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
            if (user == null) return NotFound(UserNotFoundMessage);

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