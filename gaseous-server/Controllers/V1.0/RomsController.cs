﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using gaseous_server.Classes;
using gaseous_server.Classes.Metadata;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Scripting;
using static gaseous_server.Classes.Metadata.AgeRatings;
using Asp.Versioning;
using HasheousClient.Models.Metadata.IGDB;

namespace gaseous_server.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0", Deprecated = true)]
    [ApiVersion("1.1")]
    [Authorize]
    [ApiController]
    public class RomsController : Controller
    {
        static bool uploadInProgress = false;
        static DateTime uploadStartTime = DateTime.Now;

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpPost]
        [Authorize(Roles = "Admin,Gamer")]
        [ProducesResponseType(typeof(List<IFormFile>), StatusCodes.Status200OK)]
        [RequestSizeLimit(long.MaxValue)]
        [Consumes("multipart/form-data")]
        [DisableRequestSizeLimit, RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue, ValueLengthLimit = int.MaxValue)]
        public async Task<IActionResult> UploadRom(IFormFile file, long? OverridePlatformId = null)
        {
            Guid sessionid = Guid.NewGuid();

            string workPath = Path.Combine(Config.LibraryConfiguration.LibraryUploadDirectory, sessionid.ToString());

            if (file.Length > 0)
            {
                Guid FileId = Guid.NewGuid();

                string filePath = Path.Combine(workPath, Path.GetFileName(file.FileName));

                if (!Directory.Exists(workPath))
                {
                    Directory.CreateDirectory(workPath);
                }

                Dictionary<string, object> UploadedFile = new Dictionary<string, object>();

                using (var stream = System.IO.File.Create(filePath))
                {
                    await file.CopyToAsync(stream);

                    UploadedFile.Add("id", FileId.ToString());
                    UploadedFile.Add("originalname", Path.GetFileName(file.FileName));
                    UploadedFile.Add("fullpath", filePath);
                }

                // get override platform if specified
                Platform? OverridePlatform = null;
                if (OverridePlatformId != null)
                {
                    OverridePlatform = Platforms.GetPlatform((long)OverridePlatformId);
                }

                // Process uploaded file
                Classes.ImportGame uploadImport = new ImportGame();
                // wait until uploadInProgress is false
                while (uploadInProgress)
                {
                    await Task.Delay(1000);

                    // escape if upload is taking too long
                    if (DateTime.Now.Subtract(uploadStartTime).TotalSeconds > 60)
                    {
                        uploadInProgress = false;
                        break;
                    }
                }
                uploadInProgress = true;
                uploadStartTime = DateTime.Now;

                Dictionary<string, object> RetVal = uploadImport.ImportGameFile((string)UploadedFile["fullpath"], OverridePlatform);
                uploadInProgress = false;
                switch (RetVal["type"])
                {
                    case "rom":
                        if (RetVal["status"] == "imported")
                        {
                            gaseous_server.Models.Game? game = (gaseous_server.Models.Game)RetVal["game"];
                            if (game == null || game.Id == null)
                            {
                                RetVal["game"] = Games.GetGame(HasheousClient.Models.MetadataSources.IGDB, 0);
                            }
                        }
                        break;

                }

                if (Directory.Exists(workPath))
                {
                    Directory.Delete(workPath, true);
                }

                return Ok(RetVal);
            }

            return Ok();
        }
    }
}