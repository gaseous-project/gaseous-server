namespace gaseous_server.Models
{
    public class UserProfile
    {
        public Guid UserId { get; set; }
        public string DisplayName { get; set; }
        public string Quip { get; set; }
        public NowPlayingItem? NowPlaying { get; set; }
        public class NowPlayingItem
        {
            public Game Game { get; set; }
            public HasheousClient.Models.Metadata.IGDB.Platform Platform { get; set; }
            public long Duration { get; set; }
        }
        public ProfileImageItem? Avatar { get; set; }
        public ProfileImageItem? ProfileBackground { get; set; }
        public Dictionary<string, object> Data { get; set; }
        public class ProfileImageItem
        {
            public string MimeType { get; set; }
            public string FileName { get; set; }
            public string Extension { get; set; }
        }
    }
}