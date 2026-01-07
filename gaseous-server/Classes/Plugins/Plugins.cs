namespace gaseous_server.Classes.Plugins
{
    /// <summary>
    /// Manages plugin functionality and definitions.
    /// </summary>
    public class PluginManagement
    {
        /// <summary>
        /// Defines the types of plugins that can be managed by the system.
        /// </summary>
        public enum PluginTypes
        {
            /// <summary>
            /// A plugin that provides logging functionality.
            /// </summary>
            LogProvider,

            /// <summary>
            /// A plugin that provides metadata functionality.
            /// </summary>
            MetadataProvider,

            /// <summary>
            /// A plugin that provides file signature functionality.
            /// </summary>
            FileSignatureProvider,

            /// <summary>
            /// A plugin of other types.
            /// </summary>
            Other
        }
    }
}