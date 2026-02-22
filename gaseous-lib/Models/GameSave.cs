namespace gaseous_server.Models
{
    public class UploadSaveModel
    {
        public string SaveByteArrayBase64 { get; set; }
        public byte[] SaveByteArray
        {
            get
            {
                return Convert.FromBase64String(SaveByteArrayBase64);
            }
        }
    }

    public class GameSaveItem
    {
        public long Id { get; set; }
        public DateTime SaveTime { get; set; }
    }
}