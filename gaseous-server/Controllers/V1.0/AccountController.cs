using System.Data;
using System.Security.Claims;
using System.Text;
using Authentication;
using gaseous_server.Classes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Asp.Versioning;
using IGDB;

namespace gaseous_server.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0", Deprecated = true)]
    [ApiVersion("1.1")]
    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender,
            ILoggerFactory loggerFactory)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _logger = loggerFactory.CreateLogger<AccountController>();
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("Login")]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    Logging.Log(Logging.LogType.Information, "Login", model.Email + " has logged in, from IP: " + HttpContext.Connection.RemoteIpAddress?.ToString());
                    return Ok(result.ToString());
                }
                // if (result.RequiresTwoFactor)
                // {
                //     return RedirectToAction(nameof(SendCode), new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                // }
                if (result.IsLockedOut)
                {
                    Logging.Log(Logging.LogType.Warning, "Login", model.Email + " was unable to login due to a locked account. Login attempt from IP: " + HttpContext.Connection.RemoteIpAddress?.ToString());
                    return Unauthorized();
                }
                else
                {
                    Logging.Log(Logging.LogType.Critical, "Login", "An unknown error occurred during login by " + model.Email + ". Login attempt from IP: " + HttpContext.Connection.RemoteIpAddress?.ToString());
                    return Unauthorized();
                }
            }

            // If we got this far, something failed, redisplay form
            Logging.Log(Logging.LogType.Critical, "Login", "An unknown error occurred during login by " + model.Email + ". Login attempt from IP: " + HttpContext.Connection.RemoteIpAddress?.ToString());
            return Unauthorized();
        }

        [HttpPost]
        [Route("LogOff")]
        public async Task<IActionResult> LogOff()
        {
            var userName = User.FindFirstValue(ClaimTypes.Name);
            await _signInManager.SignOutAsync();
            if (userName != null)
            {
                Logging.Log(Logging.LogType.Information, "Login", userName + " has logged out");
            }
            return Ok();
        }

        [HttpGet]
        [Route("Profile/Basic")]
        [Authorize]
        public async Task<IActionResult> ProfileBasic()
        {
            ProfileBasicViewModel profile = new ProfileBasicViewModel();
            profile.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ApplicationUser user = await _userManager.FindByIdAsync(profile.UserId);
            profile.UserName = _userManager.GetUserName(HttpContext.User);
            profile.EmailAddress = await _userManager.GetEmailAsync(user);
            profile.Roles = new List<string>(await _userManager.GetRolesAsync(user));
            profile.SecurityProfile = user.SecurityProfile;
            profile.UserPreferences = user.UserPreferences;
            profile.ProfileId = user.ProfileId;
            profile.Roles.Sort();

            return Ok(profile);
        }

        [HttpGet]
        [Route("Profile/Basic/profile.js")]
        [ApiExplorerSettings(IgnoreApi = true)]
        [AllowAnonymous]
        public async Task<IActionResult> ProfileBasicFile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                ProfileBasicViewModel profile = new ProfileBasicViewModel();
                profile.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                profile.UserName = _userManager.GetUserName(HttpContext.User);
                profile.EmailAddress = await _userManager.GetEmailAsync(user);
                profile.Roles = new List<string>(await _userManager.GetRolesAsync(user));
                profile.SecurityProfile = user.SecurityProfile;
                profile.UserPreferences = user.UserPreferences;
                profile.Roles.Sort();

                string profileString = "var userProfile = " + Newtonsoft.Json.JsonConvert.SerializeObject(profile, Newtonsoft.Json.Formatting.Indented) + ";";

                byte[] bytes = Encoding.UTF8.GetBytes(profileString);
                return File(bytes, "text/javascript");
            }
            else
            {
                string profileString = "var userProfile = null;";

                byte[] bytes = Encoding.UTF8.GetBytes(profileString);
                return File(bytes, "text/javascript");
            }
        }

        [HttpPost]
        [Route("ChangePassword")]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login");
                }

                // ChangePasswordAsync changes the user password
                var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);

                // The new password did not meet the complexity rules or
                // the current password is incorrect. Add these errors to
                // the ModelState and rerender ChangePassword view
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return Unauthorized(result);
                }

                // Upon successfully changing the password refresh sign-in cookie
                await _signInManager.RefreshSignInAsync(user);
                return Ok();
            }

            return NotFound();
        }

        [HttpGet]
        [Route("Users")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUsers()
        {
            List<UserViewModel> users = new List<UserViewModel>();

            foreach (ApplicationUser rawUser in _userManager.Users)
            {
                UserViewModel user = new UserViewModel();
                user.Id = rawUser.Id;
                user.EmailAddress = rawUser.NormalizedEmail.ToLower();
                user.LockoutEnabled = rawUser.LockoutEnabled;
                user.LockoutEnd = rawUser.LockoutEnd;
                user.SecurityProfile = rawUser.SecurityProfile;
                user.ProfileId = rawUser.ProfileId;

                // get roles
                ApplicationUser? aUser = await _userManager.FindByIdAsync(rawUser.Id);
                if (aUser != null)
                {
                    IList<string> aUserRoles = await _userManager.GetRolesAsync(aUser);
                    user.Roles = aUserRoles.ToList();

                    user.Roles.Sort();
                }

                users.Add(user);
            }

            return Ok(users);
        }

        [HttpPost]
        [Route("Users")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> NewUser(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                ApplicationUser user = new ApplicationUser
                {
                    UserName = model.UserName,
                    NormalizedUserName = model.UserName.ToUpper(),
                    Email = model.Email,
                    NormalizedEmail = model.Email.ToUpper()
                };
                if (await _userManager.FindByEmailAsync(model.Email) != null)
                {
                    return NotFound("User already exists");
                }
                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    // add new users to the player role
                    await _userManager.AddToRoleAsync(user, "Player");

                    Logging.Log(Logging.LogType.Information, "User Management", User.FindFirstValue(ClaimTypes.Name) + " created user " + model.Email + " with password.");

                    return Ok(result);
                }
                else
                {
                    return Ok(result);
                }
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet]
        [Route("Users/Test")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> TestUserExists(string Email)
        {
            ApplicationUser? rawUser = await _userManager.FindByEmailAsync(Email);

            if (rawUser != null)
            {
                return Ok(true);
            }
            else
            {
                return Ok(false);
            }
        }

        [HttpGet]
        [Route("Users/{UserId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUser(string UserId)
        {
            ApplicationUser? rawUser = await _userManager.FindByIdAsync(UserId);

            if (rawUser != null)
            {
                UserViewModel user = new UserViewModel();
                user.Id = rawUser.Id;
                user.EmailAddress = rawUser.NormalizedEmail.ToLower();
                user.LockoutEnabled = rawUser.LockoutEnabled;
                user.LockoutEnd = rawUser.LockoutEnd;
                user.SecurityProfile = rawUser.SecurityProfile;
                user.ProfileId = rawUser.ProfileId;

                // get roles
                IList<string> aUserRoles = await _userManager.GetRolesAsync(rawUser);
                user.Roles = aUserRoles.ToList();

                user.Roles.Sort();

                return Ok(user);
            }
            else
            {
                return NotFound();
            }
        }

        [HttpDelete]
        [Route("Users/{UserId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(string UserId)
        {
            // get user
            ApplicationUser? user = await _userManager.FindByIdAsync(UserId);

            if (user == null)
            {
                return NotFound();
            }
            else
            {
                await _userManager.DeleteAsync(user);
                Logging.Log(Logging.LogType.Information, "User Management", User.FindFirstValue(ClaimTypes.Name) + " deleted user " + user.Email);
                return Ok();
            }
        }

        [HttpPost]
        [Route("Users/{UserId}/Roles")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SetUserRoles(string UserId, string RoleName)
        {
            ApplicationUser? user = await _userManager.FindByIdAsync(UserId);

            if (user != null)
            {
                // get roles
                List<string> userRoles = (await _userManager.GetRolesAsync(user)).ToList();

                // delete all roles
                foreach (string role in userRoles)
                {
                    if ((new string[] { "Admin", "Gamer", "Player" }).Contains(role))
                    {
                        await _userManager.RemoveFromRoleAsync(user, role);
                    }
                }

                // add only requested roles
                switch (RoleName)
                {
                    case "Admin":
                        await _userManager.AddToRoleAsync(user, "Admin");
                        await _userManager.AddToRoleAsync(user, "Gamer");
                        await _userManager.AddToRoleAsync(user, "Player");
                        break;
                    case "Gamer":
                        await _userManager.AddToRoleAsync(user, "Gamer");
                        await _userManager.AddToRoleAsync(user, "Player");
                        break;
                    case "Player":
                        await _userManager.AddToRoleAsync(user, "Player");
                        break;
                    default:
                        await _userManager.AddToRoleAsync(user, RoleName);
                        break;
                }

                return Ok();
            }
            else
            {
                return NotFound();
            }
        }

        [HttpPost]
        [Route("Users/{UserId}/SecurityProfile")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SetUserSecurityProfile(string UserId, SecurityProfileViewModel securityProfile)
        {
            if (ModelState.IsValid)
            {
                ApplicationUser? user = await _userManager.FindByIdAsync(UserId);

                if (user != null)
                {
                    user.SecurityProfile = securityProfile;
                    await _userManager.UpdateAsync(user);

                    return Ok();
                }
                else
                {
                    return NotFound();
                }
            }
            else
            {
                return NotFound();
            }
        }

        [HttpPost]
        [Route("Users/{UserId}/Password")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ResetPassword(string UserId, SetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                // we can reset the users password
                ApplicationUser? user = await _userManager.FindByIdAsync(UserId);
                if (user != null)
                {
                    string resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                    IdentityResult passwordChangeResult = await _userManager.ResetPasswordAsync(user, resetToken, model.NewPassword);
                    if (passwordChangeResult.Succeeded == true)
                    {
                        return Ok(passwordChangeResult);
                    }
                    else
                    {
                        return Ok(passwordChangeResult);
                    }
                }
                else
                {
                    return NotFound();
                }
            }
            else
            {
                return NotFound();
            }
        }

        [HttpPost]
        [Route("Preferences")]
        public async Task<IActionResult> SetPreferences(List<UserPreferenceViewModel> model)
        {
            ApplicationUser? user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }
            else
            {
                user.UserPreferences = model;
                await _userManager.UpdateAsync(user);

                return Ok();
            }
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [RequestSizeLimit(long.MaxValue)]
        [Consumes("multipart/form-data")]
        [DisableRequestSizeLimit, RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue, ValueLengthLimit = int.MaxValue)]
        [Route("Avatar")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            ApplicationUser? user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }
            else
            {
                Guid avatarId = Guid.Empty;

                if (file.Length > 0)
                {
                    using (var ms = new MemoryStream())
                    {
                        file.CopyTo(ms);
                        byte[] fileBytes = ms.ToArray();
                        byte[] targetBytes;

                        using (var image = new ImageMagick.MagickImage(fileBytes))
                        {
                            ImageMagick.MagickGeometry size = new ImageMagick.MagickGeometry(256, 256);

                            // This will resize the image to a fixed size without maintaining the aspect ratio.
                            // Normally an image will be resized to fit inside the specified size.
                            size.IgnoreAspectRatio = true;

                            image.Resize(size);
                            var newMs = new MemoryStream();
                            image.Resize(size);
                            image.Strip();
                            image.Write(newMs, ImageMagick.MagickFormat.Jpg);

                            targetBytes = newMs.ToArray();
                        }

                        Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
                        UserTable<ApplicationUser> userTable = new UserTable<ApplicationUser>(db);
                        avatarId = userTable.SetAvatar(user, targetBytes);
                    }
                }

                return Ok(avatarId);
            }
        }

        [HttpGet]
        [Route("Avatar/{id}.jpg")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult GetAvatar(Guid id)
        {
            if (id == Guid.Empty)
            {
                return NotFound();
            }
            else
            {
                Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
                string sql = "SELECT * FROM UserAvatars WHERE Id = @id";
                Dictionary<string, object> dbDict = new Dictionary<string, object>{
                    { "id", id }
                };

                DataTable data = db.ExecuteCMD(sql, dbDict);

                if (data.Rows.Count > 0)
                {
                    string filename = id.ToString() + ".jpg";
                    byte[] filedata = (byte[])data.Rows[0]["Avatar"];
                    string contentType = "image/jpg";

                    var cd = new System.Net.Mime.ContentDisposition
                    {
                        FileName = filename,
                        Inline = true,
                    };

                    Response.Headers.Add("Content-Disposition", cd.ToString());
                    Response.Headers.Add("Cache-Control", "public, max-age=604800");

                    return File(filedata, contentType);
                }
                else
                {
                    return NotFound();
                }
            }
        }

        [HttpDelete]
        [Route("Avatar/{id}.jpg")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> DeleteAvatarAsync()
        {
            ApplicationUser? user = await _userManager.GetUserAsync(User);

            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            UserTable<ApplicationUser> userTable = new UserTable<ApplicationUser>(db);
            userTable.SetAvatar(user, new byte[0]);

            return Ok();
        }
    }
}