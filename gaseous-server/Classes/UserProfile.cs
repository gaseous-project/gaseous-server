using System.Data;

namespace gaseous_server.Classes
{
    public class UserProfile
    {
        static readonly Dictionary<string, string> supportedImages = new Dictionary<string, string>{
            { ".png", "image/png" },
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".gif", "image/gif" },
            { ".bmp", "image/bmp" },
            { ".svg", "image/svg+xml" }
        };

        public Models.UserProfile? GetUserProfile(string UserId)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "SELECT Id, DisplayName, Quip, AvatarExtension, ProfileBackgroundExtension, UnstructuredData FROM UserProfiles WHERE Id = @userid;";
            Dictionary<string, object> dbDict = new Dictionary<string, object>{
                { "userid", UserId }
            };

            DataTable data = db.ExecuteCMD(sql, dbDict);

            if (data.Rows.Count == 0)
            {
                return null;
            }

            Models.UserProfile.ProfileImageItem? Avatar = null;
            if (data.Rows[0]["AvatarExtension"] != DBNull.Value)
            {
                Avatar = new Models.UserProfile.ProfileImageItem
                {
                    MimeType = supportedImages[data.Rows[0]["AvatarExtension"].ToString()],
                    Extension = data.Rows[0]["AvatarExtension"].ToString()
                };
            }

            Models.UserProfile.ProfileImageItem? ProfileBackground = null;
            if (data.Rows[0]["ProfileBackgroundExtension"] != DBNull.Value)
            {
                ProfileBackground = new Models.UserProfile.ProfileImageItem
                {
                    MimeType = supportedImages[data.Rows[0]["ProfileBackgroundExtension"].ToString()],
                    Extension = data.Rows[0]["ProfileBackgroundExtension"].ToString()
                };
            }

            return new Models.UserProfile
            {
                UserId = Guid.Parse(data.Rows[0]["Id"].ToString()),
                DisplayName = data.Rows[0]["DisplayName"].ToString(),
                Quip = data.Rows[0]["Quip"].ToString(),
                Avatar = Avatar,
                ProfileBackground = ProfileBackground,
                Data = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(data.Rows[0]["UnstructuredData"].ToString())
            };
        }

        public void UpdateUserProfile(string InternalUserId, Models.UserProfile profile)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "UPDATE UserProfiles SET DisplayName = @displayname, Quip = @quip, UnstructuredData = @data WHERE UserId = @internalId AND Id = @userid;";
            Dictionary<string, object> dbDict = new Dictionary<string, object>{
                { "displayname", profile.DisplayName },
                { "quip", profile.Quip },
                { "data", Newtonsoft.Json.JsonConvert.SerializeObject(profile.Data) },
                { "userid", profile.UserId },
                { "internalId", InternalUserId }
            };

            db.ExecuteCMD(sql, dbDict);
        }

        public enum ImageType
        {
            Avatar,
            Background
        }

        public void UpdateImage(ImageType imageType, string UserId, string InternalUserId, string Filename, byte[] bytes)
        {
            // check if it's a supported file type
            if (!supportedImages.ContainsKey(Path.GetExtension(Filename).ToLower()))
            {
                throw new Exception("File type not supported");
            }

            string ByteFieldName;
            string ExtensionFieldName;
            switch (imageType)
            {
                case ImageType.Avatar:
                    ByteFieldName = "Avatar";
                    ExtensionFieldName = "AvatarExtension";
                    break;
                case ImageType.Background:
                    ByteFieldName = "ProfileBackground";
                    ExtensionFieldName = "ProfileBackgroundExtension";
                    break;
                default:
                    throw new Exception("Invalid image type");
            }

            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = String.Format("UPDATE UserProfiles SET {0} = @content, {1} = @extension WHERE Id = @userid AND UserId = @internaluserid;", ByteFieldName, ExtensionFieldName);
            Dictionary<string, object> dbDict = new Dictionary<string, object>{
                { "content", bytes },
                { "extension", Path.GetExtension(Filename) },
                { "userid", UserId },
                { "internaluserid", InternalUserId }
            };

            db.ExecuteCMD(sql, dbDict);
        }

        public Models.ImageItem? GetImage(ImageType imageType, string UserId)
        {
            string ByteFieldName;
            string ExtensionFieldName;
            switch (imageType)
            {
                case ImageType.Avatar:
                    ByteFieldName = "Avatar";
                    ExtensionFieldName = "AvatarExtension";
                    break;
                case ImageType.Background:
                    ByteFieldName = "ProfileBackground";
                    ExtensionFieldName = "ProfileBackgroundExtension";
                    break;
                default:
                    throw new Exception("Invalid image type");
            }

            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = String.Format("SELECT {0}, {1} FROM UserProfiles WHERE Id = @userid;", ByteFieldName, ExtensionFieldName);
            Dictionary<string, object> dbDict = new Dictionary<string, object>{
                { "userid", UserId }
            };

            DataTable data = db.ExecuteCMD(sql, dbDict);

            if (data.Rows.Count == 0)
            {
                return null;
            }

            Models.ImageItem? image = new Models.ImageItem
            {
                content = data.Rows[0][ByteFieldName] as byte[],
                mimeType = supportedImages[data.Rows[0][ExtensionFieldName] as string],
                extension = data.Rows[0][ExtensionFieldName] as string
            };

            return image;
        }

        public void DeleteImage(ImageType imageType, string UserId)
        {
            string ByteFieldName;
            string ExtensionFieldName;
            switch (imageType)
            {
                case ImageType.Avatar:
                    ByteFieldName = "Avatar";
                    ExtensionFieldName = "AvatarExtension";
                    break;
                case ImageType.Background:
                    ByteFieldName = "ProfileBackground";
                    ExtensionFieldName = "ProfileBackgroundExtension";
                    break;
                default:
                    throw new Exception("Invalid image type");
            }

            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = String.Format("UPDATE UserProfiles SET {0} = NULL, {1} = NULL WHERE UserId = @userid;", ByteFieldName, ExtensionFieldName);
            Dictionary<string, object> dbDict = new Dictionary<string, object>{
                { "userid", UserId }
            };

            db.ExecuteCMD(sql, dbDict);
        }
    }
}
