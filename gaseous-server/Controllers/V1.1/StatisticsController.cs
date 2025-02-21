using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using gaseous_server.Models;
using gaseous_server.Classes;
using Authentication;
using Microsoft.AspNetCore.Identity;
using Asp.Versioning;

namespace gaseous_server.Controllers.v1_1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0", Deprecated = true)]
    [ApiVersion("1.1")]
    [ApiController]
    public class StatisticsController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public StatisticsController(
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
        [Authorize]
        [ProducesResponseType(typeof(Models.StatisticsModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("Games/{GameId}/{PlatformId}/{RomId}")]
        public async Task<ActionResult> NewRecordStatistics(long GameId, long PlatformId, long RomId, bool IsMediaGroup)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user != null)
            {
                Statistics statistics = new Statistics();
                return Ok(statistics.RecordSession(Guid.Empty, GameId, PlatformId, RomId, IsMediaGroup, user.Id));
            }
            else
            {
                return Unauthorized();
            }
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpPut]
        [Authorize]
        [ProducesResponseType(typeof(Models.StatisticsModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("Games/{GameId}/{PlatformId}/{RomId}/{SessionId}")]
        public async Task<ActionResult> SubsequentRecordStatistics(long GameId, long PlatformId, long RomId, Guid SessionId, bool IsMediaGroup)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user != null)
            {
                Statistics statistics = new Statistics();
                return Ok(statistics.RecordSession(SessionId, GameId, PlatformId, RomId, IsMediaGroup, user.Id));
            }
            else
            {
                return Unauthorized();
            }
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(Models.StatisticsModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("Games/{GameId}")]
        public async Task<ActionResult> GetStatistics(long GameId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user != null)
            {
                Statistics statistics = new Statistics();
                StatisticsModel? model = statistics.GetSession(GameId, user.Id);
                if (model == null)
                {
                    return NoContent();
                }
                else
                {
                    return Ok(model);
                }
            }
            else
            {
                return Unauthorized();
            }
        }
    }
}