using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using gaseous_server.Models;
using gaseous_server.Classes;
using Authentication;
using Microsoft.AspNetCore.Identity;
using System.Data;
using Asp.Versioning;
using System.IO.Compression;

namespace gaseous_server.Controllers.v1_1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [ApiVersion("1.1")]
    [ApiController]
    public class StateManagerController : ControllerBase
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

            byte[] CompressedState = Common.Compress(uploadState.StateByteArray);

            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "INSERT INTO GameState (UserId, RomId, IsMediaGroup, StateDateTime, Name, Screenshot, State, Zipped) VALUES (@userid, @romid, @ismediagroup, @statedatetime, @name, @screenshot, @state, @zipped); SELECT LAST_INSERT_ID();";
            Dictionary<string, object> dbDict = new Dictionary<string, object>
            {
                { "userid", user.Id },
                { "romid", RomId },
                { "ismediagroup", IsMediaGroup },
                { "statedatetime", DateTime.UtcNow },
                { "name", "" },
                { "screenshot", uploadState.ScreenshotByteArray },
                { "state", CompressedState },
                { "zipped", true }
            };
            DataTable data = db.ExecuteCMD(sql, dbDict);

            if (IsMediaGroup == false)
            {
                Logging.Log(Logging.LogType.Information, "Save State", "Saved state for rom id " + RomId + ". State size: " + uploadState.StateByteArrayBase64.Length + " Compressed size: " + CompressedState.Length);
            }
            else
            {
                Logging.Log(Logging.LogType.Information, "Save State", "Saved state for media group id " + RomId + ". State size: " + uploadState.StateByteArrayBase64.Length + " Compressed size: " + CompressedState.Length);
            }

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
        public async Task<ActionResult> GetStateDataAsync(long RomId, long StateId, bool IsMediaGroup = false, bool StateOnly = false)
        {
            var user = await _userManager.GetUserAsync(User);
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "SELECT * FROM GameState WHERE Id = @id AND RomId = @romid AND IsMediaGroup = @ismediagroup AND UserId = @userid;";
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
                // get rom data
                Roms.GameRomItem romItem = Roms.GetRom(RomId);

                byte[] bytes;
                if ((bool)data.Rows[0]["Zipped"] == false)
                {
                    bytes = (byte[])data.Rows[0]["State"];
                }
                else
                {
                    bytes = Common.Decompress((byte[])data.Rows[0]["State"]);
                }

                string contentType = "";
                string filename = ((DateTime)data.Rows[0]["StateDateTime"]).ToString("yyyy-MM-ddTHH-mm-ss") + "-" + Path.GetFileNameWithoutExtension(romItem.Name);


                if (StateOnly == true)
                {
                    contentType = "application/octet-stream";
                    filename = filename + ".state";
                }
                else
                {
                    contentType = "application/zip";
                    filename = filename + ".zip";

                    Dictionary<string, object> RomInfo = new Dictionary<string, object>
                    {
                        { "Name", romItem.Name },
                        { "StateDateTime", data.Rows[0]["StateDateTime"] },
                        { "StateName", data.Rows[0]["Name"] }
                    };
                    if ((int)data.Rows[0]["IsMediaGroup"] == 0)
                    {
                        RomInfo.Add("MD5", romItem.Md5);
                        RomInfo.Add("SHA1", romItem.Sha1);
                        RomInfo.Add("Type", "ROM");
                    }
                    else
                    {
                        RomInfo.Add("Type", "Media Group");
                        RomInfo.Add("MediaGroupId", (long)data.Rows[0]["RomId"]);
                    }
                    string RomInfoString = Newtonsoft.Json.JsonConvert.SerializeObject(RomInfo, Newtonsoft.Json.Formatting.Indented, new Newtonsoft.Json.JsonSerializerSettings { NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore });

                    // compile zip file
                    using (var compressedFileStream = new MemoryStream())
                    {
                        List<Dictionary<string, object>> Attachments = new List<Dictionary<string, object>>();
                        Attachments.Add(new Dictionary<string, object>
                        {
                            { "Name", "savestate.state" },
                            { "Body", bytes }
                        });
                        Attachments.Add(new Dictionary<string, object>
                        {
                            { "Name", "screenshot.jpg" },
                            { "Body", (byte[])data.Rows[0]["Screenshot"] }
                        });
                        Attachments.Add(new Dictionary<string, object>
                        {
                            { "Name", "rominfo.json" },
                            { "Body", System.Text.Encoding.UTF8.GetBytes(RomInfoString) }
                        });

                        //Create an archive and store the stream in memory.
                        using (var zipArchive = new ZipArchive(compressedFileStream, ZipArchiveMode.Create, false))
                        {
                            foreach (var Attachment in Attachments)
                            {
                                //Create a zip entry for each attachment
                                var zipEntry = zipArchive.CreateEntry(Attachment["Name"].ToString());

                                //Get the stream of the attachment
                                using (var originalFileStream = new MemoryStream((byte[])Attachment["Body"]))
                                using (var zipEntryStream = zipEntry.Open())
                                {
                                    //Copy the attachment stream to the zip entry stream
                                    originalFileStream.CopyTo(zipEntryStream);
                                }
                            }
                        }

                        //return new FileContentResult(compressedFileStream.ToArray(), "application/zip") { FileDownloadName = filename };
                        bytes = compressedFileStream.ToArray();
                    }
                }

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