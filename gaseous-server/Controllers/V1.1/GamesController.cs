using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Authentication;
using gaseous_server.Classes;
using gaseous_server.Classes.Metadata;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Scripting;
using static gaseous_server.Classes.Metadata.AgeRatings;
using Asp.Versioning;
using Humanizer;
using gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes;
using gaseous_server.Models;
using gaseous_server.Classes.Plugins.MetadataProviders;

namespace gaseous_server.Controllers.v1_1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.1")]
    [ApiController]
    [Authorize]
    public class GamesController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public GamesController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [MapToApiVersion("1.1")]
        [HttpPost]
        [ProducesResponseType(typeof(GameReturnPackage), StatusCodes.Status200OK)]
        public async Task<IActionResult> Game_v1_1(GameSearchModel model, int pageNumber = 0, int pageSize = 0, bool returnSummary = true, bool returnGames = true)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user != null)
            {
                // apply security profile filtering
                if (model.GameAgeRating == null)
                {
                    model.GameAgeRating = new GameSearchModel.GameAgeRatingItem();
                }
                if (model.GameAgeRating.AgeGroupings == null)
                {
                    model.GameAgeRating.AgeGroupings = new List<AgeGroups.AgeRestrictionGroupings>();
                }
                if (model.GameAgeRating.IncludeUnrated == false)
                {
                    if (model.GameAgeRating.AgeGroupings.Count == 0)
                    {
                        model.GameAgeRating.AgeGroupings.Add(AgeGroups.AgeRestrictionGroupings.Adult);
                        model.GameAgeRating.AgeGroupings.Add(AgeGroups.AgeRestrictionGroupings.Mature);
                        model.GameAgeRating.AgeGroupings.Add(AgeGroups.AgeRestrictionGroupings.Teen);
                        model.GameAgeRating.AgeGroupings.Add(AgeGroups.AgeRestrictionGroupings.Child);
                        model.GameAgeRating.IncludeUnrated = true;
                    }
                }
                List<AgeGroups.AgeRestrictionGroupings> RemoveAgeGroups = new List<AgeGroups.AgeRestrictionGroupings>();
                switch (user.SecurityProfile.AgeRestrictionPolicy.MaximumAgeRestriction)
                {
                    case AgeGroups.AgeRestrictionGroupings.Adult:
                        break;
                    case AgeGroups.AgeRestrictionGroupings.Mature:
                        RemoveAgeGroups.Add(AgeGroups.AgeRestrictionGroupings.Adult);
                        break;
                    case AgeGroups.AgeRestrictionGroupings.Teen:
                        RemoveAgeGroups.Add(AgeGroups.AgeRestrictionGroupings.Adult);
                        RemoveAgeGroups.Add(AgeGroups.AgeRestrictionGroupings.Mature);
                        break;
                    case AgeGroups.AgeRestrictionGroupings.Child:
                        RemoveAgeGroups.Add(AgeGroups.AgeRestrictionGroupings.Adult);
                        RemoveAgeGroups.Add(AgeGroups.AgeRestrictionGroupings.Mature);
                        RemoveAgeGroups.Add(AgeGroups.AgeRestrictionGroupings.Teen);
                        break;
                }
                foreach (AgeGroups.AgeRestrictionGroupings RemoveAgeGroup in RemoveAgeGroups)
                {
                    if (model.GameAgeRating.AgeGroupings.Contains(RemoveAgeGroup))
                    {
                        model.GameAgeRating.AgeGroupings.Remove(RemoveAgeGroup);
                    }
                }
                if (user.SecurityProfile.AgeRestrictionPolicy.IncludeUnrated == false)
                {
                    model.GameAgeRating.IncludeUnrated = false;
                }

                return Ok(await GetGames(model, user, pageNumber, pageSize, returnSummary, returnGames));
            }
            else
            {
                return Unauthorized();
            }
        }

        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("{MetadataMapId}/Related")]
        [ProducesResponseType(typeof(GameReturnPackage), StatusCodes.Status200OK)]
        public async Task<IActionResult> GameRelated(long MetadataMapId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user != null)
            {
                string IncludeUnrated = "";
                if (user.SecurityProfile.AgeRestrictionPolicy.IncludeUnrated == true)
                {
                    IncludeUnrated = " OR view_Games.AgeGroupId IS NULL";
                }

                Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
                string sql = "SELECT view_Games.Id, view_Games.AgeGroupId, Relation_Game_SimilarGames.SimilarGamesId FROM view_Games JOIN Relation_Game_SimilarGames ON view_Games.Id = Relation_Game_SimilarGames.GameId AND view_Games.GameIdType = Relation_Game_SimilarGames.GameSourceId AND Relation_Game_SimilarGames.SimilarGamesId IN (SELECT Id FROM view_Games) WHERE view_Games.Id = @id AND (view_Games.AgeGroupId <= @agegroupid" + IncludeUnrated + ")";
                Dictionary<string, object> dbDict = new Dictionary<string, object>();
                dbDict.Add("id", MetadataMapId);
                dbDict.Add("agegroupid", (int)user.SecurityProfile.AgeRestrictionPolicy.MaximumAgeRestriction);

                List<Game> RetVal = new List<Game>();

                DataTable dbResponse = await db.ExecuteCMDAsync(sql, dbDict);

                foreach (DataRow dr in dbResponse.Rows)
                {
                    MetadataMap.MetadataMapItem metadataMap = (await Classes.MetadataManagement.GetMetadataMap(MetadataMapId)).PreferredMetadataMapItem;
                    RetVal.Add(await Classes.Metadata.Games.GetGame(metadataMap.SourceType, (long)dr["SimilarGamesId"]));
                }

                GameReturnPackage gameReturn = new GameReturnPackage(RetVal.Count, RetVal);

                return Ok(gameReturn);
            }
            else
            {
                return Unauthorized();
            }
        }

        private string fileSystemBasePath(string userId, long MetadataMapId, long? RomId, long? RomGroupId)
        {
            string romType = "";
            string pathId = "";
            if ((RomId == null && RomGroupId == null) || (RomId != null && RomGroupId != null))
            {
                throw new ArgumentException("Must provide either RomId or RomGroupId, but not both.");
            }

            if (RomId != null)
            {
                romType = "ROM";
                pathId = RomId.Value.ToString();
            }
            else
            {
                romType = "ROM Group";
                pathId = RomGroupId.Value.ToString();
            }
            return Path.Combine(Config.LibraryConfiguration.LibraryFileSystemDirectory, romType, pathId, userId);
        }

        /// <summary>
        /// Gets the file system data for a specified ROM or ROM group within a metadata map. This is used for syncing save data back to the server from clients.
        /// This method is effectively getting a directory listing.
        /// </summary>
        /// <param name="MetadataMapId">The ID of the metadata map the ROM or ROM group belongs to.</param>
        /// <param name="RomId">The ID of the ROM to get the file system data for. Optional if RomGroupId is provided.</param>
        /// <param name="RomGroupId">The ID of the ROM group to get the file system data for. Optional if RomId is provided.</param>
        /// <returns>A dictionary where each key is the file name and path in unix format, and the value is a list of strings representing the file system data - size, date created, date modified, etc.</returns>
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("{MetadataMapId}/roms/{RomId}/filesystem")]
        [Route("{MetadataMapId}/romgroup/{RomGroupId}/filesystem")]
        [ProducesResponseType(typeof(Dictionary<string, Dictionary<string, string>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GameFilesystem(long MetadataMapId, long? RomId, long? RomGroupId)
        {
            // resolves to a local path for the specified ROM or ROM group
            // response is a dictionary - each dictionary key is the file name and path in unix format, and the value is a dictionary representing the file system data - size, date created, date modified, etc.

            var user = await _userManager.GetUserAsync(User);

            if (user != null)
            {
                if ((RomId == null && RomGroupId == null) || (RomId != null && RomGroupId != null))
                {
                    return BadRequest("Must provide either RomId or RomGroupId, but not both.");
                }

                Dictionary<string, Dictionary<string, string>> fileSystem;

                string basePath = fileSystemBasePath(user.Id, MetadataMapId, RomId, RomGroupId);

                if (Directory.Exists(basePath))
                {
                    fileSystem = new Dictionary<string, Dictionary<string, string>>();

                    foreach (string filePath in Directory.GetFiles(basePath, "*", SearchOption.AllDirectories))
                    {
                        FileInfo fileInfo = new FileInfo(filePath);
                        string relativePath = filePath.Substring(basePath.Length)
                            .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                            .Replace(Path.DirectorySeparatorChar, '/')
                            .Replace(Path.AltDirectorySeparatorChar, '/');
                        fileSystem[relativePath] = new Dictionary<string, string>
                        {
                            { "Size", fileInfo.Length.ToString() },
                            { "CreationTimeUtc", fileInfo.CreationTimeUtc.ToString("o") },
                            { "LastWriteTimeUtc", fileInfo.LastWriteTimeUtc.ToString("o") }
                        };
                    }
                }
                else
                {
                    // No persisted filesystem exists yet for this user/game; return an empty listing.
                    fileSystem = new Dictionary<string, Dictionary<string, string>>();
                }

                return Ok(fileSystem);
            }
            else
            {
                return Unauthorized();
            }
        }

        /// <summary>
        /// Gets a specific file from the file system for a specified ROM or ROM group within a metadata map. This is used for syncing save data back to the server from clients.
        /// </summary>
        /// <param name="MetadataMapId">The ID of the metadata map the ROM or ROM group belongs to.</param>
        /// <param name="RomId">The ID of the ROM to get the file for. Optional if RomGroupId is provided.</param>
        /// <param name="RomGroupId">The ID of the ROM group to get the file for. Optional if RomId is provided.</param>
        /// <param name="filePath">The relative path of the file to get within the ROM or ROM group directory.</param>
        /// <returns>The specified file as a byte array, with a content type of application/octet-stream. If the file is not found, returns a 404 Not Found response.</returns>
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("{MetadataMapId}/roms/{RomId}/filesystem/{*filePath}")]
        [Route("{MetadataMapId}/romgroup/{RomGroupId}/filesystem/{*filePath}")]
        [ProducesResponseType(typeof(Dictionary<string, List<string>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GameFilesystemGetFile(long MetadataMapId, long? RomId, long? RomGroupId, string filePath)
        {
            // remove any http encoding from the file path
            filePath = Uri.UnescapeDataString(filePath);

            // early abort if the filePath contains any invalid characters or attempts to traverse up the directory structure
            if (filePath.Contains("..") || filePath.Contains(":") || filePath.Contains("|") || filePath.Contains("?") || filePath.Contains("*") || filePath.Contains("\"") || filePath.Contains("<") || filePath.Contains(">"))
            {
                return BadRequest("Invalid file path.");
            }

            var user = await _userManager.GetUserAsync(User);

            if (user != null)
            {
                if ((RomId == null && RomGroupId == null) || (RomId != null && RomGroupId != null))
                {
                    return BadRequest("Must provide either RomId or RomGroupId, but not both.");
                }

                string basePath = fileSystemBasePath(user.Id, MetadataMapId, RomId, RomGroupId);

                string[] pathParts = filePath
                    .Replace('\\', '/')
                    .Split('/', StringSplitOptions.RemoveEmptyEntries)
                    .Where(p => p != "." && p != "..")
                    .ToArray();

                if (pathParts.Length == 0)
                {
                    return BadRequest("Invalid file path.");
                }

                string normalizedFilePath = Path.Combine(pathParts);
                string fullBasePath = Path.GetFullPath(basePath);
                string fullPath = Path.GetFullPath(Path.Combine(basePath, normalizedFilePath));

                if (!fullPath.StartsWith(fullBasePath + Path.DirectorySeparatorChar) && !string.Equals(fullPath, fullBasePath, StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest("Invalid file path.");
                }

                if (System.IO.File.Exists(fullPath))
                {
                    return PhysicalFile(fullPath, "application/octet-stream", Path.GetFileName(fullPath));
                }
                else
                {
                    return NotFound("File not found for the specified ROM or ROM group.");
                }
            }
            else
            {
                return Unauthorized();
            }
        }

        /// <summary>
        /// Reconciles server-side filesystem files against a provided set of files that still exist in the emscripten filesystem.
        /// Files not present in the provided list will be deleted.
        /// </summary>
        /// <param name="MetadataMapId">The ID of the metadata map the ROM or ROM group belongs to.</param>
        /// <param name="RomId">The ID of the ROM to reconcile files for. Optional if RomGroupId is provided.</param>
        /// <param name="RomGroupId">The ID of the ROM group to reconcile files for. Optional if RomId is provided.</param>
        /// <param name="model">Request body containing the current emscripten file list.</param>
        /// <returns>An IActionResult containing deleted file count and paths.</returns>
        [MapToApiVersion("1.1")]
        [HttpPost]
        [Route("{MetadataMapId}/roms/{RomId}/filesystem/reconcile")]
        [Route("{MetadataMapId}/romgroup/{RomGroupId}/filesystem/reconcile")]
        [Consumes("application/json")]
        public async Task<IActionResult> GameFilesystemReconcile(long MetadataMapId, long? RomId, long? RomGroupId, [FromBody] FilesystemReconcileModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Unauthorized();
            }

            if ((RomId == null && RomGroupId == null) || (RomId != null && RomGroupId != null))
            {
                return BadRequest("Must provide either RomId or RomGroupId, but not both.");
            }

            string basePath = fileSystemBasePath(user.Id, MetadataMapId, RomId, RomGroupId);
            if (!Directory.Exists(basePath))
            {
                return Ok(new
                {
                    DeletedCount = 0,
                    DeletedFiles = Array.Empty<string>()
                });
            }

            try
            {
                HashSet<string> expectedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                IEnumerable<string> incomingFiles = model?.FileNames ?? Enumerable.Empty<string>();
                foreach (string incomingFile in incomingFiles)
                {
                    if (string.IsNullOrWhiteSpace(incomingFile))
                    {
                        continue;
                    }

                    string[] pathParts = incomingFile
                        .Replace('\\', '/')
                        .Split('/', StringSplitOptions.RemoveEmptyEntries)
                        .Where(p => p != "." && p != "..")
                        .ToArray();

                    if (pathParts.Length == 0)
                    {
                        continue;
                    }

                    string normalizedRelativePath = Path.Combine(pathParts)
                        .Replace(Path.DirectorySeparatorChar, '/')
                        .Replace(Path.AltDirectorySeparatorChar, '/');

                    expectedFiles.Add(normalizedRelativePath);
                }

                string fullBasePath = Path.GetFullPath(basePath);
                List<string> deletedFiles = new List<string>();

                foreach (string existingFilePath in Directory.GetFiles(basePath, "*", SearchOption.AllDirectories))
                {
                    string fullExistingFilePath = Path.GetFullPath(existingFilePath);
                    if (!fullExistingFilePath.StartsWith(fullBasePath + Path.DirectorySeparatorChar) && !string.Equals(fullExistingFilePath, fullBasePath, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string relativePath = fullExistingFilePath.Substring(fullBasePath.Length)
                        .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                        .Replace(Path.DirectorySeparatorChar, '/')
                        .Replace(Path.AltDirectorySeparatorChar, '/');

                    if (expectedFiles.Contains(relativePath))
                    {
                        continue;
                    }

                    System.IO.File.Delete(fullExistingFilePath);
                    deletedFiles.Add(relativePath);
                }

                return Ok(new
                {
                    DeletedCount = deletedFiles.Count,
                    DeletedFiles = deletedFiles
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error reconciling file system data: " + ex.Message);
            }
        }

        /// <summary>
        /// Accepts a single file upload with metadata for a specified ROM or ROM group and saves it to the server. This is used for syncing save data back to the server from clients.
        /// The file data is expected to be multipart form data with the following properties:
        /// - FileName: The name of the file being uploaded, including the relative path from the base directory for the ROM or ROM group (e.g. "save1.sav" or "saves/save1.sav").
        /// - FileContent: The file content uploaded as a form file.
        /// - LastModified: (Optional) The last modified date of the file being uploaded, in ISO 8601 format. If provided, this will be used to set the last modified date of the saved file on the server.
        /// The file will be saved to a directory on the server based on the metadata map ID, ROM or ROM group type, and ROM or ROM group ID. For example: "Library/File System/{MetadataMapId}/ROM/12345/save1.sav".
        /// </summary>
        /// <param name="MetadataMapId">The ID of the metadata map the ROM or ROM group belongs to.</param>
        /// <param name="RomId">The ID of the ROM to upload the file for. Optional if RomGroupId is provided.</param>
        /// <param name="RomGroupId">The ID of the ROM group to upload the file for. Optional if RomId is provided.</param>
        /// <param name="fileData">The file data to upload.</param>
        /// <returns>An IActionResult indicating the result of the upload operation.</returns>
        [MapToApiVersion("1.1")]
        [HttpPost]
        [Route("{MetadataMapId}/roms/{RomId}/filesystem")]
        [Route("{MetadataMapId}/romgroup/{RomGroupId}/filesystem")]
        [RequestSizeLimit(long.MaxValue)]
        [DisableRequestSizeLimit, RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue, ValueLengthLimit = int.MaxValue)]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> GameFilesystemUpload(long MetadataMapId, long? RomId, long? RomGroupId, [FromForm] FileUploadModel fileData)
        {
            // accepts a single file upload with metadata for a specified ROM or ROM group and saves it to the server - used for syncing save data back to the server from clients

            var user = await _userManager.GetUserAsync(User);

            if (user != null)
            {
                if ((RomId == null && RomGroupId == null) || (RomId != null && RomGroupId != null))
                {
                    return BadRequest("Must provide either RomId or RomGroupId, but not both.");
                }

                string basePath = fileSystemBasePath(user.Id, MetadataMapId, RomId, RomGroupId);

                try
                {
                    if (fileData?.FileContent == null || fileData.FileContent.Length == 0)
                    {
                        return BadRequest("Missing file content.");
                    }

                    string requestedFileName = string.IsNullOrWhiteSpace(fileData.FileName)
                        ? fileData.FileContent.FileName
                        : fileData.FileName;

                    if (string.IsNullOrWhiteSpace(requestedFileName))
                    {
                        return BadRequest("Missing file name.");
                    }

                    // Normalize the file path to be platform-agnostic
                    // Convert all separators to forward slashes, then split and recombine with platform-appropriate separator
                    string[] pathParts = requestedFileName.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
                    // Remove empty parts (e.g., from leading slashes)
                    pathParts = pathParts.Where(p => p != "." && p != "..").ToArray();
                    if (pathParts.Length == 0)
                    {
                        return BadRequest("Invalid file path.");
                    }

                    string normalizedFileName = Path.Combine(pathParts);

                    string fullBasePath = Path.GetFullPath(basePath);
                    string fullFilePath = Path.GetFullPath(Path.Combine(basePath, normalizedFileName));

                    if (!fullFilePath.StartsWith(fullBasePath + Path.DirectorySeparatorChar) && !string.Equals(fullFilePath, fullBasePath, StringComparison.OrdinalIgnoreCase))
                    {
                        return BadRequest("Invalid file path.");
                    }

                    string? directoryPath = Path.GetDirectoryName(fullFilePath);
                    if (string.IsNullOrEmpty(directoryPath))
                    {
                        return BadRequest("Invalid file path.");
                    }

                    if (!Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }

                    await using (FileStream fileStream = new FileStream(fullFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await fileData.FileContent.CopyToAsync(fileStream);
                    }

                    // Set last modified date if provided
                    if (fileData.LastModified.HasValue)
                    {
                        DateTime lastModifiedUtc = fileData.LastModified.Value.Kind == DateTimeKind.Utc
                            ? fileData.LastModified.Value
                            : fileData.LastModified.Value.ToUniversalTime();
                        System.IO.File.SetLastWriteTimeUtc(fullFilePath, lastModifiedUtc);
                    }
                }
                catch (Exception ex)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Error saving file system data: " + ex.Message);
                }

                return Ok("File system data uploaded successfully.");
            }
            else
            {
                return Unauthorized();
            }
        }

        public class FileUploadModel
        {
            public string? FileName { get; set; }
            public IFormFile? FileContent { get; set; }
            public DateTime? LastModified { get; set; }
        }

        public class FilesystemReconcileModel
        {
            public List<string>? FileNames { get; set; }
        }

        public class GameSearchModel
        {
            public string Name { get; set; }
            public List<string>? Platform { get; set; }
            public List<string>? Genre { get; set; }
            public List<string>? GameMode { get; set; }
            public List<string>? PlayerPerspective { get; set; }
            public List<string>? Theme { get; set; }
            public int MinimumReleaseYear { get; set; } = -1;
            public int MaximumReleaseYear { get; set; } = -1;
            public GameRatingItem? GameRating { get; set; }
            public GameAgeRatingItem? GameAgeRating { get; set; }
            public GameSortingItem Sorting { get; set; }
            public bool HasSavedGame { get; set; }
            public bool IsFavourite { get; set; }
            public int MinPlayTime { get; set; } = -1;
            public int MaxPlayTime { get; set; } = -1;


            public class GameRatingItem
            {
                public int MinimumRating { get; set; } = -1;
                public int MinimumRatingCount { get; set; } = -1;
                public int MaximumRating { get; set; } = -1;
                public int MaximumRatingCount { get; set; } = -1;
                public bool IncludeUnrated { get; set; } = false;
            }

            public class GameAgeRatingItem
            {
                public List<AgeGroups.AgeRestrictionGroupings> AgeGroupings { get; set; } = new List<AgeGroups.AgeRestrictionGroupings>{
                    AgeGroups.AgeRestrictionGroupings.Child,
                    AgeGroups.AgeRestrictionGroupings.Teen,
                    AgeGroups.AgeRestrictionGroupings.Mature,
                    AgeGroups.AgeRestrictionGroupings.Adult
                };
                public bool IncludeUnrated { get; set; } = true;
            }

            public class GameSortingItem
            {
                public SortField SortBy { get; set; } = SortField.NameThe;
                public bool SortAscending { get; set; } = true;

                public enum SortField
                {
                    Name,
                    NameThe,
                    Rating,
                    RatingCount,
                    DateAdded,
                    LastPlayed,
                    TimePlayed,
                    ReleaseDate
                }
            }
        }

        public static async Task<GameReturnPackage> GetGames(GameSearchModel model, ApplicationUser? user, int pageNumber = 0, int pageSize = 0, bool returnSummary = true, bool returnGames = true)
        {
            string whereClause = "";
            string havingClause = "";
            Dictionary<string, object> whereParams = new Dictionary<string, object>(20);
            whereParams.Add("userid", user.Id);

            List<string> joinClauses = new List<string>(5);
            string joinClauseTemplate = "LEFT JOIN `Relation_Game_<Datatype>s` ON `Game`.`Id` = `Relation_Game_<Datatype>s`.`GameId` AND `Relation_Game_<Datatype>s`.`GameSourceId` = `Game`.`SourceId` LEFT JOIN `Metadata_<Datatype>` AS `<Datatype>` ON `Relation_Game_<Datatype>s`.`<Datatype>sId` = `<Datatype>`.`Id`  AND `Relation_Game_<Datatype>s`.`GameSourceId` = `<Datatype>`.`SourceId`";
            List<string> whereClauses = new List<string>(10);
            List<string> havingClauses = new List<string>(10);

            // Only include metadata maps that currently contain ROMs.
            whereClauses.Add("`MetadataMap`.`RomCount` > 0");

            if (model.Name.Length > 0)
            {
                whereClauses.Add("(MATCH(`Game`.`Name`) AGAINST (@GameName IN BOOLEAN MODE) OR MATCH(`AlternativeName`.`Name`) AGAINST (@GameName IN BOOLEAN MODE) OR MATCH (`LocalizedNames`.`Name`) AGAINST (@GameName IN BOOLEAN MODE))");
                whereParams.Add("@GameName", "(*" + model.Name + "*) (" + model.Name + ") ");
            }

            if (model.HasSavedGame == true)
            {
                string hasSavesTemp = "(MAX(IFNULL(`RomSaveStats`.`RomSavedStates`, 0)) > 0 OR MAX(IFNULL(`RomSaveStats`.`RomSavedFiles`, 0)) > 0 OR MAX(IFNULL(`RomGroupStats`.`RomGroupSavedStates`, 0)) > 0 OR MAX(IFNULL(`RomGroupStats`.`RomGroupSavedFiles`, 0)) > 0)";
                havingClauses.Add(hasSavesTemp);
            }

            if (model.IsFavourite == true)
            {
                string isFavTemp = "Favourite = 1";
                havingClauses.Add(isFavTemp);
            }

            if (model.MinimumReleaseYear != -1)
            {
                string releaseTempMinVal = "FirstReleaseDate >= @minreleasedate";
                whereParams.Add("minreleasedate", new DateTime(model.MinimumReleaseYear, 1, 1));
                havingClauses.Add(releaseTempMinVal);
            }

            if (model.MaximumReleaseYear != -1)
            {
                string releaseTempMaxVal = "FirstReleaseDate <= @maxreleasedate";
                whereParams.Add("maxreleasedate", new DateTime(model.MaximumReleaseYear, 12, 31, 23, 59, 59));
                havingClauses.Add(releaseTempMaxVal);
            }

            if (model.MinPlayTime != -1)
            {
                string playTimeTempMinVal = "TimePlayed >= @minplaytime";
                whereParams.Add("minplaytime", model.MinPlayTime);
                havingClauses.Add(playTimeTempMinVal);
            }

            if (model.MaxPlayTime != -1)
            {
                string playTimeTempMaxVal = "TimePlayed <= @maxplaytime";
                whereParams.Add("maxplaytime", model.MaxPlayTime);
                havingClauses.Add(playTimeTempMaxVal);
            }

            if (model.GameRating != null)
            {
                List<string> ratingClauses = new List<string>(4);
                if (model.GameRating.MinimumRating != -1)
                {
                    string ratingTempMinVal = "totalRating >= @totalMinRating";
                    whereParams.Add("@totalMinRating", model.GameRating.MinimumRating);
                    ratingClauses.Add(ratingTempMinVal);
                }

                if (model.GameRating.MaximumRating != -1)
                {
                    string ratingTempMaxVal = "totalRating <= @totalMaxRating";
                    whereParams.Add("@totalMaxRating", model.GameRating.MaximumRating);
                    ratingClauses.Add(ratingTempMaxVal);
                }

                if (model.GameRating.MinimumRatingCount != -1)
                {
                    string ratingTempMinCountVal = "totalRatingCount >= @totalMinRatingCount";
                    whereParams.Add("@totalMinRatingCount", model.GameRating.MinimumRatingCount);
                    ratingClauses.Add(ratingTempMinCountVal);
                }

                if (model.GameRating.MaximumRatingCount != -1)
                {
                    string ratingTempMaxCountVal = "totalRatingCount <= @totalMaxRatingCount";
                    whereParams.Add("@totalMaxRatingCount", model.GameRating.MaximumRatingCount);
                    ratingClauses.Add(ratingTempMaxCountVal);
                }

                // generate rating sub clause
                string ratingClauseValue = string.Join(" AND ", ratingClauses);

                string unratedClause = "";
                if (model.GameRating.IncludeUnrated == true)
                {
                    unratedClause = "totalRating IS NULL";
                }

                if (ratingClauseValue.Length > 0)
                {
                    if (unratedClause.Length > 0)
                    {
                        havingClauses.Add("((" + ratingClauseValue + ") OR " + unratedClause + ")");
                    }
                    else
                    {
                        havingClauses.Add("(" + ratingClauseValue + ")");
                    }
                }
            }

            if (model.Platform != null)
            {
                if (model.Platform.Count > 0)
                {
                    var sb = new System.Text.StringBuilder("`MetadataMap`.`PlatformId` IN (", model.Platform.Count * 15);
                    for (int i = 0; i < model.Platform.Count; i++)
                    {
                        if (i > 0)
                        {
                            sb.Append(", ");
                        }
                        string platformLabel = "@Platform" + i;
                        sb.Append(platformLabel);
                        whereParams.Add(platformLabel, model.Platform[i]);
                    }
                    sb.Append(')');
                    whereClauses.Add(sb.ToString());
                }
            }

            if (model.Genre != null)
            {
                if (model.Genre.Count > 0)
                {
                    var sb = new System.Text.StringBuilder("Genre.`Name` IN (", model.Genre.Count * 15);
                    for (int i = 0; i < model.Genre.Count; i++)
                    {
                        if (i > 0)
                        {
                            sb.Append(", ");
                        }
                        string genreLabel = "@Genre" + i;
                        sb.Append(genreLabel);
                        whereParams.Add(genreLabel, model.Genre[i]);
                    }
                    sb.Append(')');
                    whereClauses.Add(sb.ToString());

                    joinClauses.Add(joinClauseTemplate.Replace("<Datatype>", "Genre"));
                }
            }

            if (model.GameMode != null)
            {
                if (model.GameMode.Count > 0)
                {
                    var sb = new System.Text.StringBuilder("GameMode.`Name` IN (", model.GameMode.Count * 15);
                    for (int i = 0; i < model.GameMode.Count; i++)
                    {
                        if (i > 0)
                        {
                            sb.Append(", ");
                        }
                        string gameModeLabel = "@GameMode" + i;
                        sb.Append(gameModeLabel);
                        whereParams.Add(gameModeLabel, model.GameMode[i]);
                    }
                    sb.Append(')');
                    whereClauses.Add(sb.ToString());

                    joinClauses.Add(joinClauseTemplate.Replace("<Datatype>", "GameMode"));
                }
            }

            if (model.PlayerPerspective != null)
            {
                if (model.PlayerPerspective.Count > 0)
                {
                    var sb = new System.Text.StringBuilder("PlayerPerspective.`Name` IN (", model.PlayerPerspective.Count * 15);
                    for (int i = 0; i < model.PlayerPerspective.Count; i++)
                    {
                        if (i > 0)
                        {
                            sb.Append(", ");
                        }
                        string playerPerspectiveLabel = "@PlayerPerspective" + i;
                        sb.Append(playerPerspectiveLabel);
                        whereParams.Add(playerPerspectiveLabel, model.PlayerPerspective[i]);
                    }
                    sb.Append(')');
                    whereClauses.Add(sb.ToString());

                    joinClauses.Add(joinClauseTemplate.Replace("<Datatype>", "PlayerPerspective"));
                }
            }

            if (model.Theme != null)
            {
                if (model.Theme.Count > 0)
                {
                    var sb = new System.Text.StringBuilder("Theme.`Name` IN (", model.Theme.Count * 15);
                    for (int i = 0; i < model.Theme.Count; i++)
                    {
                        if (i > 0)
                        {
                            sb.Append(", ");
                        }
                        string themeLabel = "@Theme" + i;
                        sb.Append(themeLabel);
                        whereParams.Add(themeLabel, model.Theme[i]);
                    }
                    sb.Append(')');
                    whereClauses.Add(sb.ToString());

                    joinClauses.Add(joinClauseTemplate.Replace("<Datatype>", "Theme"));
                }
            }

            if (model.GameAgeRating != null)
            {
                var sb = new System.Text.StringBuilder("(");
                if (model.GameAgeRating.AgeGroupings.Count > 0)
                {
                    sb.Append("Game.AgeGroupId IN (");
                    for (int i = 0; i < model.GameAgeRating.AgeGroupings.Count; i++)
                    {
                        if (i > 0)
                        {
                            sb.Append(", ");
                        }
                        string themeLabel = "@Rating" + i;
                        sb.Append(themeLabel);
                        whereParams.Add(themeLabel, model.GameAgeRating.AgeGroupings[i]);
                    }
                    sb.Append(')');
                }

                if (model.GameAgeRating.IncludeUnrated == true)
                {
                    if (model.GameAgeRating.AgeGroupings.Count > 0)
                    {
                        sb.Append(" OR ");
                    }
                    sb.Append("Game.AgeGroupId IS NULL");
                }
                sb.Append(')');
                whereClauses.Add(sb.ToString());
            }

            // build where clause
            if (whereClauses.Count > 0)
            {
                whereClause = "WHERE " + string.Join(" AND ", whereClauses);
            }

            // build having clause
            if (havingClauses.Count > 0)
            {
                havingClause = "HAVING " + string.Join(" AND ", havingClauses);
            }

            // order by clause
            string orderByField = "NameThe";
            string orderByOrder = "ASC";
            if (model.Sorting != null)
            {
                switch (model.Sorting.SortBy)
                {
                    case GameSearchModel.GameSortingItem.SortField.NameThe:
                        orderByField = "NameThe";
                        break;
                    case GameSearchModel.GameSortingItem.SortField.Name:
                        orderByField = "Name";
                        break;
                    case GameSearchModel.GameSortingItem.SortField.Rating:
                        orderByField = "TotalRating";
                        break;
                    case GameSearchModel.GameSortingItem.SortField.RatingCount:
                        orderByField = "TotalRatingCount";
                        break;
                    case GameSearchModel.GameSortingItem.SortField.DateAdded:
                        orderByField = "DateAdded";
                        break;
                    case GameSearchModel.GameSortingItem.SortField.LastPlayed:
                        orderByField = "LastPlayed";
                        break;
                    case GameSearchModel.GameSortingItem.SortField.TimePlayed:
                        orderByField = "TimePlayed";
                        break;
                    case GameSearchModel.GameSortingItem.SortField.ReleaseDate:
                        orderByField = "FirstReleaseDate";
                        break;
                    default:
                        orderByField = "NameThe";
                        break;
                }

                if (model.Sorting.SortAscending == true)
                {
                    orderByOrder = "ASC";
                }
                else
                {
                    orderByOrder = "DESC";
                }
            }
            string orderByClause = "ORDER BY `" + orderByField + "` " + orderByOrder;

            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);

            string sql = @"
                SELECT 
    `MetadataMapBridge`.`MetadataSourceId` AS `Id`,
    `MetadataMap`.`Id` AS `MetadataMapId`,
    `MetadataMapBridge`.`MetadataSourceType` AS `GameIdType`,
    `MetadataMap`.`SignatureGameName`,
    CONCAT('[',
            GROUP_CONCAT(DISTINCT `MetadataMap`.`PlatformId`
                ORDER BY `MetadataMap`.`PlatformId`
                SEPARATOR ','),
            ']') AS `Platforms`,
    `MetadataMap`.`RomCount`,
    IFNULL(`RomGroupStats`.`MediaGroups`, 0) AS `MediaGroups`,
    CASE
        WHEN `Favourites`.`UserId` IS NULL THEN 0
        ELSE 1
    END AS `Favourite`,
    MAX(IFNULL(`RomSaveStats`.`RomSavedStates`, 0)) AS `RomSavedStates`,
    MAX(IFNULL(`RomGroupStats`.`RomGroupSavedStates`, 0)) AS `RomGroupSavedStates`,
    MAX(IFNULL(`RomSaveStats`.`RomSavedFiles`, 0)) AS `RomSavedFiles`,
    MAX(IFNULL(`RomGroupStats`.`RomGroupSavedFiles`, 0)) AS `RomGroupSavedFiles`,
    `Game`.`AgeGroupId`,
    CASE
        WHEN `LocalizedNames`.`Name` IS NOT NULL THEN `LocalizedNames`.`Name`
        WHEN `Game`.`Name` IS NULL THEN `MetadataMap`.`SignatureGameName`
        ELSE `Game`.`Name`
    END AS `Name`,
    CASE
        WHEN `LocalizedNames`.`NameThe` IS NOT NULL THEN `LocalizedNames`.`NameThe`
        WHEN `Game`.`NameThe` IS NULL THEN `MetadataMap`.`SignatureGameNameThe`
        ELSE `Game`.`NameThe`
    END AS `NameThe`,
    `Game`.`Slug`,
    `Game`.`Summary`,
    `Game`.`TotalRating`,
    `Game`.`TotalRatingCount`,
    CASE
        WHEN `LocalizedNames`.`Cover` IS NULL THEN `Game`.`Cover`
        WHEN `LocalizedNames`.`Cover` = 0 THEN `Game`.`Cover`
        ELSE `LocalizedNames`.`Cover`
    END AS `Cover`,
    `Game`.`Artworks`,
    `Game`.`FirstReleaseDate`,
    `Game`.`Category`,
    `Game`.`ParentGame`,
    `Game`.`AgeRatings`,
    `Game`.`Genres`,
    `Game`.`GameModes`,
    `Game`.`PlayerPerspectives`,
    `Game`.`Screenshots`,
    `Game`.`Themes`,
    `RomDates`.`DateAdded`,
    `RomDates`.`DateUpdated`,
    IFNULL(`UserTimeTracking`.`TimePlayed`, 0) AS `TimePlayed`,
    `UserTimeTracking`.`LastPlayed`
FROM
    `MetadataMap`
        LEFT JOIN
    `MetadataMapBridge` ON (`MetadataMap`.`Id` = `MetadataMapBridge`.`ParentMapId`
        AND `MetadataMapBridge`.`Preferred` = 1)
        JOIN
    (SELECT 
        `MetadataMapId`,
            MIN(`DateCreated`) AS `DateAdded`,
            MAX(`DateUpdated`) AS `DateUpdated`
    FROM
        `Games_Roms`
    GROUP BY `MetadataMapId`) AS `RomDates` ON `MetadataMap`.`Id` = `RomDates`.`MetadataMapId`
        LEFT JOIN
    (SELECT 
        `Games_Roms`.`MetadataMapId`,
            COUNT(DISTINCT `RomSavedState`.`Id`) AS `RomSavedStates`,
            COUNT(DISTINCT `RomSavedFile`.`Id`) AS `RomSavedFiles`
    FROM
        `Games_Roms`
    LEFT JOIN `GameState` AS `RomSavedState` ON `Games_Roms`.`Id` = `RomSavedState`.`RomId`
        AND `RomSavedState`.`IsMediaGroup` = 0
        AND `RomSavedState`.`UserId` = @userid
    LEFT JOIN `GameSaves` AS `RomSavedFile` ON `Games_Roms`.`Id` = `RomSavedFile`.`RomId`
        AND `RomSavedFile`.`IsMediaGroup` = 0
        AND `RomSavedFile`.`UserId` = @userid
    GROUP BY `Games_Roms`.`MetadataMapId`) AS `RomSaveStats` ON `MetadataMap`.`Id` = `RomSaveStats`.`MetadataMapId`
        LEFT JOIN
    (SELECT 
        `RomMediaGroup`.`GameId`,
            `RomMediaGroup`.`PlatformId`,
            1 AS `MediaGroups`,
            COUNT(DISTINCT `RomGroupSavedState`.`Id`) AS `RomGroupSavedStates`,
            COUNT(DISTINCT `RomGroupSavedFile`.`Id`) AS `RomGroupSavedFiles`
    FROM
        `RomMediaGroup`
    LEFT JOIN `GameState` AS `RomGroupSavedState` ON `RomMediaGroup`.`Id` = `RomGroupSavedState`.`RomId`
        AND `RomGroupSavedState`.`IsMediaGroup` = 1
        AND `RomGroupSavedState`.`UserId` = @userid
    LEFT JOIN `GameSaves` AS `RomGroupSavedFile` ON `RomMediaGroup`.`Id` = `RomGroupSavedFile`.`RomId`
        AND `RomGroupSavedFile`.`IsMediaGroup` = 1
        AND `RomGroupSavedFile`.`UserId` = @userid
    GROUP BY `RomMediaGroup`.`GameId` , `RomMediaGroup`.`PlatformId`) AS `RomGroupStats` ON `MetadataMap`.`Id` = `RomGroupStats`.`GameId`
        AND `MetadataMap`.`PlatformId` = `RomGroupStats`.`PlatformId`
        LEFT JOIN
    (SELECT 
        `GameId`,
            `PlatformId`,
            SUM(`SessionLength`) AS `TimePlayed`,
            MAX(`SessionTime`) AS `LastPlayed`
    FROM
        `UserTimeTracking`
    WHERE
        `UserId` = @userid
    GROUP BY `GameId` , `PlatformId`) AS `UserTimeTracking` ON `MetadataMap`.`Id` = `UserTimeTracking`.`GameId`
        AND `MetadataMap`.`PlatformId` = `UserTimeTracking`.`PlatformId`
        LEFT JOIN
    `Favourites` ON `MetadataMapBridge`.`ParentMapId` = `Favourites`.`GameId`
        AND `Favourites`.`UserId` = @userid
        LEFT JOIN
    `Metadata_Game` AS `Game` ON `MetadataMapBridge`.`MetadataSourceType` = `Game`.`SourceId`
        AND `MetadataMapBridge`.`MetadataSourceId` = `Game`.`Id`
        LEFT JOIN
    `Metadata_AlternativeName` AS `AlternativeName` ON `Game`.`Id` = `AlternativeName`.`Game`
        AND `Game`.`SourceId` = `AlternativeName`.`SourceId`
        LEFT JOIN
    (
        SELECT `gl`.`Game`, `gl`.`SourceId`, `gl`.`Name`, `gl`.`NameThe`, `gl`.`Cover`
        FROM `Metadata_GameLocalization` AS `gl`
        JOIN `Metadata_Region` AS `r`
            ON `gl`.`Region` = `r`.`Id`
            AND `gl`.`SourceId` = `r`.`SourceId`
            AND `r`.`Identifier` = @lang
    ) AS `LocalizedNames` ON `Game`.`Id` = `LocalizedNames`.`Game`
        AND `Game`.`SourceId` = `LocalizedNames`.`SourceId`
" + String.Join(" ", joinClauses) + " " + whereClause + " GROUP BY `MetadataMapBridge`.`MetadataSourceType`, `MetadataMapBridge`.`MetadataSourceId` " + havingClause + " " + orderByClause;

            string? userLocale = user.UserPreferences?.Find(x => x.Setting == "User.Locale")?.Value;
            if (userLocale != null)
            {
                // userLocale is in a serliazed format, so we need to deserialize it - but since it's the only thing, we can simply remove the quotes
                userLocale = userLocale.Replace("\"", "");
                whereParams["lang"] = userLocale;
            }
            else
            {
                whereParams["lang"] = "";
            }

            DataTable dbResponse;
            DataTable fullDataset = null;
            int? RecordCount = null;

            // Optimize query execution: if we need both summary and games, execute once and slice in memory
            if (returnSummary == true && returnGames == true)
            {
                // Execute full query once
                fullDataset = await db.ExecuteCMDAsync(sql, whereParams, new DatabaseMemoryCacheOptions(CacheEnabled: true, ExpirationSeconds: 60));
                RecordCount = fullDataset.Rows.Count;

                // Create a view for the paginated results
                dbResponse = fullDataset.Clone();
                int startIndex = pageSize * (pageNumber - 1);
                int endIndex = Math.Min(startIndex + pageSize, fullDataset.Rows.Count);

                for (int i = startIndex; i < endIndex; i++)
                {
                    dbResponse.ImportRow(fullDataset.Rows[i]);
                }
            }
            else if (returnGames == true)
            {
                // Only need paginated results
                string limiter = " LIMIT @pageOffset, @pageSize";
                whereParams.Add("pageOffset", pageSize * (pageNumber - 1));
                whereParams.Add("pageSize", pageSize);
                dbResponse = await db.ExecuteCMDAsync(sql + limiter, whereParams, new DatabaseMemoryCacheOptions(CacheEnabled: true, ExpirationSeconds: 60));
            }
            else if (returnSummary == true)
            {
                // Only need summary
                fullDataset = await db.ExecuteCMDAsync(sql, whereParams, new DatabaseMemoryCacheOptions(CacheEnabled: true, ExpirationSeconds: 60));
                RecordCount = fullDataset.Rows.Count;
                dbResponse = fullDataset.Clone(); // Empty table
            }
            else
            {
                // Neither requested (edge case)
                dbResponse = new DataTable();
            }

            int indexInPage = 0;
            if (pageNumber > 1)
            {
                indexInPage = pageSize * (pageNumber - 1);
            }

            // compile data for return
            List<Games.MinimalGameItem>? RetVal = null;
            if (returnGames == true)
            {
                RetVal = new List<Games.MinimalGameItem>(dbResponse.Rows.Count);
                for (int i = 0; i < dbResponse.Rows.Count; i++)
                {
                    DataRow row = dbResponse.Rows[i];
                    Game retGame = Storage.BuildCacheObject<Game>(new Game(), row);
                    retGame.MetadataMapId = (long)row["MetadataMapId"];
                    retGame.SourceType = (FileSignature.MetadataSources)row["GameIdType"];

                    Games.MinimalGameItem retMinGame = new Games.MinimalGameItem(retGame);
                    retMinGame.Index = indexInPage++;

                    // Check for saved games more efficiently
                    long romSavedStates = row["RomSavedStates"] == DBNull.Value ? 0 : Convert.ToInt64(row["RomSavedStates"]);
                    long romGroupSavedStates = row["RomGroupSavedStates"] == DBNull.Value ? 0 : Convert.ToInt64(row["RomGroupSavedStates"]);
                    long romSavedFiles = row["RomSavedFiles"] == DBNull.Value ? 0 : Convert.ToInt64(row["RomSavedFiles"]);
                    long romGroupSavedFiles = row["RomGroupSavedFiles"] == DBNull.Value ? 0 : Convert.ToInt64(row["RomGroupSavedFiles"]);

                    retMinGame.HasSavedGame =
                        romSavedStates >= 1 ||
                        romGroupSavedStates >= 1 ||
                        romSavedFiles >= 1 ||
                        romGroupSavedFiles >= 1;

                    retMinGame.IsFavourite = (int)row["Favourite"] != 0;

                    RetVal.Add(retMinGame);
                }
            }

            Dictionary<string, GameReturnPackage.AlphaListItem>? AlphaList = null;
            if (returnSummary == true)
            {
                AlphaList = new Dictionary<string, GameReturnPackage.AlphaListItem>(27);

                // build alpha list
                if (orderByField == "NameThe" || orderByField == "Name")
                {
                    int currentPage = 1;
                    int nextPageIndex = pageSize > 0 ? pageSize : int.MaxValue;

                    string alphaSearchField = orderByField == "NameThe" ? "NameThe" : "Name";
                    HashSet<string> seenKeys = new HashSet<string>(27);

                    for (int i = 0; i < fullDataset.Rows.Count; i++)
                    {
                        if (i + 1 == nextPageIndex)
                        {
                            currentPage++;
                            nextPageIndex += pageSize;
                        }

                        string? gameName = fullDataset.Rows[i][alphaSearchField]?.ToString();
                        string key;
                        if (string.IsNullOrEmpty(gameName))
                        {
                            key = "#";
                        }
                        else
                        {
                            char firstChar = char.ToUpperInvariant(gameName[0]);
                            key = (firstChar >= 'A' && firstChar <= 'Z') ? firstChar.ToString() : "#";
                        }

                        if (seenKeys.Add(key))
                        {
                            AlphaList[key] = new GameReturnPackage.AlphaListItem
                            {
                                Index = i,
                                Page = currentPage
                            };
                        }
                    }
                }
            }

            GameReturnPackage gameReturn = new GameReturnPackage
            {
                Count = RecordCount,
                Games = RetVal,
                AlphaList = AlphaList
            };

            return gameReturn;
        }

        public class GameReturnPackage
        {
            public GameReturnPackage()
            {

            }

            public GameReturnPackage(int Count, List<Game> Games)
            {
                this.Count = Count;

                List<Games.MinimalGameItem> minimalGames = new List<Games.MinimalGameItem>();
                foreach (Game game in Games)
                {
                    minimalGames.Add(new Classes.Metadata.Games.MinimalGameItem(game));
                }

                this.Games = minimalGames;
            }

            public int? Count { get; set; }
            public List<Games.MinimalGameItem>? Games { get; set; } = new List<Games.MinimalGameItem>();
            public Dictionary<string, AlphaListItem>? AlphaList { get; set; }
            public class AlphaListItem
            {
                public int Index { get; set; }
                public int Page { get; set; }
            }
        }
    }
}