namespace gaseous_server.Models
{
    public class UserProfile
    {
        public Guid UserId { get; set; }
        public string DisplayName { get; set; }
        public string Quip { get; set; }
        public ProfileImageItem? Avatar { get; set; }
        public ProfileImageItem? ProfileBackground { get; set; }
        public Dictionary<string, object> Data { get; set; }
        public class ProfileImageItem
        {
            public string MimeType { get; set; }
            public string Extension { get; set; }
        }
    }
}