using System.ComponentModel;

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
            /// A plugin that provides metadata proxy functionality.
            /// </summary>
            MetadataProxyProvider,

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

        /// <summary>
        /// Defines various image sizes and their corresponding resolutions for use in plugins that handle image processing.
        /// </summary>
        public static class ImageResize
        {
            /// <summary>
            /// Defines the different image sizes and their corresponding resolutions for use in plugins that handle image processing.
            /// </summary>
            public enum ImageSize
            {
                /// <summary>
                /// 90x128 Fit
                /// </summary>
                [Description("cover_small")]
                [Resolution(90, 128)]
                cover_small,

                /// <summary>
                /// 264x374 Fit
                /// </summary>
                [Description("cover_big")]
                [Resolution(264, 374)]
                cover_big,

                /// <summary>
                /// 165x90 Lfill, Centre gravity - resized by Gaseous and is not a real IGDB size
                /// </summary>
                [Description("screenshot_thumb")]
                [Resolution(165, 90)]
                screenshot_thumb,

                /// <summary>
                /// 235x128 Lfill, Centre gravity - resized by Gaseous and is not a real IGDB size
                /// </summary>
                [Description("screenshot_small")]
                [Resolution(235, 128)]
                screenshot_small,

                /// <summary>
                /// 589x320 Lfill, Centre gravity
                /// </summary>
                [Description("screenshot_med")]
                [Resolution(589, 320)]
                screenshot_med,

                /// <summary>
                /// 889x500 Lfill, Centre gravity
                /// </summary>
                [Description("screenshot_big")]
                [Resolution(889, 500)]
                screenshot_big,

                /// <summary>
                /// 1280x720 Lfill, Centre gravity
                /// </summary>
                [Description("screenshot_huge")]
                [Resolution(1280, 720)]
                screenshot_huge,

                /// <summary>
                /// 284x160 Fit
                /// </summary>
                [Description("logo_med")]
                [Resolution(284, 160)]
                logo_med,

                /// <summary>
                /// 90x90 Thumb, Centre gravity
                /// </summary>
                [Description("thumb")]
                [Resolution(90, 90)]
                thumb,

                /// <summary>
                /// 35x35 Thumb, Centre gravity
                /// </summary>
                [Description("micro")]
                [Resolution(35, 35)]
                micro,

                /// <summary>
                /// 1280x720 Fit, Centre gravity
                /// </summary>
                [Description("720p")]
                [Resolution(1280, 720)]
                r720p,

                /// <summary>
                /// 1920x1080 Fit, Centre gravity
                /// </summary>
                [Description("1080p")]
                [Resolution(1920, 1080)]
                r1080p,

                /// <summary>
                /// The originally uploaded image
                /// </summary>
                [Description("original")]
                [Resolution(0, 0)]
                original
            }


            /// <summary>
            /// Specifies a resolution for an image size enum
            /// </summary>
            [AttributeUsage(AttributeTargets.All)]
            public class ResolutionAttribute : Attribute
            {
                /// <summary>
                /// Defines a default resolution attribute with width and height set to 0, indicating the original image size.
                /// </summary>
                public static readonly ResolutionAttribute Default = new ResolutionAttribute();

                /// <summary>
                /// Initializes a new instance of the ResolutionAttribute class with default values (0, 0), indicating the original image size.
                /// </summary>
                public ResolutionAttribute() : this(0, 0)
                {
                }

                /// <summary>
                /// Initializes a new instance of the ResolutionAttribute class with the specified width and height.
                /// </summary>
                /// <param name="width">The width of the image resolution.</param>
                /// <param name="height">The height of the image resolution.</param>
                public ResolutionAttribute(int width, int height)
                {
                    ResolutionWidth = width;
                    ResolutionHeight = height;
                }

                /// <summary>
                /// Gets the width of the image resolution defined by this attribute. If the width is set to 0, it indicates that the original image width should be used.
                /// </summary>
                public virtual int width => ResolutionWidth;

                /// <summary>
                /// Gets the height of the image resolution defined by this attribute. If the height is set to 0, it indicates that the original image height should be used.
                /// </summary>
                public virtual int height => ResolutionHeight;

                /// <summary>
                /// Gets a value indicating whether this resolution is the original image size (i.e., both width and height are 0).
                /// </summary>
                protected int ResolutionWidth { get; set; }

                /// <summary>
                /// Gets a value indicating whether this resolution is the original image size (i.e., both width and height are 0).
                /// </summary>
                protected int ResolutionHeight { get; set; }
            }
        }
    }
}