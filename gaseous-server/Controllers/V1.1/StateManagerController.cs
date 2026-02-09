using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using gaseous_server.Models;
using gaseous_server.Classes;
using Authentication;
using Microsoft.AspNetCore.Identity;
using System.Data;
using Asp.Versioning;
using System.IO.Compression;
using gaseous_server.Classes.Metadata;
using gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes;

namespace gaseous_server.Controllers.v1_1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0", Deprecated = true)]
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
            DataTable data = await db.ExecuteCMDAsync(sql, dbDict);

            if (IsMediaGroup == false)
            {
                Logging.LogKey(Logging.LogType.Information, "process.save_state", "savestate.saved_state_for_rom", null, new string[] { RomId.ToString(), uploadState.StateByteArrayBase64.Length.ToString(), CompressedState.Length.ToString() });
            }
            else
            {
                Logging.LogKey(Logging.LogType.Information, "process.save_state", "savestate.saved_state_for_media_group", null, new string[] { RomId.ToString(), uploadState.StateByteArrayBase64.Length.ToString(), CompressedState.Length.ToString() });
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
            DataTable data = await db.ExecuteCMDAsync(sql, dbDict);

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
            DataTable data = await db.ExecuteCMDAsync(sql, dbDict);

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
            DataTable data = await db.ExecuteCMDAsync(sql, dbDict);

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
            DataTable data = await db.ExecuteCMDAsync(sql, dbDict);

            if (data.Rows.Count == 0)
            {
                // invalid match - return not found
                return NotFound();
            }
            else
            {
                // get rom data
                string romName = "";
                string romMd5 = "";
                string romSha1 = "";
                if (IsMediaGroup == false)
                {
                    Roms.GameRomItem romItem = await Roms.GetRom(RomId);
                    romName = romItem.Name;
                    romMd5 = romItem.Md5;
                    romSha1 = romItem.Sha1;
                }
                else
                {
                    RomMediaGroup.GameRomMediaGroupItem mediaGroupItem = await RomMediaGroup.GetMediaGroupAsync(RomId);
                    Game game = await Games.GetGame(Config.MetadataConfiguration.DefaultMetadataSource, mediaGroupItem.GameId);
                    Classes.HashObject HashObject = new Classes.HashObject(Path.Combine(Config.LibraryConfiguration.LibraryMediaGroupDirectory, mediaGroupItem.Id.ToString() + ".zip"));
                    romName = game.Name;
                    romMd5 = HashObject.md5hash;
                    romSha1 = HashObject.sha1hash;
                }

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
                string filename = ((DateTime)data.Rows[0]["StateDateTime"]).ToString("yyyy-MM-ddTHH-mm-ss") + "-" + Path.GetFileNameWithoutExtension(romName);


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
                         { "Name", romName },
                         { "StateDateTime", data.Rows[0]["StateDateTime"] },
                         { "StateName", data.Rows[0]["Name"] }
                     };
                    if ((int)data.Rows[0]["IsMediaGroup"] == 0)
                    {
                        RomInfo.Add("MD5", romMd5);
                        RomInfo.Add("SHA1", romSha1);
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
                        // check if value is dbnull
                        if (data.Rows[0]["Screenshot"] != DBNull.Value)
                        {
                            Attachments.Add(new Dictionary<string, object>
                             {
                                 { "Name", "screenshot.jpg" },
                                 { "Body", (byte[])data.Rows[0]["Screenshot"] }
                             });
                        }
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
                                    await originalFileStream.CopyToAsync(zipEntryStream);
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

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpPost]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [RequestSizeLimit(long.MaxValue)]
        [Consumes("multipart/form-data")]
        [DisableRequestSizeLimit, RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue, ValueLengthLimit = int.MaxValue)]
        [Route("Upload")]
        public async Task<ActionResult> UploadStateDataAsync(IFormFile file, long RomId = 0, bool IsMediaGroup = false)
        {
            // get user
            var user = await _userManager.GetUserAsync(User);

            if (file.Length > 0)
            {
                MemoryStream fileContent = new MemoryStream();
                await file.CopyToAsync(fileContent);

                // test if file is a zip file
                try
                {
                    using (var zipArchive = new ZipArchive(fileContent, ZipArchiveMode.Read, false))
                    {
                        foreach (var entry in zipArchive.Entries)
                        {
                            if (entry.FullName == "rominfo.json")
                            {
                                using (var stream = entry.Open())
                                using (var reader = new StreamReader(stream))
                                {
                                    string RomInfoString = await reader.ReadToEndAsync();
                                    Dictionary<string, object> RomInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(RomInfoString);

                                    // get rom data
                                    Roms.GameRomItem romItem;

                                    try
                                    {
                                        romItem = await Roms.GetRom((string)RomInfo["MD5"]);
                                    }
                                    catch (Roms.InvalidRomHash)
                                    {
                                        return NotFound();
                                    }

                                    // get state data
                                    byte[] StateData = null;
                                    byte[] ScreenshotData = null;
                                    string StateName = RomInfo["StateName"].ToString();
                                    DateTime StateDateTime = DateTime.Parse(RomInfo["StateDateTime"].ToString());
                                    IsMediaGroup = RomInfo["Type"].ToString() == "Media Group" ? true : false;

                                    if (zipArchive.GetEntry("savestate.state") != null)
                                    {
                                        using (var stateStream = zipArchive.GetEntry("savestate.state").Open())
                                        using (var stateReader = new MemoryStream())
                                        {
                                            stateStream.CopyTo(stateReader);
                                            StateData = stateReader.ToArray();
                                        }
                                    }
                                    if (zipArchive.GetEntry("screenshot.jpg") != null)
                                    {
                                        using (var screenshotStream = zipArchive.GetEntry("screenshot.jpg").Open())
                                        using (var screenshotReader = new MemoryStream())
                                        {
                                            screenshotStream.CopyTo(screenshotReader);
                                            ScreenshotData = screenshotReader.ToArray();
                                        }
                                    }

                                    // save state
                                    Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
                                    string sql = "INSERT INTO GameState (UserId, RomId, IsMediaGroup, StateDateTime, Name, Screenshot, State, Zipped) VALUES (@userid, @romid, @ismediagroup, @statedatetime, @name, @screenshot, @state, @zipped); SELECT LAST_INSERT_ID();";
                                    Dictionary<string, object> dbDict = new Dictionary<string, object>
                                     {
                                         { "userid", user.Id },
                                         { "romid", romItem.Id },
                                         { "ismediagroup", IsMediaGroup },
                                         { "statedatetime", StateDateTime },
                                         { "name", StateName },
                                         { "screenshot", ScreenshotData },
                                         { "state", Common.Compress(StateData) },
                                         { "zipped", true }
                                     };
                                    DataTable data = await db.ExecuteCMDAsync(sql, dbDict);

                                    RomInfo.Add("RomId", romItem.Id);
                                    RomInfo.Add("Management", "Managed");
                                    return Ok(RomInfo);
                                }
                            }
                        }
                    }

                    return BadRequest("File is not a valid Gaseous state file.");
                }
                catch
                {
                    // not a zip file
                    if (RomId != 0)
                    {
                        // get rom data
                        Roms.GameRomItem romItem;

                        try
                        {
                            romItem = await Roms.GetRom(RomId);
                        }
                        catch (Roms.InvalidRomHash)
                        {
                            return NotFound();
                        }

                        // save state
                        Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
                        string sql = "INSERT INTO GameState (UserId, RomId, IsMediaGroup, StateDateTime, Name, Screenshot, State, Zipped) VALUES (@userid, @romid, @ismediagroup, @statedatetime, @name, @screenshot, @state, @zipped); SELECT LAST_INSERT_ID();";
                        Dictionary<string, object> dbDict = new Dictionary<string, object>
                         {
                             { "userid", user.Id },
                             { "romid", RomId },
                             { "ismediagroup", IsMediaGroup },
                             { "statedatetime", DateTime.UtcNow },
                             { "name", "" },
                             { "screenshot", null },
                             { "state", Common.Compress(fileContent.ToArray()) },
                             { "zipped", true }
                         };
                        DataTable data = await db.ExecuteCMDAsync(sql, dbDict);

                        return Ok(new Dictionary<string, object>
                         {
                             { "RomId", RomId },
                             { "Management", "Unmanaged" }
                         });
                    }
                    else
                    {
                        return BadRequest("No rom id provided.");
                    }
                }
            }
            else
            {
                return BadRequest("File is empty.");
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