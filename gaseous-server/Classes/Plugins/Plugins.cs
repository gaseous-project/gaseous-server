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

        /// <summary>
        /// Represents the different operating systems that plugins can run on.
        /// </summary>
        public enum OperatingSystems
        {
            /// <summary>
            /// An unknown or unspecified operating system.
            /// </summary>
            Unknown,

            /// <summary>
            /// macOS operating system.
            /// </summary>
            macOS,

            /// <summary>
            /// Linux operating system.
            /// </summary>
            Linux,

            /// <summary>
            /// Windows operating system.
            /// </summary>
            Windows
        }

        /// <summary>
        /// Gets the current operating system.
        /// </summary>
        /// <returns>The current operating system.</returns>
        public static OperatingSystems GetCurrentOperatingSystem()
        {
            if (OperatingSystem.IsMacOS())
            {
                return OperatingSystems.macOS;
            }
            else if (OperatingSystem.IsLinux())
            {
                return OperatingSystems.Linux;
            }
            else if (OperatingSystem.IsWindows())
            {
                return OperatingSystems.Windows;
            }
            else
            {
                return OperatingSystems.Unknown;
            }
        }

        /// <summary>
        /// Determines if the current process is running as a Windows Service. Returns false on non-Windows platforms
        /// or when detection fails. Used to decide between console output and Windows Event Log writes.
        /// </summary>
        public static bool IsRunningAsWindowsService()
        {
            if (!OperatingSystem.IsWindows())
                return false;

            try
            {
                // Only available on Windows; analyzer suppression as we guard at runtime
#pragma warning disable CA1416
                return Microsoft.Extensions.Hosting.WindowsServices.WindowsServiceHelpers.IsWindowsService();
#pragma warning restore CA1416
            }
            catch
            {
                return false;
            }
        }
    }
}