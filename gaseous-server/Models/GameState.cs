namespace gaseous_server.Models
{
    public class UploadStateModel
    {
        public string ScreenshotByteArrayBase64 { get; set; }
        public string StateByteArrayBase64 { get; set; }
        public byte[] ScreenshotByteArray
        {
            get
            {
                return Convert.FromBase64String(ScreenshotByteArrayBase64);
            }
        }
        public byte[] StateByteArray
        {
            get
            {
                return Convert.FromBase64String(StateByteArrayBase64);
            }
        }
    }

    public class GameStateItem
    {
        public long Id { get; set; }
        public string Name = "";
        public DateTime SaveTime { get; set; }
        public bool HasScreenshot { get; set; }
    }
}