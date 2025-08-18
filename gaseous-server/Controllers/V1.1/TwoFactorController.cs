using System.Security.Claims;
using Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace gaseous_server.Controllers
{
    /// <summary>
    /// Two-factor authentication management endpoints (status, enable/disable, authenticator key, confirmation, recovery codes).
    /// </summary>
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.1")]
    [Authorize]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class TwoFactorController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="TwoFactorController"/> class.
        /// </summary>
        public TwoFactorController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        /// <summary>
        /// Gets the current two-factor status, including whether enabled, presence of an authenticator key, and remaining recovery codes.
        /// </summary>
        [HttpGet("status")] // GET api/v1.1/TwoFactor/status
        public async Task<ActionResult<TwoFactorStatusModel>> GetStatus()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            var enabled = await _userManager.GetTwoFactorEnabledAsync(user);
            var key = await _userManager.GetAuthenticatorKeyAsync(user);
            var count = await _userManager.CountRecoveryCodesAsync(user);
            return Ok(new TwoFactorStatusModel
            {
                Enabled = enabled,
                HasAuthenticatorKey = !string.IsNullOrEmpty(key),
                RecoveryCodesLeft = count
            });
        }

        /// <summary>
        /// Enables or disables two-factor authentication for the current user.
        /// </summary>
        [HttpPost("enable/{enabled}")] // POST api/v1.1/TwoFactor/enable/true
        public async Task<IActionResult> SetEnabled([FromRoute] bool enabled)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            await _userManager.SetTwoFactorEnabledAsync(user, enabled);
            await _signInManager.RefreshSignInAsync(user);
            return Ok();
        }

        /// <summary>
        /// Resets the authenticator key and returns the new key.
        /// </summary>
        [HttpPost("authenticator/reset")] // reset key and return new
        public async Task<ActionResult<string>> ResetAuthenticatorKey()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            await _userManager.ResetAuthenticatorKeyAsync(user);
            var key = await _userManager.GetAuthenticatorKeyAsync(user);
            return Ok(key ?? string.Empty);
        }

        /// <summary>
        /// Gets the current authenticator key for the user.
        /// </summary>
        [HttpGet("authenticator/key")] // get current key
        public async Task<ActionResult<string>> GetAuthenticatorKey()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            var key = await _userManager.GetAuthenticatorKeyAsync(user);
            return Ok(key ?? string.Empty);
        }

        /// <summary>
        /// Confirms the authenticator setup by validating a TOTP code and enabling two-factor authentication.
        /// </summary>
        [HttpPost("authenticator/confirm")] // confirm code and enable 2FA
        public async Task<IActionResult> ConfirmAuthenticator([FromBody] ConfirmAuthenticatorRequest req)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // normalize code (remove spaces/hyphens)
            var code = req.Code.Replace(" ", string.Empty).Replace("-", string.Empty);

            var provider = _userManager.Options.Tokens.AuthenticatorTokenProvider;
            var valid = await _userManager.VerifyTwoFactorTokenAsync(user, provider, code);
            if (!valid) return BadRequest();

            await _userManager.SetTwoFactorEnabledAsync(user, true);
            await _signInManager.RefreshSignInAsync(user);
            return Ok();
        }



        /// <summary>
        /// Generates a set of new recovery codes for the user.
        /// </summary>
        [HttpPost("recovery/generate")] // generate new recovery codes
        public async Task<ActionResult<IEnumerable<string>>> GenerateRecoveryCodes([FromBody] GenerateRecoveryCodesRequest req)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            var count = req?.Count > 0 ? req.Count : 10;
            var codes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, count);
            return Ok(codes);
        }

        /// <summary>
        /// Counts how many recovery codes remain.
        /// </summary>
        [HttpGet("recovery/count")] // count remaining
        public async Task<ActionResult<int>> CountRecoveryCodes()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            var remaining = await _userManager.CountRecoveryCodesAsync(user);
            return Ok(remaining);
        }

        /// <summary>
        /// Redeems a recovery code for the current user.
        /// </summary>
        [HttpPost("recovery/redeem/{code}")] // redeem a code
        public async Task<IActionResult> RedeemRecoveryCode([FromRoute] string code)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            var result = await _userManager.RedeemTwoFactorRecoveryCodeAsync(user, code);
            if (!result.Succeeded) return BadRequest();
            return Ok();
        }

        // Admin-only endpoint to disable 2FA for another user
        /// <summary>
        /// Admin-only endpoint to disable two-factor for a user, clearing keys and recovery codes.
        /// </summary>
        [HttpPost("admin/disable")] // POST api/v1.1/TwoFactor/admin/disable
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDisableTwoFactor([FromBody] AdminDisable2FARequest req)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            ApplicationUser? target = null;
            if (!string.IsNullOrWhiteSpace(req.UserId))
            {
                target = await _userManager.FindByIdAsync(req.UserId);
            }
            if (target == null && !string.IsNullOrWhiteSpace(req.Email))
            {
                target = await _userManager.FindByEmailAsync(req.Email);
            }
            if (target == null) return NotFound();

            // disable 2FA flag and clear authenticator key and recovery codes
            await _userManager.SetTwoFactorEnabledAsync(target, false);
            await _userManager.ResetAuthenticatorKeyAsync(target);
            // Replace recovery codes with zero to effectively clear
            await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(target, 0);

            await _signInManager.RefreshSignInAsync(target);
            return Ok();
        }
    }
}
