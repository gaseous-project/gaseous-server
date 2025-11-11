using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace gaseous_server.Models
{
    /// <summary>
    /// Represents a locale item containing display names, code, and internal status for localization management.
    /// </summary>
    public class LocaleFileItem
    {
        /// <summary>
        /// Gets or sets the English display name for the locale item.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the native (localized) display name for the locale item.
        /// </summary>
        public string? NativeName { get; set; }

        /// <summary>
        /// Gets or sets the locale code (e.g., "en-US", "fr-FR") for this item.
        /// </summary>
        public string? Code { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this locale item is internal-only.
        /// </summary>
        public bool IsInternal { get; set; }
    }

    /// <summary>
    /// Represents a locale file model containing localization data and metadata for internationalization support.
    /// </summary>
    public class LocaleFileModel
    {
        /// <summary>
        /// Gets or sets the display name of the locale in English.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the native name of the locale in its own language.
        /// </summary>
        public string? NativeName { get; set; }

        /// <summary>
        /// Gets or sets the locale code (e.g., "en-US", "fr-FR").
        /// </summary>
        public string? Code { get; set; }

        /// <summary>
        /// Gets the parent language code extracted from the locale code (e.g., "en" from "en-US").
        /// </summary>
        public string ParentLanguage
        {
            get
            {
                if (string.IsNullOrEmpty(Code))
                {
                    return string.Empty;
                }

                var parts = Code.Split('-');
                return parts[0];
            }
        }

        /// <summary>
        /// Gets or sets the plural rule identifier for determining plural forms in this locale.
        /// </summary>
        public string? PluralRule { get; set; }

        /// <summary>
        /// Gets or sets advanced plural rules mapping category name to boolean expression over n. Example:
        /// { "one": "n == 1", "few": "n &gt;= 2 &amp;&amp; n &lt;= 4", "many": "n &gt;= 5", "other": "n == 0" }
        /// If provided this takes precedence over the legacy PluralRule (binary singular/plural).
        /// </summary>
        public Dictionary<string, string>? PluralRules { get; set; }

        /// <summary>
        /// Gets or sets the text direction for the locale (e.g., "ltr" for left-to-right, "rtl" for right-to-left).
        /// </summary>
        public string? Direction { get; set; }

        /// <summary>
        /// Gets or sets the type of locale file, indicating whether it's a base locale or an overlay.
        /// </summary>
        public LocaleFileType? Type { get; set; }

        /// <summary>
        /// Gets or sets a dictionary containing key-value pairs of localized strings.
        /// </summary>
        public Dictionary<string, string>? Strings { get; set; }

        /// <summary>
        /// Gets or sets a dictionary containing server-specific localized strings. Used for localization on the server side in system logs and messages. Note: this is independent of client-facing strings, and only used for logging server messages.
        /// </summary>
        public Dictionary<string, string>? ServerStrings { get; set; }

        /// <summary>
        /// Defines the types of locale files available in the system.
        /// </summary>
        public enum LocaleFileType
        {
            /// <summary>
            /// Represents a base locale file containing the primary localization data.
            /// </summary>
            Base,

            /// <summary>
            /// Represents an overlay locale file that extends or overrides a base locale.
            /// </summary>
            Overlay
        }
    }
}