using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;

namespace gaseous_configurator
{
    internal static class Program
    {
        private static Mutex? _singleInstanceMutex;

        private static bool IsAdministrator()
        {
            try
            {
                var wi = WindowsIdentity.GetCurrent();
                var wp = new WindowsPrincipal(wi);
                return wp.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        [STAThread]
        static void Main()
        {
            // Elevate if not running as admin
            if (OperatingSystem.IsWindows() && !IsAdministrator())
            {
                try
                {
                    var exe = Application.ExecutablePath;
                    var psi = new ProcessStartInfo(exe)
                    {
                        UseShellExecute = true,
                        Verb = "runas"
                    };
                    Process.Start(psi);
                }
                catch
                {
                    // user cancelled UAC or failed; exit silently
                }
                return;
            }

            // Ensure both configurator and service use a shared config path under ProgramData
            EnsureSharedConfigPath();

            // Single instance guard
            bool createdNew;
            _singleInstanceMutex = new Mutex(initiallyOwned: true, name: "Global\\GaseousConfigurator_SingleInstance", createdNew: out createdNew);
            if (!createdNew)
            {
                // Another instance is running; exit quietly
                return;
            }

            ApplicationConfiguration.Initialize();
            try
            {
                Application.Run(new MainForm());
            }
            finally
            {
                // Release single-instance mutex on exit
                _singleInstanceMutex?.ReleaseMutex();
                _singleInstanceMutex?.Dispose();
            }
    }

        private static void EnsureSharedConfigPath()
        {
            try
            {
                var sharedPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "gaseous-server");

                // Set for current process immediately so Config uses it this run
                Environment.SetEnvironmentVariable("GASEOUS_CONFIG_PATH", sharedPath, EnvironmentVariableTarget.Process);
                // Persist for the machine so the Windows service picks it up
                Environment.SetEnvironmentVariable("GASEOUS_CONFIG_PATH", sharedPath, EnvironmentVariableTarget.Machine);

                if (!System.IO.Directory.Exists(sharedPath))
                {
                    System.IO.Directory.CreateDirectory(sharedPath);
                }

                // Best-effort migration from prior per-user location if present and shared is missing
                var userPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gaseous-server");
                var userConfig = System.IO.Path.Combine(userPath, "config.json");
                var userPlatformMap = System.IO.Path.Combine(userPath, "platformmap.json");
                var sharedConfig = System.IO.Path.Combine(sharedPath, "config.json");
                var sharedPlatformMap = System.IO.Path.Combine(sharedPath, "platformmap.json");

                if (System.IO.File.Exists(userConfig) && !System.IO.File.Exists(sharedConfig))
                {
                    System.IO.File.Copy(userConfig, sharedConfig, overwrite: false);
                }
                if (System.IO.File.Exists(userPlatformMap) && !System.IO.File.Exists(sharedPlatformMap))
                {
                    System.IO.File.Copy(userPlatformMap, sharedPlatformMap, overwrite: false);
                }
            }
            catch
            {
                // Non-fatal: if setting env var fails, Config will fall back to per-user path
            }
        }
    }
}
