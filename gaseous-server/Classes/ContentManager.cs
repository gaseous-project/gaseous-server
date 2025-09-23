using System.Data;
using System.Threading.Tasks;
using gaseous_server.Models;

namespace gaseous_server.Classes.Content
{
    /// <summary>
    /// Provides configuration and access to different types of user-submitted or managed content.
    /// </summary>
    public class ContentManager
    {
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
                AllowedRoles = new List<string>{"Admin"},
                IsUserManaged = false
            } },
            { ContentType.GlobalManual, new ContentConfiguration() {
                LimitToPlatformIds = new List<long>(),
                AllowedRoles = new List<string>{"Admin" },
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
                userId = "System";
                userRoles.Add("Admin");
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
            string userDirectory = userId == "System" ? "Global" : userId;
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
            // if the content is a screenshot or photo, validate it's a valid image and convert it to PNG if it's not already. Annimated GIFs should be converted to MP4 if possible.
            // if the content is a video, validate it's a valid video format and convert it to MP4 if it's not already
            // if the content is a note, validate it's valid text (UTF-8)
            // if the content is a global manual, validate it's a valid PDF
            // if the content is an audio sample, validate it's a valid zip file
            switch (contentType)
            {
                case ContentType.Screenshot:
                case ContentType.Photo:
                    try
                    {
                        // Detect GIF (header GIF87a / GIF89a)
                        bool isGif = contentData.Length >= 6 &&
                                     contentData[0] == 'G' && contentData[1] == 'I' && contentData[2] == 'F' &&
                                     contentData[3] == '8' &&
                                     (contentData[4] == '7' || contentData[4] == '9') &&
                                     contentData[5] == 'a';

                        bool converted = false;

                        if (isGif)
                        {
                            using (var gifStream = new MemoryStream(contentData))
                            {
                                var collection = new ImageMagick.MagickImageCollection();
                                collection.Read(gifStream);

                                // Animated if more than one frame
                                if (collection.Count > 1)
                                {
                                    // Coalesce to ensure full frames
                                    collection.Coalesce();

                                    using var mp4Stream = new MemoryStream();
                                    // Convert to MP4 (requires ffmpeg delegate in environment)
                                    collection.Write(mp4Stream, ImageMagick.MagickFormat.Mp4);
                                    contentData = mp4Stream.ToArray();
                                    contentType = ContentType.Video;
                                    converted = true;
                                }
                                collection.Dispose();
                            }
                        }

                        if (!converted)
                        {
                            // Static image (including non-animated GIF) -> normalize to PNG
                            using var inputStream = new MemoryStream(contentData);
                            using var image = new ImageMagick.MagickImage(inputStream);
                            image.Format = ImageMagick.MagickFormat.Png;
                            using var ms = new MemoryStream();
                            image.Write(ms);
                            contentData = ms.ToArray();
                        }
                        break;
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException("Invalid image data.", ex);
                    }

                case ContentType.Video:
                    try
                    {
                        // Detect MP4 via 'ftyp' box at offset 4
                        bool isMp4 = contentData.Length > 12 &&
                                     contentData[4] == (byte)'f' &&
                                     contentData[5] == (byte)'t' &&
                                     contentData[6] == (byte)'y' &&
                                     contentData[7] == (byte)'p';

                        if (!isMp4)
                        {
                            // Attempt to load as a sequence (e.g., animated GIF or other supported container)
                            using var inputStream = new MemoryStream(contentData);
                            var collection = new ImageMagick.MagickImageCollection();
                            collection.Read(inputStream);

                            if (collection.Count == 0)
                                throw new InvalidOperationException("No frames detected in video source.");

                            // Ensure frames are full
                            collection.Coalesce();

                            using var outputStream = new MemoryStream();
                            // Write to MP4 (requires ImageMagick build with proper delegate support)
                            collection.Write(outputStream, ImageMagick.MagickFormat.Mp4);
                            contentData = outputStream.ToArray();

                            collection.Dispose();
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException("Invalid or unsupported video data.", ex);
                    }
                    break;

                case ContentType.Note:
                    // Check if content is valid UTF-8 text
                    try
                    {
                        var text = System.Text.Encoding.UTF8.GetString(contentData);
                        contentData = System.Text.Encoding.UTF8.GetBytes(text); // re-encode to ensure valid UTF-8
                        break;
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException("Invalid text data.", ex);
                    }

                case ContentType.GlobalManual:
                    // Check for PDF file signature (%PDF-)
                    if (contentData.Length < 5 || !(contentData[0] == '%' && contentData[1] == 'P' && contentData[2] == 'D' && contentData[3] == 'F' && contentData[4] == '-'))
                    {
                        throw new InvalidOperationException("Invalid PDF data.");
                    }
                    break;

                case ContentType.AudioSample:
                    // Check for ZIP file signature (PK\x03\x04)
                    if (contentData.Length < 4 || !(contentData[0] == 'P' && contentData[1] == 'K' && contentData[2] == 3 && contentData[3] == 4))
                    {
                        throw new InvalidOperationException("Invalid ZIP data.");
                    }
                    break;

                default:
                    throw new InvalidOperationException("Unsupported content type.");
            }
        }

        /// <summary>
        /// Retrieves a list of content attachments associated with the specified metadata item IDs that the user has access to.
        /// </summary>
        /// <param name="metadataIds">List of metadata item IDs to retrieve content for.</param>
        /// <param name="user">The user requesting the content; used for access control.</param>
        /// <param name="contentTypes">Optional list of content types to filter by; if null, all types are returned.</param>
        /// <returns>List of ContentViewModel representing the accessible content attachments.</returns>
        /// <exception cref="ArgumentException">Thrown if parameters are invalid.</exception>
        public static async Task<List<ContentViewModel>> GetMetadataItemContents(List<long> metadataIds, Authentication.ApplicationUser user, List<ContentType>? contentTypes)
        {
            if (metadataIds == null || metadataIds.Count == 0)
            {
                throw new ArgumentException("metadataIds cannot be null or empty");
            }

            if (contentTypes == null)
            {
                throw new ArgumentException("contentTypes cannot be null");
            }

            if (contentTypes.Count == 0)
            {
                // if no content types specified, return empty list
                return new List<ContentViewModel>();
            }

            if (user == null)
            {
                throw new ArgumentException("user cannot be null");
            }

            // get the data
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "SELECT * FROM MetadataMap_Attachments WHERE MetadataMapID IN (" + string.Join(",", metadataIds) + ") AND AttachmentType IN (" + string.Join(",", contentTypes.Select(ct => (int)ct)) + ") AND (UserId = @userid OR UserId = @systemuserid) OR IsShared = @isshared;";
            var parameters = new Dictionary<string, object>
            {
                { "@userid", user.Id },
                { "@systemuserid", "System" },
                { "@isshared", true }
            };
            var result = await db.ExecuteCMDAsync(sql, parameters);
            List<ContentViewModel> contents = new List<ContentViewModel>();
            foreach (DataRow row in result.Rows)
            {
                contents.Add(BuildContentView(row));
            }

            return contents;
        }

        /// <summary>
        /// Retrieves a specific content attachment by its ID if the user has access to it.
        /// </summary>
        /// <param name="attachmentId">The ID of the content attachment to retrieve.</param>
        /// <param name="user">The user requesting the content; used for access control.</param>
        /// <returns>The ContentViewModel representing the content attachment.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the attachment is not found or access is denied.</exception>
        public static async Task<ContentViewModel> GetMetadataItemContent(long attachmentId, Authentication.ApplicationUser user)
        {
            // return the requested attachment if the user has access to it
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "SELECT * FROM MetadataMap_Attachments WHERE AttachmentID = @attachmentid AND ((UserId = @userid OR UserId = @systemuserid) OR IsShared = @isshared);";
            var parameters = new Dictionary<string, object>
            {
                { "@attachmentid", attachmentId },
                { "@userid", user.Id },
                { "@systemuserid", "System" },
                { "@isshared", true }
            };

            var result = await db.ExecuteCMDAsync(sql, parameters);
            if (result.Rows.Count == 0)
            {
                throw new InvalidOperationException("Attachment not found or access denied.");
            }
            return BuildContentView(result.Rows[0]);
        }

        private static ContentViewModel BuildContentView(DataRow row)
        {
            var contentView = new ContentViewModel
            {
                AttachmentId = Convert.ToInt64(row["AttachmentID"]),
                FileName = Convert.ToString(row["Filename"]) ?? "",
                ContentType = (ContentType)Convert.ToInt32(row["AttachmentType"]),
                Size = Convert.ToInt64(row["Size"]),
                UploadedAt = Convert.ToDateTime(row["DateCreated"]),
                UploadedByUserId = Convert.ToString(row["UserId"]) ?? "",
                IsShared = Convert.ToBoolean(row["IsShared"])
            };

            // get uploader profile - if UserId is "System", set to null
            string userId = Convert.ToString(row["UserId"]) ?? "";
            if (userId == "System")
            {
                contentView.UploadedBy = null;
            }
            else
            {
                var userProfile = new UserProfile();
                contentView.UploadedBy = userProfile.GetUserProfile(userId).Result;
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
            string filePath = Path.Combine(dirPath, existingContent.FileName);
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
            string sql = "DELETE FROM MetadataMap_Attachments WHERE AttachmentID = @attachmentid;";
            var parameters = new Dictionary<string, object>
            {
                { "@attachmentid", attachmentId }
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
        /// <returns>The updated ContentViewModel representing the content attachment.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the attachment is not found, the user lacks permission to update it, or if invalid updates are attempted.</exception>
        public static async Task<ContentViewModel> UpdateMetadataItem(long attachmentId, Authentication.ApplicationUser user, bool? isShared = null, string? content = null)
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
                string sql = "UPDATE MetadataMap_Attachments SET IsShared = @isshared WHERE AttachmentID = @attachmentid;";
                var parameters = new Dictionary<string, object>
                {
                    { "@isshared", isShared.Value },
                    { "@attachmentid", attachmentId }
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
                        string sql = "UPDATE MetadataMap_Attachments SET Filename = @filename WHERE AttachmentID = @attachmentid;";
                        var parameters = new Dictionary<string, object>
                        {
                            { "@filename", content },
                            { "@attachmentid", attachmentId }
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
    }
}