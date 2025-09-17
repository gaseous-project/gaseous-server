using Asp.Versioning;
using Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

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
    }
}