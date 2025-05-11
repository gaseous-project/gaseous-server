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

namespace gaseous_server.Controllers.v1_1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0", Deprecated = true)]
    [ApiVersion("1.1")]
    [ApiController]
    public class SaveFileController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public SaveFileController(
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
        [Route("{core}/{ismediagroup}/{romid}")]
        public async Task<IActionResult> SaveFile(
            [FromRoute] string core,
            [FromRoute] bool ismediagroup,
            [FromRoute] long romid,
            [FromBody] Models.UploadSaveModel gameSaveItem
        )
        {
            var user = await _userManager.GetUserAsync(User);

            // compress the save data
            byte[] CompressedData = Common.Compress(gameSaveItem.SaveByteArray);

            // generate an MD5 hash of the compressed data
            string md5Hash;
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                md5Hash = BitConverter.ToString(md5.ComputeHash(CompressedData)).Replace("-", "").ToLowerInvariant();
            }

            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql;
            Dictionary<string, object> parameters = new Dictionary<string, object>();

            // check if the save file is different to the last recorded file
            sql = "SELECT `MD5` FROM GameSaves WHERE UserId = @userid AND RomId = @romid AND IsMediaGroup = @ismediagroup AND CoreName = @core ORDER BY TimeStamp DESC LIMIT 1";
            parameters.Add("@userid", user.Id);
            parameters.Add("@romid", romid);
            parameters.Add("@ismediagroup", ismediagroup);
            parameters.Add("@core", core);
            DataTable dt = db.ExecuteCMD(sql, parameters);

            if (dt.Rows.Count > 0)
            {
                string lastMD5 = dt.Rows[0]["MD5"].ToString();
                if (lastMD5 == md5Hash)
                {
                    // if the save file is the same as the last recorded file, just return Ok
                    return Ok();
                }
            }

            // if the save file does not exist, insert it into the database
            sql = "INSERT INTO GameSaves (`UserId`, `RomId`, `IsMediaGroup`, `CoreName`, `MD5`, `TimeStamp`, `File`) VALUES (@userid, @romid, @ismediagroup, @core, @md5hash, @timestamp, @savedata)";
            parameters.Clear();
            parameters.Add("@userid", user.Id);
            parameters.Add("@romid", romid);
            parameters.Add("@ismediagroup", ismediagroup);
            parameters.Add("@core", core);
            parameters.Add("@md5hash", md5Hash);
            parameters.Add("@timestamp", DateTime.UtcNow);
            parameters.Add("@savedata", CompressedData);

            db.ExecuteCMD(sql, parameters);

            // keep only the 20 most recent save files
            sql = "DELETE FROM GameSaves WHERE UserId = @userid AND RomId = @romid AND IsMediaGroup = @ismediagroup AND CoreName = @core AND Id NOT IN (SELECT Id FROM (SELECT Id FROM GameSaves WHERE UserId = @userid AND RomId = @romid AND IsMediaGroup = @ismediagroup AND CoreName = @core ORDER BY TimeStamp DESC LIMIT 20) AS t);";
            db.ExecuteNonQuery(sql, parameters);

            return Ok();
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(List<Models.GameSaveItem>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("{core}/{ismediagroup}/{romid}")]
        public async Task<IActionResult> GetSaveFiles(
            [FromRoute] string core,
            [FromRoute] bool ismediagroup,
            [FromRoute] long romid
        )
        {
            var user = await _userManager.GetUserAsync(User);

            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql;
            Dictionary<string, object> parameters = new Dictionary<string, object>();

            sql = "SELECT `Id`, `TimeStamp` FROM GameSaves WHERE UserId = @userid AND RomId = @romid AND IsMediaGroup = @ismediagroup AND CoreName = @core";
            parameters.Add("@userid", user.Id);
            parameters.Add("@romid", romid);
            parameters.Add("@ismediagroup", ismediagroup);
            parameters.Add("@core", core);

            DataTable dt = db.ExecuteCMD(sql, parameters);

            List<Models.GameSaveItem> gameSaveItems = new List<Models.GameSaveItem>();

            foreach (DataRow row in dt.Rows)
            {
                Models.GameSaveItem gameSaveItem = new Models.GameSaveItem();
                gameSaveItem.Id = Convert.ToInt64(row["Id"]);
                gameSaveItem.SaveTime = Convert.ToDateTime(row["TimeStamp"]);
                gameSaveItems.Add(gameSaveItem);
            }

            return Ok(gameSaveItems);
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(Models.GameSaveItem), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Route("{core}/{ismediagroup}/{romid}/{index}")]
        public async Task<IActionResult> GetSaveFileInfo(
            [FromRoute] string core,
            [FromRoute] bool ismediagroup,
            [FromRoute] long romid,
            [FromRoute] string index
        )
        {
            var user = await _userManager.GetUserAsync(User);

            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql;
            Dictionary<string, object> parameters = new Dictionary<string, object>();

            if (index == "latest")
            {
                sql = "SELECT `Id`, `TimeStamp`, `File` FROM GameSaves WHERE UserId = @userid AND RomId = @romid AND IsMediaGroup = @ismediagroup AND CoreName = @core ORDER BY TimeStamp DESC LIMIT 1";
            }
            else
            {
                // check if the index is a valid number
                if (!long.TryParse(index, out long indexValue))
                {
                    return BadRequest("Invalid index");
                }
                sql = "SELECT `Id`, `TimeStamp`, `File` FROM GameSaves WHERE UserId = @userid AND RomId = @romid AND IsMediaGroup = @ismediagroup AND CoreName = @core AND Id = @index";
            }
            parameters.Add("@userid", user.Id);
            parameters.Add("@romid", romid);
            parameters.Add("@ismediagroup", ismediagroup);
            parameters.Add("@core", core);
            parameters.Add("@index", index);

            DataTable dt = db.ExecuteCMD(sql, parameters);

            if (dt.Rows.Count == 0)
                return NotFound();

            Models.GameSaveItem gameSaveItem = new Models.GameSaveItem();
            gameSaveItem.Id = Convert.ToInt64(dt.Rows[0]["Id"]);
            gameSaveItem.SaveTime = Convert.ToDateTime(dt.Rows[0]["TimeStamp"]);

            // return the save data
            return Ok(gameSaveItem);
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Route("{core}/{ismediagroup}/{romid}/{index}/data")]
        public async Task<IActionResult> GetSaveFileData(
            [FromRoute] string core,
            [FromRoute] bool ismediagroup,
            [FromRoute] long romid,
            [FromRoute] string index,
            [FromQuery] string? format = null
        )
        {
            var user = await _userManager.GetUserAsync(User);

            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql;
            Dictionary<string, object> parameters = new Dictionary<string, object>();

            if (index == "latest")
            {
                sql = "SELECT `Id`, `TimeStamp`, `File` FROM GameSaves WHERE UserId = @userid AND RomId = @romid AND IsMediaGroup = @ismediagroup AND CoreName = @core ORDER BY TimeStamp DESC LIMIT 1";
            }
            else
            {
                // check if the index is a valid number
                if (!long.TryParse(index, out long indexValue))
                {
                    return BadRequest("Invalid index");
                }
                sql = "SELECT `Id`, `TimeStamp`, `File` FROM GameSaves WHERE UserId = @userid AND RomId = @romid AND IsMediaGroup = @ismediagroup AND CoreName = @core AND Id = @index";
            }
            parameters.Add("@userid", user.Id);
            parameters.Add("@romid", romid);
            parameters.Add("@ismediagroup", ismediagroup);
            parameters.Add("@core", core);
            parameters.Add("@index", index);

            DataTable dt = db.ExecuteCMD(sql, parameters);

            if (dt.Rows.Count == 0)
                return NotFound();

            byte[] saveData = (byte[])dt.Rows[0]["File"];

            // decompress the save data
            byte[] decompressedData = Common.Decompress(saveData);

            // return the save data
            if (format == "base64")
            {
                string base64String = Convert.ToBase64String(decompressedData);
                return Ok(base64String);
            }
            else if (format == "raw")
            {
                return File(decompressedData, "application/octet-stream");
            }
            else
            {
                return BadRequest("Invalid format");
            }
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpDelete]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Route("{core}/{ismediagroup}/{romid}")]
        public async Task<IActionResult> DeleteSaveFile(
            [FromRoute] string core,
            [FromRoute] bool ismediagroup,
            [FromRoute] long romid
        )
        {
            var user = await _userManager.GetUserAsync(User);

            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql;
            Dictionary<string, object> parameters = new Dictionary<string, object>();

            sql = "DELETE FROM GameSaves WHERE UserId = @userid AND RomId = @romid AND IsMediaGroup = @ismediagroup AND CoreName = @core;";
            parameters.Add("@userid", user.Id);
            parameters.Add("@romid", romid);
            parameters.Add("@ismediagroup", ismediagroup);
            parameters.Add("@core", core);

            db.ExecuteCMD(sql, parameters);

            return Ok();
        }
    }
}