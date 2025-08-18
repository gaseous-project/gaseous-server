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
            // Disable local password login when turned off
            if (!Config.SocialAuthConfiguration.PasswordLoginEnabled)
            {
                Logging.Log(Logging.LogType.Warning, "Login", $"Password login attempt blocked for {model?.Email ?? "unknown"} from IP: {HttpContext.Connection.RemoteIpAddress}");
                return NotFound(); // or: return Forbid();
            }

            if (ModelState.IsValid)
            {
                // Allow login with either username or email
                var input = model.Email?.Trim();
                if (string.IsNullOrWhiteSpace(input))
                {
                    Logging.Log(Logging.LogType.Warning, "Login", $"Empty username/email provided from IP: {HttpContext.Connection.RemoteIpAddress}");
                    return Unauthorized();
                }

                // Resolve user by email first (if input looks like an email), then by username
                ApplicationUser? user = null;
                if (input.Contains('@'))
                {
                    user = await _userManager.FindByEmailAsync(input);
                }
                if (user == null)
                {
                    user = await _userManager.FindByNameAsync(input);
                }

                if (user == null)
                {
                    Logging.Log(Logging.LogType.Warning, "Login", $"Login failed for unknown user '{input}' from IP: {HttpContext.Connection.RemoteIpAddress}");
                    return Unauthorized();
                }

                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                var result = await _signInManager.PasswordSignInAsync(user.UserName, model.Password, model.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    Logging.Log(Logging.LogType.Information, "Login", $"{user.UserName} has logged in, from IP: {HttpContext.Connection.RemoteIpAddress}");
                    return Ok(new { success = true });
                }
                if (result.RequiresTwoFactor)
                {
                    // Return a hint to the client to prompt for 2FA code
                    Logging.Log(Logging.LogType.Information, "Login", $"{user.UserName} requires two-factor authentication. IP: {HttpContext.Connection.RemoteIpAddress}");
                    return Ok(new { requiresTwoFactor = true, rememberMe = model.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    Logging.Log(Logging.LogType.Warning, "Login", $"{user.UserName} was unable to login due to a locked account. Login attempt from IP: {HttpContext.Connection.RemoteIpAddress}");
                    return Unauthorized();
                }
                else
                {
                    Logging.Log(Logging.LogType.Critical, "Login", $"An unknown error occurred during login by {user.UserName}. Login attempt from IP: {HttpContext.Connection.RemoteIpAddress}");
                    return Unauthorized();
                }
            }

            // If we got this far, something failed, redisplay form
            Logging.Log(Logging.LogType.Critical, "Login", $"An unknown error occurred during login. Login attempt from IP: {HttpContext.Connection.RemoteIpAddress}");
            return Unauthorized();
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("Login2FA")]
        public async Task<IActionResult> LoginTwoFactor(TwoFactorVerifyViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            // Normalize code (remove spaces/hyphens)
            var code = model.Code.Replace(" ", string.Empty).Replace("-", string.Empty);

            var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(code, model.RememberMe, model.RememberMachine);
            if (result.Succeeded)
            {
                return Ok(new { success = true });
            }
            if (result.IsLockedOut)
            {
                Logging.Log(Logging.LogType.Warning, "Login", $"Two-factor lockout triggered for IP: {HttpContext.Connection.RemoteIpAddress}");
                return Unauthorized();
            }

            return Unauthorized();
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("LoginRecoveryCode")]
        public async Task<IActionResult> LoginWithRecoveryCode(TwoFactorRecoveryViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var code = model.RecoveryCode.Replace(" ", string.Empty);
            var result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(code);
            if (result.Succeeded)
            {
                return Ok(new { success = true });
            }
            if (result.IsLockedOut)
            {
                Logging.Log(Logging.LogType.Warning, "Login", $"Recovery code sign-in lockout for IP: {HttpContext.Connection.RemoteIpAddress}");
                return Unauthorized();
            }

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
            if (!Config.SocialAuthConfiguration.PasswordLoginEnabled)
            {
                return Forbid(); // or NotFound();
            }

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

        [HttpPost]
        [Route("ChangeUsername")]
        [Authorize]
        public async Task<IActionResult> ChangeUsername(ChangeUsernameViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var desired = model.NewUserName?.Trim();
            if (string.IsNullOrWhiteSpace(desired))
            {
                return BadRequest("Username cannot be empty.");
            }

            // If same username (case-insensitive), no change needed
            if (string.Equals(user.UserName, desired, StringComparison.OrdinalIgnoreCase))
            {
                return Ok();
            }

            // Ensure uniqueness (case-insensitive via normalized lookup)
            var existing = await _userManager.FindByNameAsync(desired);
            if (existing != null && existing.Id != user.Id)
            {
                return Conflict("Username already in use.");
            }

            // Update username and normalized username
            user.UserName = desired;
            user.NormalizedUserName = desired.ToUpperInvariant();

            var result = await _userManager.SetUserNameAsync(user, desired);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            await _signInManager.RefreshSignInAsync(user);
            Logging.Log(Logging.LogType.Information, "User Management", $"{User.FindFirstValue(ClaimTypes.NameIdentifier)} changed their username to '{desired}'.");
            return Ok();
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
                user.UserName = rawUser.UserName ?? rawUser.NormalizedEmail.ToLower();
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

                IdentityResult result;
                if (!Config.SocialAuthConfiguration.PasswordLoginEnabled)
                {
                    // Create without a password; user must link an external login
                    result = await _userManager.CreateAsync(user);
                }
                else
                {
                    result = await _userManager.CreateAsync(user, model.Password);
                }

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
        [Authorize(Roles = "Player,Gamer,Admin")]
        public async Task<IActionResult> TestUserExists(string Email)
        {
            // check if the user exists by email
            ApplicationUser? rawUser = await _userManager.FindByEmailAsync(Email);

            // fall back to username if email is not found
            if (rawUser == null)
            {
                rawUser = await _userManager.FindByNameAsync(Email);
            }

            // return true if user exists, false otherwise
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
            if (!Config.SocialAuthConfiguration.PasswordLoginEnabled)
            {
                return Forbid(); // or NotFound();
            }

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

        [HttpGet]
        [AllowAnonymous]
        [Route("social-login")]
        public IActionResult SocialLoginAvailable()
        {
            // This endpoint is used to check if social login is available
            List<string> availableLogins = new List<string>();

            if (Config.SocialAuthConfiguration.PasswordLoginEnabled)
            {
                availableLogins.Add("Password");
            }
            if (Config.SocialAuthConfiguration.GoogleAuthEnabled)
            {
                availableLogins.Add("Google");
            }
            if (Config.SocialAuthConfiguration.MicrosoftAuthEnabled)
            {
                availableLogins.Add("Microsoft");
            }
            if (Config.SocialAuthConfiguration.OIDCAuthEnabled)
            {
                availableLogins.Add("OIDC");
            }

            return Ok(availableLogins);
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("signin-google")]
        public IActionResult SignInGoogle(string returnUrl = "/")
        {
            var redirectUrl = Url.Action("GoogleResponse", "Account", new { ReturnUrl = returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties("Google", redirectUrl);
            return Challenge(properties, "Google");
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("signin-microsoft")]
        public IActionResult SignInMicrosoft(string returnUrl = "/")
        {
            var redirectUrl = Url.Action("MicrosoftResponse", "Account", new { ReturnUrl = returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties("Microsoft", redirectUrl);
            return Challenge(properties, "Microsoft");
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("signin-oidc")]
        public IActionResult SignInOIDC(string returnUrl = "/")
        {
            var redirectUrl = Url.Action("OIDCResponse", "Account", new { ReturnUrl = returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties("OIDC", redirectUrl);
            return Challenge(properties, "OIDC");
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("GoogleResponse")]
        public async Task<IActionResult> GoogleResponse(string returnUrl = "/")
        {
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
                return RedirectToAction(nameof(Login));

            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: true, bypassTwoFactor: true);
            if (result.Succeeded)
            {
                return LocalRedirect(returnUrl);
            }
            else
            {
                // Get the email from the external provider
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                if (email == null)
                {
                    return Unauthorized();
                }

                // Try to find an existing user with this email
                var user = await _userManager.FindByEmailAsync(email);
                if (user != null)
                {
                    // Link the Google login to the existing user
                    var addLoginResult = await _userManager.AddLoginAsync(user, info);
                    if (addLoginResult.Succeeded)
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                    else
                    {
                        return Unauthorized(addLoginResult.Errors);
                    }
                }
                else
                {
                    // No user exists, create a new one
                    user = new ApplicationUser { UserName = email, Email = email };
                    var identityResult = await _userManager.CreateAsync(user);
                    if (identityResult.Succeeded)
                    {
                        await _userManager.AddLoginAsync(user, info);
                        await _userManager.AddToRoleAsync(user, "Player");
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                    return Unauthorized(identityResult.Errors);
                }
            }
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("MicrosoftResponse")]
        public async Task<IActionResult> MicrosoftResponse(string returnUrl = "/")
        {
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
                return RedirectToAction(nameof(Login));

            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: true, bypassTwoFactor: true);
            if (result.Succeeded)
            {
                return LocalRedirect(returnUrl);
            }
            else
            {
                // Get the email from the external provider
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                if (email == null)
                {
                    return Unauthorized();
                }

                // Try to find an existing user with this email
                var user = await _userManager.FindByEmailAsync(email);
                if (user != null)
                {
                    // Link the Microsoft login to the existing user
                    var addLoginResult = await _userManager.AddLoginAsync(user, info);
                    if (addLoginResult.Succeeded)
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                    else
                    {
                        return Unauthorized(addLoginResult.Errors);
                    }
                }
                else
                {
                    // No user exists, create a new one
                    user = new ApplicationUser { UserName = email, Email = email };
                    var identityResult = await _userManager.CreateAsync(user);
                    if (identityResult.Succeeded)
                    {
                        await _userManager.AddLoginAsync(user, info);
                        await _userManager.AddToRoleAsync(user, "Player");
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                    return Unauthorized(identityResult.Errors);
                }
            }
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("OIDCResponse")]
        public async Task<IActionResult> OIDCResponse(string returnUrl = "/")
        {
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
                return RedirectToAction(nameof(Login));

            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: true, bypassTwoFactor: true);
            if (result.Succeeded)
            {
                return LocalRedirect(returnUrl);
            }
            else
            {
                // Get the email from the external provider
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                if (email == null)
                {
                    return Unauthorized();
                }

                // Try to find an existing user with this email
                var user = await _userManager.FindByEmailAsync(email);
                if (user != null)
                {
                    // Link the OIDC login to the existing user
                    var addLoginResult = await _userManager.AddLoginAsync(user, info);
                    if (addLoginResult.Succeeded)
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                    else
                    {
                        return Unauthorized(addLoginResult.Errors);
                    }
                }
                else
                {
                    // No user exists, create a new one
                    user = new ApplicationUser { UserName = email, Email = email };
                    var identityResult = await _userManager.CreateAsync(user);
                    if (identityResult.Succeeded)
                    {
                        await _userManager.AddLoginAsync(user, info);
                        await _userManager.AddToRoleAsync(user, "Player");
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                    return Unauthorized(identityResult.Errors);
                }
            }
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("signout-oidc")]
        public async Task<IActionResult> SignOutOIDC(string returnUrl = "/")
        {
            await _signInManager.SignOutAsync();
            Logging.Log(Logging.LogType.Information, "Login", "User has signed out via OIDC.");
            return LocalRedirect(returnUrl);
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("signout-google")]
        public async Task<IActionResult> SignOutGoogle(string returnUrl = "/")
        {
            await _signInManager.SignOutAsync();
            Logging.Log(Logging.LogType.Information, "Login", "User has signed out via Google.");
            return LocalRedirect(returnUrl);
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("signout-microsoft")]
        public async Task<IActionResult> SignOutMicrosoft(string returnUrl = "/")
        {
            await _signInManager.SignOutAsync();
            Logging.Log(Logging.LogType.Information, "Login", "User has signed out via Microsoft.");
            return LocalRedirect(returnUrl);
        }
    }
}