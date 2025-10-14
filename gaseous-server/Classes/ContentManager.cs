using System.Data;
using System.Threading.Tasks;
using gaseous_server.Models;
using System.Diagnostics;
using System.Globalization;

namespace gaseous_server.Classes.Content
{
    /// <summary>
    /// Provides configuration and access to different types of user-submitted or managed content.
    /// </summary>
    public class ContentManager
    {
        private const string AttachmentIdParam = "@attachmentid";
        private const string RoleAdmin = "Admin";
        private const string SystemUserId = "System";
        /// <summary>
        /// Enumerates the supported types of content.
        /// </summary>
        public enum ContentType
        {
            /// <summary>Screenshots or still images.</summary>
            Screenshot = 0,
            /// <summary>Video content.</summary>
            Video = 1,
            /// <summary>Short audio sample content (e.g., previews, sound bites).</summary>
            AudioSample = 2,
            /// <summary>Manuals or documentation files. Available to all users.</summary>
            GlobalManual = 3,
            /// <summary>Photos, such as box art or promotional images.</summary>
            Photo = 4,
            /// <summary>Notes or text-based content.</summary>
            Note = 5,
            /// <summary>Miscellaneous content not covered by other types.</summary>
            Misc = 100
        }

        /// <summary>
        /// Configuration for a specific content type, including platform restrictions.
        /// </summary>
        public class ContentConfiguration
        {
            /// <summary>
            /// Optional list of platform IDs this content type is limited to; an empty list means no restriction.
            /// </summary>
            public List<long> LimitToPlatformIds { get; set; } = new List<long>(); // empty list means no limit

            /// <summary>
            /// Optional list of user roles allowed to manage this content type; an empty list means no role restriction.
            /// </summary>
            public List<string> AllowedRoles { get; set; } = new List<string>(); // empty list means no role restriction

            /// <summary>
            /// Defines if the content type is user or system managed. Default is user managed. If system managed, AllowedRoles must contain "Admin".
            /// </summary>
            public bool IsUserManaged { get; set; } = true; // if false, AllowedRoles must contain "Admin"

            /// <summary>
            /// Defines if the content is shareable between users. Default is false. If true, content will only be viewable via an ACL check (not implemented yet).
            /// </summary>
            public bool IsShareable { get; set; } = false; // if true, content will only be viewable via an ACL check (not implemented yet)
        }

        private static readonly Dictionary<ContentType, ContentConfiguration> _contentConfigurations = new()
        {
            { ContentType.Screenshot, new ContentConfiguration() {
                LimitToPlatformIds = new List<long>(),
                IsUserManaged = true,
                IsShareable = true
            } },
            { ContentType.Video, new ContentConfiguration() {
                LimitToPlatformIds = new List<long>(),
                IsUserManaged = true,
                IsShareable = true
            } },
            { ContentType.AudioSample, new ContentConfiguration() {
                LimitToPlatformIds = new List<long>{ 52 },
                AllowedRoles = new List<string>{ RoleAdmin },
                IsUserManaged = false
            } },
            { ContentType.GlobalManual, new ContentConfiguration() {
                LimitToPlatformIds = new List<long>(),
                AllowedRoles = new List<string>{ RoleAdmin },
                IsUserManaged = false
            } },
            { ContentType.Photo, new ContentConfiguration() {
                LimitToPlatformIds = new List<long>(),
                IsUserManaged = true,
                IsShareable = true
            } },
            { ContentType.Note, new ContentConfiguration() {
                LimitToPlatformIds = new List<long>(),
                IsUserManaged = true,
                IsShareable = false
            } },
            { ContentType.Misc, new ContentConfiguration() {
                LimitToPlatformIds = new List<long>()
            } }
        };

        /// <summary>
        /// Read-only access to the configured content type definitions.
        /// </summary>
        public static IReadOnlyDictionary<ContentType, ContentConfiguration> ContentConfigurations => _contentConfigurations;

