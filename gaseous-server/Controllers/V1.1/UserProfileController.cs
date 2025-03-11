using Asp.Versioning;
using Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace gaseous_server.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0", Deprecated = true)]
    [ApiVersion("1.1")]
    [Authorize]
    public class UserProfileController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public UserProfileController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager
        )
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("{UserId}")]
        [ProducesResponseType(typeof(Models.UserProfile), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public ActionResult GetUserProfile(string UserId)
        {
            Classes.UserProfile profile = new Classes.UserProfile();
            Models.UserProfile RetVal = profile.GetUserProfile(UserId);
            return Ok(RetVal);
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpPut]
        [Route("{UserId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> UpdateUserProfileAsync(string UserId, Models.UserProfile profile)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user.ProfileId.ToString() != UserId)
            {
                return Unauthorized();
            }

            Classes.UserProfile userProfile = new Classes.UserProfile();
            userProfile.UpdateUserProfile(user.Id, profile);
            return Ok();
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpPut]
        [ProducesResponseType(typeof(List<IFormFile>), StatusCodes.Status200OK)]
        [RequestSizeLimit(long.MaxValue)]
        [Consumes("multipart/form-data")]
        [DisableRequestSizeLimit, RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue, ValueLengthLimit = int.MaxValue)]
        [Route("{UserId}/{ProfileImageType}")]
        public async Task<ActionResult> UpdateAvatarAsync(string UserId, Classes.UserProfile.ImageType ProfileImageType, IFormFile file)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user.ProfileId.ToString() != UserId)
            {
                return Unauthorized();
            }

            if (file.Length > 0)
            {
                using (var ms = new MemoryStream())
                {
                    file.CopyTo(ms);
                    byte[] fileBytes = ms.ToArray();

                    Classes.UserProfile userProfile = new Classes.UserProfile();
                    userProfile.UpdateImage(ProfileImageType, UserId, user.Id, file.FileName, fileBytes);
                }
            }

            return Ok();
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("{UserId}/{ProfileImageType}/{FileName}")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ResponseCache(CacheProfileName = "7Days")]
        public async Task<ActionResult> GetAvatarAsync(string UserId, Classes.UserProfile.ImageType ProfileImageType, string FileName)
        {
            Classes.UserProfile userProfile = new Classes.UserProfile();

            Models.ImageItem image = userProfile.GetImage(ProfileImageType, UserId);

            if (image == null)
            {
                return NotFound();
            }
            else
            {
                return File(image.content, image.mimeType, UserId + image.extension);
            }
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpDelete]
        [Route("{UserId}/{ProfileImageType}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> DeleteAvatarAsync(string UserId, Classes.UserProfile.ImageType ProfileImageType)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user.ProfileId.ToString() != UserId)
            {
                return Unauthorized();
            }

            Classes.UserProfile userProfile = new Classes.UserProfile();
            userProfile.DeleteImage(ProfileImageType, user.Id);

            return Ok();
        }
    }
}
