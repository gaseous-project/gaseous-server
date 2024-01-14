using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using gaseous_server.Models;
using gaseous_server.Classes;
using Authentication;
using Microsoft.AspNetCore.Identity;
using System.Data;

namespace gaseous_server.Controllers.v1_1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [ApiVersion("1.1")]
    [ApiController]
    public class StateManagerController: ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public StateManagerController(
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
        [ProducesResponseType(typeof(Models.GameStateItem), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("{RomId}")]
        public async Task<ActionResult> SaveStateAsync(long RomId, UploadStateModel uploadState, bool IsMediaGroup = false)
        {
            var user = await _userManager.GetUserAsync(User);
            
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "INSERT INTO GameState (UserId, RomId, IsMediaGroup, StateDateTime, Name, Screenshot, State) VALUES (@userid, @romid, @ismediagroup, @statedatetime, @name, @screenshot, @state); SELECT LAST_INSERT_ID();";
            Dictionary<string, object> dbDict = new Dictionary<string, object>
            {
                { "userid", user.Id },
                { "romid", RomId },
                { "ismediagroup", IsMediaGroup },
                { "statedatetime", DateTime.UtcNow },
                { "name", "" },
                { "screenshot", uploadState.ScreenshotByteArray },
                { "state", uploadState.StateByteArray }
            };
            DataTable data = db.ExecuteCMD(sql, dbDict);

            return Ok(await GetStateAsync(RomId, (long)(ulong)data.Rows[0][0], IsMediaGroup));
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(List<Models.GameStateItem>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("{RomId}")]
        public async Task<ActionResult> GetAllStateAsync(long RomId, bool IsMediaGroup = false)
        {
            var user = await _userManager.GetUserAsync(User);
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "SELECT Id, StateDateTime, `Name`, Screenshot FROM GameState WHERE RomId = @romid AND IsMediaGroup = @ismediagroup AND UserId = @userid ORDER BY StateDateTime DESC;";
            Dictionary<string, object> dbDict = new Dictionary<string, object>
            {
                { "romid", RomId },
                { "userid", user.Id },
                { "ismediagroup", IsMediaGroup }
            };
            DataTable data = db.ExecuteCMD(sql, dbDict);
            
            List<Models.GameStateItem> gameStates = new List<GameStateItem>();
            foreach (DataRow row in data.Rows)
            {
                gameStates.Add(BuildGameStateItem(row));
            }

            return Ok(gameStates);
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(Models.GameStateItem), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("{RomId}/{StateId}")]
        public async Task<ActionResult> GetStateAsync(long RomId, long StateId, bool IsMediaGroup = false)
        {
            var user = await _userManager.GetUserAsync(User);
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "SELECT Id, StateDateTime, `Name`, Screenshot FROM GameState WHERE Id = @id AND RomId = @romid AND IsMediaGroup = @ismediagroup AND UserId = @userid;";
            Dictionary<string, object> dbDict = new Dictionary<string, object>
            {
                { "id", StateId },
                { "romid", RomId },
                { "userid", user.Id },
                { "ismediagroup", IsMediaGroup }
            };
            DataTable data = db.ExecuteCMD(sql, dbDict);
            
            if (data.Rows.Count == 0)
            {
                // invalid match - return not found
                return NotFound();
            }
            else
            {
                GameStateItem stateItem = BuildGameStateItem(data.Rows[0]);

                return Ok(stateItem);
            }
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpDelete]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("{RomId}/{StateId}")]
        public async Task<ActionResult> DeleteStateAsync(long RomId, long StateId, bool IsMediaGroup = false)
        {
            var user = await _userManager.GetUserAsync(User);
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "DELETE FROM GameState WHERE Id = @id AND RomId = @romid AND IsMediaGroup = @ismediagroup AND UserId = @userid;";
            Dictionary<string, object> dbDict = new Dictionary<string, object>
            {
                { "id", StateId },
                { "romid", RomId },
                { "userid", user.Id },
                { "ismediagroup", IsMediaGroup }
            };
            db.ExecuteNonQuery(sql, dbDict);
            
            return Ok();
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpPut]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("{RomId}/{StateId}")]
        public async Task<ActionResult> EditStateAsync(long RomId, long StateId, GameStateItemUpdateModel model, bool IsMediaGroup = false)
        {
            var user = await _userManager.GetUserAsync(User);
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "UPDATE GameState SET `Name` = @name WHERE Id = @id AND RomId = @romid AND IsMediaGroup = @ismediagroup AND UserId = @userid;";
            Dictionary<string, object> dbDict = new Dictionary<string, object>
            {
                { "id", StateId },
                { "romid", RomId },
                { "userid", user.Id },
                { "ismediagroup", IsMediaGroup },
                { "name", model.Name }
            };
            db.ExecuteNonQuery(sql, dbDict);
            
            return Ok();
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("{RomId}/{StateId}/Screenshot/")]
        [Route("{RomId}/{StateId}/Screenshot/image.png")]
        public async Task<ActionResult> GetStateScreenshotAsync(long RomId, long StateId, bool IsMediaGroup = false)
        {
            var user = await _userManager.GetUserAsync(User);
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "SELECT Screenshot FROM GameState WHERE Id = @id AND RomId = @romid AND IsMediaGroup = @ismediagroup AND UserId = @userid;";
            Dictionary<string, object> dbDict = new Dictionary<string, object>
            {
                { "id", StateId },
                { "romid", RomId },
                { "userid", user.Id },
                { "ismediagroup", IsMediaGroup }
            };
            DataTable data = db.ExecuteCMD(sql, dbDict);
            
            if (data.Rows.Count == 0)
            {
                // invalid match - return not found
                return NotFound();
            }
            else
            {
                string filename = "image.jpg";
                byte[] bytes = (byte[])data.Rows[0][0];
                string contentType = "image/png";

                var cd = new System.Net.Mime.ContentDisposition
                {
                    FileName = filename,
                    Inline = true,
                };

                Response.Headers.Add("Content-Disposition", cd.ToString());
                Response.Headers.Add("Cache-Control", "public, max-age=604800");

                return File(bytes, contentType);
            }
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("{RomId}/{StateId}/State/")]
        [Route("{RomId}/{StateId}/State/savestate.state")]
        public async Task<ActionResult> GetStateDataAsync(long RomId, long StateId, bool IsMediaGroup = false)
        {
            var user = await _userManager.GetUserAsync(User);
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "SELECT State FROM GameState WHERE Id = @id AND RomId = @romid AND IsMediaGroup = @ismediagroup AND UserId = @userid;";
            Dictionary<string, object> dbDict = new Dictionary<string, object>
            {
                { "id", StateId },
                { "romid", RomId },
                { "userid", user.Id },
                { "ismediagroup", IsMediaGroup }
            };
            DataTable data = db.ExecuteCMD(sql, dbDict);
            
            if (data.Rows.Count == 0)
            {
                // invalid match - return not found
                return NotFound();
            }
            else
            {
                string filename = "savestate.state";
                byte[] bytes = (byte[])data.Rows[0][0];
                string contentType = "application/octet-stream";

                var cd = new System.Net.Mime.ContentDisposition
                {
                    FileName = filename,
                    Inline = true,
                };

                Response.Headers.Add("Content-Disposition", cd.ToString());
                Response.Headers.Add("Cache-Control", "public, max-age=604800");

                return File(bytes, contentType);
            }
        }

        private Models.GameStateItem BuildGameStateItem(DataRow dr)
        {
            bool HasScreenshot = true;
            if (dr["Screenshot"] == DBNull.Value)
            {
                HasScreenshot = false;
            }
            GameStateItem stateItem = new GameStateItem
            {
                Id = (long)dr["Id"],
                Name = (string)dr["Name"],
                SaveTime = DateTime.Parse(((DateTime)dr["StateDateTime"]).ToString("yyyy-MM-ddThh:mm:ss") + 'Z'),
                HasScreenshot = HasScreenshot
            };

            return stateItem;
        }
    }
}