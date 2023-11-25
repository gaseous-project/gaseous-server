using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Authentication;
using gaseous_server.Classes;
using gaseous_server.Classes.Metadata;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace gaseous_server.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.1")]
    [Authorize]
    public class FirstSetupController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public FirstSetupController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [MapToApiVersion("1.1")]
        [HttpPost]
        [Route("0")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> CreateAdminAccount(Authentication.RegisterViewModel model)
        {
            if (Config.ReadSetting("FirstRunStatus", "0") == "0")
            {
                if (ModelState.IsValid)
                {
                    ApplicationUser user = new ApplicationUser
                    { 
                        UserName = model.UserName, 
                        NormalizedUserName = model.UserName.ToUpper(),
                        Email = model.Email,
                        NormalizedEmail = model.Email.ToUpper(),
                        SecurityProfile = new SecurityProfileViewModel()
                    };
                    var result = await _userManager.CreateAsync(user, model.Password);
                    if (result.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(user, "Player");
                        await _userManager.AddToRoleAsync(user, "Gamer");
                        await _userManager.AddToRoleAsync(user, "Admin");

                        await _signInManager.SignInAsync(user, isPersistent: true);
                        
                        Config.SetSetting("FirstRunStatus", "1");

                        return Ok();
                    }
                }

                return Problem(ModelState.ToString());
            }
            else
            {
                return NotFound();
            }
        }
    }
}