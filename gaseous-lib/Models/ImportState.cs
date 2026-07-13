namespace gaseous_server.Models
{
    public class ImportStateItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImportStateItem"/> class.
        /// </summary>
        public ImportStateItem()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImportStateItem"/> class by copying values from an existing instance.
        /// </summary>
        /// <param name="source">
        /// The source import state item to clone.
        /// </param>
        public ImportStateItem(ImportStateItem source)
        {
            FileName = source.FileName;
            State = source.State;
            Method = source.Method;
            Type = source.Type;
            UserId = source.UserId;
            Created = source.Created;
            LastUpdated = source.LastUpdated;
            ErrorMessage = source.ErrorMessage;
            ProcessData = source.ProcessData != null ? new Dictionary<string, object>(source.ProcessData) : null;
            AdditionalData = source.AdditionalData != null ? new Dictionary<string, object>(source.AdditionalData) : null;
            PlatformOverride = source.PlatformOverride;
            SessionId = source.SessionId;
        }

        /// <summary>
        /// Creates a deep copy of this import state item.
        /// </summary>
        /// <returns>
        /// A detached copy of the current import state item.
        /// </returns>
        public ImportStateItem Clone()
        {
            return new ImportStateItem(this);
        }

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
            Queued,
            Processing,
            Completed
        }
        public string? ErrorMessage { get; set; } = null;
        public enum ImportMethod
        {
            ImportDirectory,
            WebUpload,
            LibraryScan
        }
        public enum ImportType
        {
            Unknown,
            Rom,
            BIOS
        }
        public Dictionary<string, object>? ProcessData { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object>? AdditionalData { get; set; } = new Dictionary<string, object>();
        public long? PlatformOverride { get; set; } = null;
        public Guid SessionId { get; set; }
    }
}