        /// <summary>
        /// Checks if a given content type is allowed to be uploaded. If no rule is defned, it is allowed by default.
        /// </summary>
        /// <param name="contentType">The content type to check.</param>
        /// <param name="platformId">Optional platform ID to check against platform restrictions.</param>
        /// <param name="userRoles">Optional list of user roles to check against role restrictions.</param>
        /// <returns>True if the content type is allowed under the given conditions; otherwise, false.</returns>
        private static bool IsContentTypeUploadable(ContentType contentType, long? platformId = null, List<string>? userRoles = null)
        {
            if (!_contentConfigurations.TryGetValue(contentType, out var config))
            {
                // If no configuration exists for the content type, allow by default
                return true;
            }
            // Check platform restrictions
            if (config.LimitToPlatformIds.Any() && platformId.HasValue && !config.LimitToPlatformIds.Contains(platformId.Value))
            {
                return false;
            }
            // Check role restrictions
            if (config.AllowedRoles.Any() && (userRoles == null || !userRoles.Intersect(config.AllowedRoles).Any()))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Adds user-submitted content associated with a metadata item, saving the file and recording metadata in the database.
        /// </summary>
        /// <param name="metadataId">The ID of the metadata item to associate the content with.</param>
        /// <param name="contentModel">The content model containing the content data and metadata.</param>
        /// <param name="user">The user submitting the content; can be null for GlobalManual content type if the user has Admin role.</param>
        /// <returns>The ID of the newly created content attachment record.</returns>
        /// <exception cref="InvalidOperationException">Thrown if validation fails or database insertion fails.</exception>
        public static async Task<long> AddMetadataItemContent(long metadataId, ContentModel contentModel, Authentication.ApplicationUser? user = null)
        {
            // get metadata map to determine platform
            var metadataMap = await MetadataManagement.GetMetadataMap(metadataId);
            if (metadataMap == null)
            {
                throw new InvalidOperationException($"Metadata map not found for id {metadataId}.");
            }
            var platformId = metadataMap.PlatformId;

            // get user roles if user is provided
            string userId = "";
            List<string> userRoles = new List<string>();
            if (user == null && contentModel.ContentType != ContentType.GlobalManual)
            {
                throw new InvalidOperationException("User must be provided for non-global manual content types.");
            }
            else if (user == null && contentModel.ContentType == ContentType.GlobalManual)
            {
                // Global manuals can be added without a user, but only by Admins
                userId = SystemUserId;
                userRoles.Add(RoleAdmin);
            }
            else if (user == null)
            {
                throw new InvalidOperationException("User must be provided for non-global manual content types.");
            }
            else
            {
                var userStore = new Authentication.UserStore();
                userRoles = (await userStore.GetRolesAsync(user, new CancellationToken())).ToList();
                userId = user.Id;
            }

            // validate content type is allowed
            if (!IsContentTypeUploadable(contentModel.ContentType, platformId, userRoles))
            {
                throw new InvalidOperationException($"Content type {contentModel.ContentType} is not allowed for the given platform or user roles.");
            }

            // validate content
            ContentType contentType = contentModel.ContentType;
            byte[] contentData = contentModel.ByteArray;
            try
            {
                ValidateContent(ref contentType, ref contentData);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Content validation failed: " + ex.Message, ex);
            }

            // save file to disk
            string userDirectory = userId == SystemUserId ? "Global" : userId;
            string contentDir = contentType == ContentType.GlobalManual ? "manuals" : contentType.ToString().ToLower() + "s";
            string dirPath = Path.Combine(Config.LibraryConfiguration.LibraryContentDirectory, userDirectory, contentDir);
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
            string fileName = $"{Guid.NewGuid()}"; // use a GUID for the filename to avoid collisions
            string filePath = Path.Combine(dirPath, fileName);
            await System.IO.File.WriteAllBytesAsync(filePath, contentData);

            // compute SHA1 hash of file
            using var sha1 = System.Security.Cryptography.SHA1.Create();
            byte[] fileHashBytes = sha1.ComputeHash(contentData);
            string fileHash = BitConverter.ToString(fileHashBytes).Replace("-", "").ToLowerInvariant();

            // Save content metadata to database, associating with metadataId and user if provided
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "INSERT INTO MetadataMap_Attachments (MetadataMapID, AttachmentType, UserId, SHA1, Filename, FileSystemFilename, Size) VALUES (@MetadataMapID, @AttachmentType, @UserId, @SHA1, @filename, @filesystemfilename, @size); SELECT LAST_INSERT_ID();";
            var parameters = new Dictionary<string, object>
            {
                { "@MetadataMapID", metadataId },
                { "@AttachmentType", (int)contentType },
                { "@UserId", userId },
                { "@SHA1", fileHash },
                { "@filename", contentModel.Filename },
                { "@filesystemfilename", System.IO.Path.GetFileName(filePath) },
                { "@size", new System.IO.FileInfo(filePath).Length }
            };
            var result = await db.ExecuteCMDAsync(sql, parameters);
            if (result.Rows.Count > 0)
            {
                long newId = Convert.ToInt64(result.Rows[0][0]);
                return newId;
            }
            else
            {
                throw new InvalidOperationException("Failed to insert content metadata into the database.");
            }
        }
        private static void ValidateContent(ref ContentType contentType, ref byte[] contentData)
        {
            switch (contentType)
            {
                case ContentType.Screenshot:
                case ContentType.Photo:
                    ValidateImageContent(ref contentType, ref contentData);
                    break;
                case ContentType.Video:
                    ValidateVideoContent(ref contentData);
                    break;
                case ContentType.Note:
                    ValidateNoteContent(ref contentData);
                    break;
                case ContentType.GlobalManual:
                    ValidatePdfContent(contentData);
                    break;
                case ContentType.AudioSample:
                    ValidateAudioSample(contentData);
                    break;
                default:
                    throw new InvalidOperationException("Unsupported content type.");
            }
        }

        private static void ValidateImageContent(ref ContentType contentType, ref byte[] contentData)
        {
            try
            {
                bool isGif = contentData.Length >= 6 &&
                             contentData[0] == 'G' && contentData[1] == 'I' && contentData[2] == 'F' &&
                             contentData[3] == '8' &&
                             (contentData[4] == '7' || contentData[4] == '9') &&
                             contentData[5] == 'a';
                bool converted = false;
                if (isGif)
                {
                    using var gifStream = new MemoryStream(contentData);
                    var collection = new ImageMagick.MagickImageCollection();
                    collection.Read(gifStream);
                    if (collection.Count > 1)
                    {
                        try
                        {
                            // Collect frame delays (in 1/100s of a second units per GIF spec)
                            var delays = collection.Select(f => (int)f.AnimationDelay).Where(d => d > 0).ToList();
                            double avgDelay = delays.Any() ? delays.Average() : 10d; // default 10 (i.e. 100ms) if missing
                            double derivedFps = 100.0 / avgDelay; // 100 * (1/avgDelayHundredths) = frames per second
                            if (derivedFps < 5) derivedFps = 5;
                            if (derivedFps > 60) derivedFps = 60;

                            // Prefer an integer fps for ffmpeg stability
                            int targetFps = (int)Math.Round(derivedFps);
                            if (targetFps < 5) targetFps = 5;
                            if (targetFps > 60) targetFps = 60;

                            // Execute ffmpeg conversion
                            byte[]? mp4Bytes = ConvertGifToMp4WithFfmpeg(contentData, targetFps);
                            if (mp4Bytes != null && mp4Bytes.Length > 0)
                            {
                                contentData = mp4Bytes;
                                contentType = ContentType.Video;
                                converted = true;
                            }
                            else
                            {
                                Logging.Log(Logging.LogType.Warning, "Content Manager", "Animated GIF conversion to MP4 failed: empty output. Falling back to PNG.");
                            }
                        }
                        catch (Exception ex)
                        {
                            Logging.Log(Logging.LogType.Warning, "Content Manager", $"Animated GIF conversion to MP4 failed: {ex.Message}. Falling back to PNG.");
                        }
                    }
                    collection.Dispose();
                }
                if (!converted)
                {
                    using var inputStream = new MemoryStream(contentData);
                    using var image = new ImageMagick.MagickImage(inputStream);
                    image.Format = ImageMagick.MagickFormat.Png;
                    using var ms = new MemoryStream();
                    image.Write(ms);
                    contentData = ms.ToArray();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Invalid image data.", ex);
            }
        }

        private static void ValidateVideoContent(ref byte[] contentData)
        {
            try
            {
                bool isMp4 = contentData.Length > 12 &&
                             contentData[4] == (byte)'f' &&
                             contentData[5] == (byte)'t' &&
                             contentData[6] == (byte)'y' &&
                             contentData[7] == (byte)'p';
                if (!isMp4)
                {
                    using var inputStream = new MemoryStream(contentData);
                    var collection = new ImageMagick.MagickImageCollection();
                    collection.Read(inputStream);
                    if (collection.Count == 0)
                        throw new InvalidOperationException("No frames detected in video source.");
                    collection.Coalesce();
                    // Ensure even dimensions
                    int width = (int)collection[0].Width;
                    int height = (int)collection[0].Height;
                    int newWidth = (width % 2 == 0) ? width : width + 1;
                    int newHeight = (height % 2 == 0) ? height : height + 1;
                    if (newWidth != width || newHeight != height)
                    {
                        var normalized = new ImageMagick.MagickImageCollection();
                        foreach (var frame in collection)
                        {
                            var canvas = new ImageMagick.MagickImage(ImageMagick.MagickColors.Transparent, (uint)newWidth, (uint)newHeight);
                            canvas.Composite(frame, 0, 0);
                            canvas.AnimationDelay = frame.AnimationDelay;
                            normalized.Add(canvas);
                        }
                        collection.Dispose();
                        collection = normalized;
                    }
                    using var outputStream = new MemoryStream();
                    collection.Write(outputStream, ImageMagick.MagickFormat.Mp4);
                    contentData = outputStream.ToArray();
                    collection.Dispose();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Invalid or unsupported video data.", ex);
            }
        }

        private static void ValidateNoteContent(ref byte[] contentData)
        {
            try
            {
                var text = System.Text.Encoding.UTF8.GetString(contentData);
                contentData = System.Text.Encoding.UTF8.GetBytes(text);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Invalid text data.", ex);
            }
        }

        private static void ValidatePdfContent(byte[] contentData)
        {
            if (contentData.Length < 5 || !(contentData[0] == '%' && contentData[1] == 'P' && contentData[2] == 'D' && contentData[3] == 'F' && contentData[4] == '-'))
            {
                throw new InvalidOperationException("Invalid PDF data.");
            }
        }

        private static void ValidateAudioSample(byte[] contentData)
        {
            if (contentData.Length < 4 || !(contentData[0] == 'P' && contentData[1] == 'K' && contentData[2] == 3 && contentData[3] == 4))
            {
                throw new InvalidOperationException("Invalid ZIP data.");
            }
        }

        /// <summary>
        /// Retrieves a list of content attachments associated with the specified metadata item IDs that the user has access to.
        /// </summary>
        /// <param name="metadataIds">List of metadata item IDs to retrieve content for.</param>
        /// <param name="user">The user requesting the content; used for access control.</param>
        /// <param name="contentTypes">Optional list of content types to filter by; if null, all types are returned.</param>
        /// <param name="page">The page number for pagination (1-based).</param>
        /// <param name="pageSize">The number of items per page for pagination.</param>
        /// <returns>List of ContentViewModel representing the accessible content attachments.</returns>
        /// <exception cref="ArgumentException">Thrown if parameters are invalid.</exception>
        public static async Task<ContentViewModel> GetMetadataItemContents(List<long> metadataIds, Authentication.ApplicationUser user, List<ContentType>? contentTypes, int page = 1, int pageSize = 50)
        {
            if (metadataIds == null || metadataIds.Count == 0)
            {
                throw new ArgumentException("metadataIds cannot be null or empty");
            }

            if (contentTypes == null)
            {
                throw new ArgumentException("contentTypes cannot be null");
            }

            ContentViewModel contentViewModel = new ContentViewModel
            {
                Items = new List<ContentViewModel.ContentViewItemModel>(),
                TotalCount = 0,
                Page = page,
                PageSize = pageSize
            };

            if (contentTypes.Count == 0)
            {
                // if no content types specified, return empty list
                return contentViewModel;
            }

            if (user == null)
            {
                throw new ArgumentException("user cannot be null");
            }

            // get total count for pagination
            Database dbCount = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string countSql = "SELECT COUNT(*) FROM MetadataMap_Attachments WHERE MetadataMapID IN (" + string.Join(",", metadataIds) + ") AND AttachmentType IN (" + string.Join(",", contentTypes.Select(ct => (int)ct)) + ") AND (UserId = @userid OR UserId = @systemuserid);";
            var countParameters = new Dictionary<string, object>
            {
                { "@userid", user.Id },
                { "@systemuserid", "System" }
            };
            var countResult = await dbCount.ExecuteCMDAsync(countSql, countParameters);
            if (countResult.Rows.Count > 0)
            {
                contentViewModel.TotalCount = Convert.ToInt32(countResult.Rows[0][0]);
            }

            // get the data
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "SELECT * FROM MetadataMap_Attachments WHERE MetadataMapID IN (" + string.Join(",", metadataIds) + ") AND AttachmentType IN (" + string.Join(",", contentTypes.Select(ct => (int)ct)) + ") AND (UserId = @userid OR UserId = @systemuserid) ORDER BY DateCreated DESC LIMIT @offset, @pagesize;";
            var parameters = new Dictionary<string, object>
            {
                { "@userid", user.Id },
                { "@systemuserid", "System" },
                { "@offset", (page - 1) * pageSize },
                { "@pagesize", pageSize }
            };
            var result = await db.ExecuteCMDAsync(sql, parameters);
            List<ContentViewModel.ContentViewItemModel> contents = new List<ContentViewModel.ContentViewItemModel>();
            foreach (DataRow row in result.Rows)
            {
                contents.Add(await BuildContentView(row));
            }

            // add to view model
            contentViewModel.Items = contents;

            return contentViewModel;
        }

        /// <summary>
        /// Retrieves a specific content attachment by its ID if the user has access to it.
        /// </summary>
        /// <param name="attachmentId">The ID of the content attachment to retrieve.</param>
        /// <param name="user">The user requesting the content; used for access control.</param>
        /// <returns>The ContentViewModel representing the content attachment.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the attachment is not found or access is denied.</exception>
        public static async Task<ContentViewModel.ContentViewItemModel> GetMetadataItemContent(long attachmentId, Authentication.ApplicationUser user)
        {
            // return the requested attachment if the user has access to it
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = $"SELECT * FROM MetadataMap_Attachments WHERE AttachmentID = {AttachmentIdParam} AND ((UserId = @userid OR UserId = @systemuserid) OR IsShared = @isshared);";
            var parameters = new Dictionary<string, object>
            {
                { AttachmentIdParam, attachmentId },
                { "@userid", user.Id },
                { "@systemuserid", "System" },
                { "@isshared", true }
            };

            var result = await db.ExecuteCMDAsync(sql, parameters);
            if (result.Rows.Count == 0)
            {
                throw new InvalidOperationException("Attachment not found or access denied.");
            }
            return await BuildContentView(result.Rows[0]);
        }

        /// <summary>
        /// Retrieves the binary data of a specific content attachment by its ID if the user has access to it.
        /// </summary>
        /// <param name="attachmentId">The ID of the content attachment to retrieve.</param>
        /// <param name="user">The user requesting the content; used for access control.</param>
        /// <returns>The binary data of the content attachment.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the attachment is not found, access is denied, or the file is missing.</exception>
        public static async Task<byte[]> GetMetadataItemContentData(long attachmentId, Authentication.ApplicationUser user)
        {
            // return the requested attachment data if the user has access to it
            var contentView = await GetMetadataItemContent(attachmentId, user);
            if (contentView == null)
            {
                throw new InvalidOperationException("Attachment not found or access denied.");
            }

            string userDirectory = contentView.UploadedByUserId == "System" ? "Global" : contentView.UploadedByUserId;
            string contentDir = contentView.ContentType == ContentType.GlobalManual ? "manuals" : contentView.ContentType.ToString().ToLower() + "s";
            string dirPath = Path.Combine(Config.LibraryConfiguration.LibraryContentDirectory, userDirectory, contentDir);
            string filePath = Path.Combine(dirPath, contentView.FileSystemFilename);
            if (!System.IO.File.Exists(filePath))
            {
                throw new InvalidOperationException("Attachment file not found on disk.");
            }

            byte[] fileData = await System.IO.File.ReadAllBytesAsync(filePath);
            return fileData;
        }

        private async static Task<ContentViewModel.ContentViewItemModel> BuildContentView(DataRow row)
        {
            var contentView = new ContentViewModel.ContentViewItemModel
            {
                MetadataId = Convert.ToInt64(row["MetadataMapID"]),
                Metadata = await MetadataManagement.GetMetadataMap(Convert.ToInt64(row["MetadataMapID"])),
                AttachmentId = Convert.ToInt64(row["AttachmentID"]),
                FileName = Convert.ToString(row["Filename"]) ?? "",
                FileSystemFilename = Convert.ToString(row["FileSystemFilename"]) ?? "",
                ContentType = (ContentType)Convert.ToInt32(row["AttachmentType"]),
                Size = Convert.ToInt64(row["Size"]),
                UploadedAt = Convert.ToDateTime(row["DateCreated"]),
                UploadedByUserId = Convert.ToString(row["UserId"]) ?? "",
                IsShared = Convert.ToBoolean(row["IsShared"])
            };

            // remove unwanted heavy collection data from Metadata WITHOUT mutating the cached instance
            if (contentView.Metadata != null)
            {
                // Create a lightweight shallow copy so the cached MetadataMap remains intact
                var original = contentView.Metadata;
                contentView.Metadata = new MetadataMap
                {
                    Id = original.Id,
                    PlatformId = original.PlatformId,
                    SignatureGameName = original.SignatureGameName,
                    // Intentionally omit MetadataMapItems (set to null) for this trimmed view
                    MetadataMapItems = null
                };
            }

            // remove any file extensions from the FileName
            contentView.FileName = System.IO.Path.GetFileNameWithoutExtension(contentView.FileName);

            // get uploader profile - if UserId is "System", set to null
            string userId = Convert.ToString(row["UserId"]) ?? "";
            if (userId == "System")
            {
                contentView.UploadedBy = null;
            }
            else
            {
                // get the user account from the userId
                var userStore = new Authentication.UserStore();
                var user = await userStore.FindByIdAsync(userId, CancellationToken.None);
                if (user == null)
                {
                    contentView.UploadedBy = null;
                }
                else
                {
                    var userProfile = new UserProfile();
                    contentView.UploadedBy = await userProfile.GetUserProfile(user.ProfileId.ToString());
                }
            }

            return contentView;
        }

        /// <summary>
        /// Deletes a specific content attachment by its ID if the user has permission to delete it.
        /// </summary>
        /// <param name="attachmentId">The ID of the content attachment to delete.</param>
        /// <param name="user">The user requesting the deletion; used for permission checks.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the attachment is not found or the user lacks permission to delete it.</exception>
        public static async Task DeleteMetadataItemContent(long attachmentId, Authentication.ApplicationUser user)
        {
            // user can only delete their own content, or system content if they are admin
            var existingContent = await GetMetadataItemContent(attachmentId, user);
            if (existingContent == null)
            {
                throw new InvalidOperationException("Attachment not found or access denied.");
            }

            // check if user has permission to delete
            var userStore = new Authentication.UserStore();
            var userRoles = (await userStore.GetRolesAsync(user, new CancellationToken())).ToList();
            bool allowDelete = false;

            if (existingContent.UploadedByUserId != user.Id && !userRoles.Contains("Admin"))
            {
                throw new InvalidOperationException("You do not have permission to delete this content.");
            }
            else if (existingContent.UploadedByUserId == "System" && !userRoles.Contains("Admin"))
            {
                throw new InvalidOperationException("You do not have permission to delete system content.");
            }
            else if (existingContent.UploadedByUserId == "System" && userRoles.Contains("Admin"))
            {
                // Admin deleting system content - allow
                allowDelete = true;
            }
            else if (existingContent.UploadedByUserId == user.Id)
            {
                // User deleting their own content - allow
                allowDelete = true;
            }
            else
            {
                throw new InvalidOperationException("You do not have permission to delete this content.");
            }

            if (!allowDelete)
            {
                throw new InvalidOperationException("You do not have permission to delete this content.");
            }

            // delete the file from disk
            string userDirectory = existingContent.UploadedByUserId == "System" ? "Global" : existingContent.UploadedByUserId;
            string contentDir = existingContent.ContentType == ContentType.GlobalManual ? "manuals" : existingContent.ContentType.ToString().ToLower() + "s";
            string dirPath = Path.Combine(Config.LibraryConfiguration.LibraryContentDirectory, userDirectory, contentDir);
            string filePath = Path.Combine(dirPath, existingContent.FileSystemFilename);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
            else
            {
                Logging.Log(Logging.LogType.Warning, "Content Manager", $"File {filePath} not found when attempting to delete attachment {attachmentId}.");
            }

            // delete the database record
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = $"DELETE FROM MetadataMap_Attachments WHERE AttachmentID = {AttachmentIdParam};";
            var parameters = new Dictionary<string, object>
            {
                { AttachmentIdParam, attachmentId }
            };
            await db.ExecuteCMDAsync(sql, parameters);
        }

        /// <summary>
        /// Updates properties of a specific content attachment if the user has permission to update it.
        /// </summary>
        /// <param name="attachmentId">The ID of the content attachment to update.</param>
        /// <param name="user">The user requesting the update; used for permission checks.</param>
        /// <param name="isShared">Optional new value for the IsShared property; can only be modified for shareable content types.</param>
        /// <param name="content">Optional new content: Note type updates a file on disk; Screenshot, Photo, Video, and GlobalManual types update the filename field, which is used as the content title in the frontend</param>
        /// <returns>The updated ContentViewModel.ContentViewItemModel representing the content attachment.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the attachment is not found, the user lacks permission to update it, or if invalid updates are attempted.</exception>
        public static async Task<ContentViewModel.ContentViewItemModel> UpdateMetadataItem(long attachmentId, Authentication.ApplicationUser user, bool? isShared = null, string? content = null)
        {
            // users can only update their own content
            // isShared can be modified for any user-owned content if the content type is shareable
            // content behaves differently based on content type
            //  - Note content updates the note file on disk
            //  - Screenshot, Photo, Video, and GlobalManual content updates the filename field, which is used as the content title in the frontend

            // get existing content
            var existingContent = await GetMetadataItemContent(attachmentId, user);
            if (existingContent == null)
            {
                throw new InvalidOperationException("Attachment not found or access denied.");
            }

            if (existingContent.UploadedByUserId != user.Id)
            {
                throw new InvalidOperationException("You do not have permission to update this content.");
            }

            if (isShared.HasValue)
            {
                // check if content type is shareable
                if (!_contentConfigurations.TryGetValue(existingContent.ContentType, out var config) || !config.IsShareable)
                {
                    throw new InvalidOperationException("This content type cannot be shared.");
                }

                // update isShared
                Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
                string sql = $"UPDATE MetadataMap_Attachments SET IsShared = @isshared WHERE AttachmentID = {AttachmentIdParam};";
                var parameters = new Dictionary<string, object>
                {
                    { "@isshared", isShared.Value },
                    { AttachmentIdParam, attachmentId }
                };
                await db.ExecuteCMDAsync(sql, parameters);

                existingContent.IsShared = isShared.Value;
            }

            if (!string.IsNullOrEmpty(content))
            {
                // update content based on type
                switch (existingContent.ContentType)
                {
                    case ContentType.Screenshot:
                    case ContentType.Photo:
                    case ContentType.Video:
                    case ContentType.GlobalManual:
                        // update the filename field in the database
                        Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
                        string sql = $"UPDATE MetadataMap_Attachments SET Filename = @filename WHERE AttachmentID = {AttachmentIdParam};";
                        var parameters = new Dictionary<string, object>
                        {
                            { "@filename", content },
                            { AttachmentIdParam, attachmentId }
                        };
                        await db.ExecuteCMDAsync(sql, parameters);

                        existingContent.FileName = content;
                        break;

                    case ContentType.Note:
                        // update the note content
                        string userDirectory = existingContent.UploadedByUserId == "System" ? "Global" : existingContent.UploadedByUserId;
                        string contentDir = existingContent.ContentType.ToString().ToLower() + "s";
                        string dirPath = Path.Combine(Config.LibraryConfiguration.LibraryContentDirectory, userDirectory, contentDir);
                        string filePath = Path.Combine(dirPath, existingContent.FileName);
                        await System.IO.File.WriteAllTextAsync(filePath, content);
                        break;
                    default:
                        throw new InvalidOperationException("This content type cannot be updated.");
                }
            }

            return existingContent;
        }

        /// <summary>
        /// Convert an animated GIF (byte array) to MP4 using an installed ffmpeg binary. Returns null if conversion fails.
        /// </summary>
        /// <param name="gifBytes">Source GIF bytes.</param>
        /// <param name="fps">Target frames per second (5-60 recommended).</param>
        /// <returns>MP4 bytes or null on failure.</returns>
        private static byte[]? ConvertGifToMp4WithFfmpeg(byte[] gifBytes, int fps)
        {
            try
            {
                // Ensure fps bounds
                if (fps < 5) fps = 5;
                if (fps > 60) fps = 60;

                // Check for ffmpeg availability
                string ffmpegPath = "ffmpeg"; // rely on PATH
                try
                {
                    var check = Process.Start(new ProcessStartInfo
                    {
                        FileName = ffmpegPath,
                        Arguments = "-version",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });
                    if (check == null)
                        return null;
                    check.WaitForExit(3000);
                    if (check.ExitCode != 0)
                        return null;
                }
                catch
                {
                    // ffmpeg not available
                    return null;
                }

                string tempDir = Path.Combine(Path.GetTempPath(), "gaseous_gifconv");
                Directory.CreateDirectory(tempDir);
                string inputPath = Path.Combine(tempDir, Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture) + ".gif");
                string outputPath = Path.Combine(tempDir, Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture) + ".mp4");
                File.WriteAllBytes(inputPath, gifBytes);

                // Build ffmpeg arguments:
                //  -y overwrite
                //  -i input.gif
                //  -vf scale filter to ensure even dimensions and chosen fps
                //  -movflags +faststart for streaming friendliness
                //  -pix_fmt yuv420p for compatibility
                string args = $"-hide_banner -loglevel error -y -i \"{inputPath}\" -vf \"scale=trunc(iw/2)*2:trunc(ih/2)*2,fps={fps}\" -movflags +faststart -pix_fmt yuv420p -an \"{outputPath}\"";

                var psi = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                if (proc == null)
                    return null;
                // Capture stderr in case of error for logging
                string stdErr = proc.StandardError.ReadToEnd();
                proc.WaitForExit(30000); // 30s timeout for safety
                if (proc.ExitCode != 0 || !File.Exists(outputPath))
                {
                    Logging.Log(Logging.LogType.Warning, "Content Manager", $"ffmpeg GIF->MP4 conversion failed (exit {proc.ExitCode}): {stdErr}");
                    return null;
                }

                byte[] mp4 = File.ReadAllBytes(outputPath);
                // Cleanup (best effort)
                try { File.Delete(inputPath); } catch { }
                try { File.Delete(outputPath); } catch { }
                return mp4;
            }
            catch (Exception ex)
            {
                Logging.Log(Logging.LogType.Warning, "Content Manager", $"ffmpeg GIF->MP4 conversion exception: {ex.Message}");
                return null;
            }
        }
    }
}