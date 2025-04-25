namespace gaseous_server.Models
{
    public class ImportStateItem
    {
        public string FileName { get; set; }
        public ImportState State { get; set; } = ImportState.Pending;
        public ImportMethod Method { get; set; }
        public ImportType Type { get; set; } = ImportType.Unknown;
        public string UserId { get; set; } = string.Empty;
        public DateTime Created { get; } = DateTime.UtcNow;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public enum ImportState
        {
            Pending,
            Processing,
            Completed,
            Skipped,
            Failed
        }
        public string? ErrorMessage { get; set; } = null;
        public enum ImportMethod
        {
            ImportDirectory,
            WebUpload
        }
        public enum ImportType
        {
            Unknown,
            Rom,
            BIOS
        }
        public Dictionary<string, object>? ProcessData { get; set; } = new Dictionary<string, object>();
        public long? PlatformOverride { get; set; } = null;
        public Guid SessionId { get; set; }
    }
}