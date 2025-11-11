namespace gaseous_server.Models
{
    /// <summary>
    /// Lightweight summary of a locale for selection lists.
    /// </summary>
    public class LocaleSummaryModel
    {
        /// <summary>
        /// Locale code (e.g. en, en-US, fr-FR)
        /// </summary>
        public string? Code { get; set; }
        /// <summary>
        /// English display name (optional if not available in file)
        /// </summary>
        public string? Name { get; set; }
        /// <summary>
        /// Native display name (optional)
        /// </summary>
        public string? NativeName { get; set; }
    }
}
