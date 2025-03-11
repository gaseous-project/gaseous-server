using System.Data;
using gaseous_server.Classes.Metadata;
using gaseous_server.Models;
using HasheousClient.Models;

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
            // build the user profile object
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "SELECT Id, UserId, DisplayName, Quip, AvatarHash, AvatarExtension, ProfileBackgroundHash, ProfileBackgroundExtension, UnstructuredData FROM UserProfiles WHERE Id = @userid;";
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
                    FileName = data.Rows[0]["AvatarHash"].ToString(),
                    Extension = data.Rows[0]["AvatarExtension"].ToString()
                };
            }

            Models.UserProfile.ProfileImageItem? ProfileBackground = null;
            if (data.Rows[0]["ProfileBackgroundExtension"] != DBNull.Value)
            {
                ProfileBackground = new Models.UserProfile.ProfileImageItem
                {
                    MimeType = supportedImages[data.Rows[0]["ProfileBackgroundExtension"].ToString()],
                    FileName = data.Rows[0]["ProfileBackgroundHash"].ToString(),
                    Extension = data.Rows[0]["ProfileBackgroundExtension"].ToString()
                };
            }

            // get now playing game - if available
            Models.UserProfile.NowPlayingItem? NowPlaying = null;
            sql = "SELECT * FROM `view_UserTimeTracking` WHERE UserId = @userid AND UTC_TIMESTAMP() BETWEEN SessionTime AND DATE_ADD(SessionEnd, INTERVAL 1 MINUTE) ORDER BY SessionEnd DESC LIMIT 1;";
            dbDict = new Dictionary<string, object>{
                { "userid", data.Rows[0]["UserId"].ToString() }
            };
            DataTable nowPlayingData = db.ExecuteCMD(sql, dbDict);
            if (nowPlayingData.Rows.Count > 0)
            {
                try
                {
                    gaseous_server.Models.MetadataMap.MetadataMapItem metadataMap = Classes.MetadataManagement.GetMetadataMap((long)nowPlayingData.Rows[0]["GameId"]).PreferredMetadataMapItem;
                    NowPlaying = new Models.UserProfile.NowPlayingItem
                    {
                        Game = Games.GetGame(metadataMap.SourceType, metadataMap.SourceId),
                        Platform = Platforms.GetPlatform((long)nowPlayingData.Rows[0]["PlatformId"]),
                        Duration = Convert.ToInt64(nowPlayingData.Rows[0]["SessionLength"])
                    };
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            // return the user profile object
            return new Models.UserProfile
            {
                UserId = Guid.Parse(data.Rows[0]["Id"].ToString()),
                DisplayName = data.Rows[0]["DisplayName"].ToString(),
                Quip = data.Rows[0]["Quip"].ToString(),
                Avatar = Avatar,
                ProfileBackground = ProfileBackground,
                NowPlaying = NowPlaying,
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
            string FileNameFieldName;
            string ExtensionFieldName;
            switch (imageType)
            {
                case ImageType.Avatar:
                    ByteFieldName = "Avatar";
                    FileNameFieldName = "AvatarHash";
                    ExtensionFieldName = "AvatarExtension";
                    break;
                case ImageType.Background:
                    ByteFieldName = "ProfileBackground";
                    FileNameFieldName = "ProfileBackgroundHash";
                    ExtensionFieldName = "ProfileBackgroundExtension";
                    break;
                default:
                    throw new Exception("Invalid image type");
            }

            string fileHash = BitConverter.ToString(System.Security.Cryptography.SHA256.HashData(bytes)).Replace("-", "").ToLower();

            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = String.Format("UPDATE UserProfiles SET {0} = @content, {1} = @filehash, {2} = @extension WHERE Id = @userid AND UserId = @internaluserid;", ByteFieldName, FileNameFieldName, ExtensionFieldName);
            Dictionary<string, object> dbDict = new Dictionary<string, object>{
                { "content", bytes },
                { "filehash", fileHash },
                { "extension", Path.GetExtension(Filename) },
                { "userid", UserId },
                { "internaluserid", InternalUserId }
            };

            db.ExecuteCMD(sql, dbDict);
        }

        public Models.ImageItem? GetImage(ImageType imageType, string UserId)
        {
            string ByteFieldName;
            string FileNameFieldName;
            string ExtensionFieldName;
            switch (imageType)
            {
                case ImageType.Avatar:
                    ByteFieldName = "Avatar";
                    FileNameFieldName = "AvatarHash";
                    ExtensionFieldName = "AvatarExtension";
                    break;
                case ImageType.Background:
                    ByteFieldName = "ProfileBackground";
                    FileNameFieldName = "ProfileBackgroundHash";
                    ExtensionFieldName = "ProfileBackgroundExtension";
                    break;
                default:
                    throw new Exception("Invalid image type");
            }

            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = String.Format("SELECT {0}, {1}, {2} FROM UserProfiles WHERE Id = @userid;", ByteFieldName, FileNameFieldName, ExtensionFieldName);
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
                fileName = data.Rows[0][FileNameFieldName] as string,
                extension = data.Rows[0][ExtensionFieldName] as string
            };

            return image;
        }

        public void DeleteImage(ImageType imageType, string UserId)
        {
            string ByteFieldName;
            string FileNameFieldName;
            string ExtensionFieldName;
            switch (imageType)
            {
                case ImageType.Avatar:
                    ByteFieldName = "Avatar";
                    FileNameFieldName = "AvatarHash";
                    ExtensionFieldName = "AvatarExtension";
                    break;
                case ImageType.Background:
                    ByteFieldName = "ProfileBackground";
                    FileNameFieldName = "ProfileBackgroundHash";
                    ExtensionFieldName = "ProfileBackgroundExtension";
                    break;
                default:
                    throw new Exception("Invalid image type");
            }

            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = String.Format("UPDATE UserProfiles SET {0} = NULL, {1} = NULL, {2} = NULL WHERE UserId = @userid;", ByteFieldName, FileNameFieldName, ExtensionFieldName);
            Dictionary<string, object> dbDict = new Dictionary<string, object>{
                { "userid", UserId }
            };

            db.ExecuteCMD(sql, dbDict);
        }
    }
}
