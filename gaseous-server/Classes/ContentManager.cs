namespace gaseous_server.Classes.Content
{
    /// <summary>
    /// Provides configuration and access to different types of user-submitted or managed content.
    /// </summary>
    public class ContentManager
    {
        /// <summary>
        /// Enumerates the supported types of content.
        /// </summary>
        public enum ContentType
        {
            /// <summary>Screenshots or still images.</summary>
            Screenshot = 0,
            /// <summary>Video content.</summary>
            Video = 1,
            /// <summary>Short audio sample content (e.g., previews, sound bites).</summary>
            AudioSample = 2,
            /// <summary>Manuals or documentation files. Available to all users.</summary>
            GlobalManual = 3,
            /// <summary>Miscellaneous content not covered by other types.</summary>
            Misc = 100
        }

        /// <summary>
        /// Configuration for a specific content type, including platform restrictions.
        /// </summary>
        public class ContentConfiguration
        {
            /// <summary>
            /// Optional list of platform IDs this content type is limited to; an empty list means no restriction.
            /// </summary>
            public List<long> LimitToPlatformIds { get; set; } = new List<long>(); // empty list means no limit

            /// <summary>
            /// Optional list of user roles allowed to manage this content type; an empty list means no role restriction.
            /// </summary>
            public List<string> AllowedRoles { get; set; } = new List<string>(); // empty list means no role restriction

            /// <summary>
            /// Defines if the content type is user or system managed. Default is user managed. If system managed, AllowedRoles must contain "Admin".
            /// </summary>
            public bool IsUserManaged { get; set; } = true; // if false, AllowedRoles must contain "Admin"

            /// <summary>
            /// Defines if the content is shareable between users. Default is false. If true, content will only be viewable via an ACL check (not implemented yet).
            /// </summary>
            public bool IsShareable { get; set; } = false; // if true, content will only be viewable via an ACL check (not implemented yet)
        }

        private static readonly Dictionary<ContentType, ContentConfiguration> _contentConfigurations = new()
        {
            { ContentType.Screenshot, new ContentConfiguration() {
                LimitToPlatformIds = new List<long>(),
                IsUserManaged = true,
                IsShareable = true
            } },
            { ContentType.Video, new ContentConfiguration() {
                LimitToPlatformIds = new List<long>(),
                IsUserManaged = true,
                IsShareable = true
            } },
            { ContentType.AudioSample, new ContentConfiguration() {
                LimitToPlatformIds = new List<long>{ 52 },
                AllowedRoles = new List<string>{"Admin"},
                IsUserManaged = false
            } },
            { ContentType.GlobalManual, new ContentConfiguration() {
                LimitToPlatformIds = new List<long>(),
                AllowedRoles = new List<string>{"Admin" },
                IsUserManaged = false
            } },
            { ContentType.Misc, new ContentConfiguration() {
                LimitToPlatformIds = new List<long>()
            } }
        };

        /// <summary>
        /// Read-only access to the configured content type definitions.
        /// </summary>
        public static IReadOnlyDictionary<ContentType, ContentConfiguration> ContentConfigurations => _contentConfigurations;

        /// <summary>
        /// Checks if a given content type is allowed to be uploaded. If no rule is defned, it is allowed by default.
        /// </summary>
        /// <param name="contentType">The content type to check.</param>
        /// <param name="platformId">Optional platform ID to check against platform restrictions.</param>
        /// <param name="userRoles">Optional list of user roles to check against role restrictions.</param>
        /// <returns>True if the content type is allowed under the given conditions; otherwise, false.</returns>
        public static bool IsContentTypeUploadable(ContentType contentType, long? platformId = null, List<string>? userRoles = null)
        {
            if (!_contentConfigurations.TryGetValue(contentType, out var config))
            {
                // If no configuration exists for the content type, allow by default
                return true;
            }
            // Check platform restrictions
            if (config.LimitToPlatformIds.Any() && platformId.HasValue && !config.LimitToPlatformIds.Contains(platformId.Value))
            {
                return false;
            }
            // Check role restrictions
            if (config.AllowedRoles.Any() && (userRoles == null || !userRoles.Intersect(config.AllowedRoles).Any()))
            {
                return false;
            }
            return true;
        }

    }
}