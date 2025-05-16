using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using gaseous_server.Classes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Authentication;
using Microsoft.AspNetCore.Identity;
using gaseous_server.Models;
using gaseous_server.Classes.Metadata;
using HasheousClient.Models.Metadata.IGDB;

namespace gaseous_server.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0", Deprecated = true)]
    [ApiVersion("1.1")]
    [Authorize]
    public class BiosController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public BiosController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<List<Classes.Bios.BiosItem>> GetBios()
        {
            return await Classes.Bios.GetBios();
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("{PlatformId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<List<Classes.Bios.BiosItem>> GetBios(long PlatformId, bool AvailableOnly = true)
        {
            return await Classes.Bios.GetBios(PlatformId, AvailableOnly);
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpHead]
        [Route("zip/{PlatformId}")]
        [Route("zip/{PlatformId}/{GameId}")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetBiosCompressedAsync(long PlatformId, long GameId = -1, bool filtered = false)
        {
            try
            {
                Platform platform = await Platforms.GetPlatform(PlatformId);
                PlatformMapping.PlatformMapItem platformMap = await PlatformMapping.GetPlatformMap(PlatformId);

                List<string> biosHashes = new List<string>();

                if (GameId == -1 || filtered == false)
                {
                    // get all bios files for selected platform
                    biosHashes.AddRange(platformMap.EnabledBIOSHashes);
                }
                else
                {
                    // get user platform map
                    var user = await _userManager.GetUserAsync(User);

                    PlatformMapping platformMapping = new PlatformMapping();
                    PlatformMapping.PlatformMapItem userPlatformMap = await platformMapping.GetUserPlatformMap(user.Id, PlatformId, GameId);

                    biosHashes.AddRange(userPlatformMap.EnabledBIOSHashes);
                }

                // build zip file
                string tempFile = Path.GetTempFileName();

                using (FileStream zipFile = System.IO.File.Create(tempFile))
                using (var zipArchive = new ZipArchive(zipFile, ZipArchiveMode.Create))
                {
                    foreach (string hash in biosHashes)
                    {
                        // get the bios data for the hash
                        foreach (PlatformMapping.PlatformMapItem.EmulatorBiosItem bios in platformMap.Bios)
                        {
                            if (bios.hash == hash)
                            {
                                // add the bios file to the zip
                                string biosFilePath = Path.Combine(Config.LibraryConfiguration.LibraryFirmwareDirectory, hash + ".bios");
                                if (System.IO.File.Exists(biosFilePath))
                                {
                                    zipArchive.CreateEntryFromFile(biosFilePath, bios.filename);
                                }
                            }
                        }
                    }
                }

                var stream = new FileStream(tempFile, FileMode.Open);
                return File(stream, "application/zip", platform.Slug + ".zip");
            }
            catch
            {
                return NotFound();
            }
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpHead]
        [Route("{PlatformId}/{BiosName}")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> BiosFile(long PlatformId, string BiosName)
        {
            try
            {
                foreach (Classes.Bios.BiosItem biosItem in await Classes.Bios.GetBios(PlatformId, true))
                {
                    if (biosItem.filename == BiosName)
                    {
                        if (System.IO.File.Exists(biosItem.biosPath))
                        {
                            string filename = Path.GetFileName(biosItem.biosPath);
                            string filepath = biosItem.biosPath;
                            byte[] filedata = System.IO.File.ReadAllBytes(filepath);
                            string contentType = "application/octet-stream";

                            var cd = new System.Net.Mime.ContentDisposition
                            {
                                FileName = filename,
                                Inline = false,
                            };

                            Response.Headers.Add("Content-Disposition", cd.ToString());
                            Response.Headers.Add("Cache-Control", "public, max-age=604800");

                            return File(filedata, contentType);
                        }
                        else
                        {
                            return NotFound();
                        }
                    }
                }

                return NotFound();
            }
            catch
            {
                return NotFound();
            }
        }
    }
}

