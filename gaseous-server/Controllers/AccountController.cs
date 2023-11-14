using System.Security.Claims;
using Authentication;
using gaseous_server.Classes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace gaseous_server.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
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

            return Ok(profile);
        }
    }
}