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
using Asp.Versioning;

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
            if (Config.ReadSetting<string>("FirstRunStatus", "0") == "0")
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
                    Logging.Log(Logging.LogType.Information, "First Run", "Creating new account " + model.Email);
                    var result = await _userManager.CreateAsync(user, model.Password);
                    if (result.Succeeded)
                    {
                        Logging.Log(Logging.LogType.Information, "First Run", "Creation of " + model.Email + " successful.");
                        Logging.Log(Logging.LogType.Information, "First Run", "Adding Player role to " + model.Email);
                        await _userManager.AddToRoleAsync(user, "Player");
                        Logging.Log(Logging.LogType.Information, "First Run", "Adding Gamer role to " + model.Email);
                        await _userManager.AddToRoleAsync(user, "Gamer");
                        Logging.Log(Logging.LogType.Information, "First Run", "Adding Admin role to " + model.Email);
                        await _userManager.AddToRoleAsync(user, "Admin");

                        Logging.Log(Logging.LogType.Information, "First Run", "Signing in as " + model.Email);
                        await _signInManager.SignInAsync(user, isPersistent: true);

                        Logging.Log(Logging.LogType.Information, "First Run", "Setting first run state to 1");
                        Config.SetSetting<string>("FirstRunStatus", "1");

                        Logging.Log(Logging.LogType.Information, "First Run", "Migrating existing collections to newly created user (for upgrades from v1.6.1 and earlier)");
                        Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
                        string sql = "UPDATE RomCollections SET OwnedBy=@userid WHERE OwnedBy IS NULL;";
                        Dictionary<string, object> dbDict = new Dictionary<string, object>
                        {
                            { "userid", user.Id }
                        };
                        db.ExecuteCMD(sql, dbDict);

                        return Ok(result);
                    }
                    else
                    {
                        return Ok(result);
                    }
                }

                return Problem(ModelState.ToString());
            }
            else
            {
                return NotFound();
            }
        }

        [MapToApiVersion("1.1")]
        [HttpPost]
        [Route("1")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult SetupDatasources(SystemSettingsModel model)
        {
            if (Config.ReadSetting<string>("FirstRunStatus", "0") == "1")
            {
                if (ModelState.IsValid)
                {
                    Logging.Log(Logging.LogType.Information, "First Run", "Setting up datasources");

                    SystemController sys = new SystemController();
                    sys.SetSystemSettings(model);

                    Logging.Log(Logging.LogType.Information, "First Run", "Setting first run state to 1");
                    Config.SetSetting<string>("FirstRunStatus", "2");

                    Logging.Log(Logging.LogType.Information, "First Run", "Setting up datasources complete, starting metadata refresh");
                    ProcessQueue.QueueItem metadataRefresh = ProcessQueue.QueueItems.Find(x => x.ItemType == ProcessQueue.QueueItemType.MetadataRefresh);
                    if (metadataRefresh != null)
                    {
                        metadataRefresh.ForceExecute();
                    }

                    return Ok();
                }
                else
                {
                    return Problem(ModelState.ToString());
                }
            }
            else
            {
                return NotFound();
            }
        }
    }
}