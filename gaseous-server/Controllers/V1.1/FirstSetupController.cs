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
                    Logging.LogKey(Logging.LogType.Information, "process.first_run", "firstrun.creating_new_account", null, new string[] { model.Email });
                    var result = await _userManager.CreateAsync(user, model.Password);
                    if (result.Succeeded)
                    {
                        Logging.LogKey(Logging.LogType.Information, "process.first_run", "firstrun.creation_successful", null, new string[] { model.Email });
                        Logging.LogKey(Logging.LogType.Information, "process.first_run", "firstrun.adding_player_role", null, new string[] { model.Email });
                        await _userManager.AddToRoleAsync(user, "Player");
                        Logging.LogKey(Logging.LogType.Information, "process.first_run", "firstrun.adding_gamer_role", null, new string[] { model.Email });
                        await _userManager.AddToRoleAsync(user, "Gamer");
                        Logging.LogKey(Logging.LogType.Information, "process.first_run", "firstrun.adding_admin_role", null, new string[] { model.Email });
                        await _userManager.AddToRoleAsync(user, "Admin");

                        Logging.LogKey(Logging.LogType.Information, "process.first_run", "firstrun.signing_in_as", null, new string[] { model.Email });
                        await _signInManager.SignInAsync(user, isPersistent: true);

                        Logging.LogKey(Logging.LogType.Information, "process.first_run", "firstrun.setting_first_run_state_to", null, new string[] { "1" });
                        Config.SetSetting<string>("FirstRunStatus", "1");

                        Logging.LogKey(Logging.LogType.Information, "process.first_run", "firstrun.migrating_existing_collections");
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
                    Logging.LogKey(Logging.LogType.Information, "process.first_run", "firstrun.setting_up_datasources");

                    SystemController sys = new SystemController();
                    sys.SetSystemSettings(model);

                    Logging.LogKey(Logging.LogType.Information, "process.first_run", "firstrun.setting_first_run_state_to", null, new string[] { "1" });
                    Config.SetSetting<string>("FirstRunStatus", "2");

                    Logging.LogKey(Logging.LogType.Information, "process.first_run", "firstrun.setting_up_datasources_complete_starting_metadata_refresh");
                    ProcessQueue.QueueProcessor.QueueItem metadataRefresh = ProcessQueue.QueueProcessor.QueueItems.Find(x => x.ItemType == ProcessQueue.QueueItemType.MetadataRefresh);
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