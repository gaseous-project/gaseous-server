using System;
using System.Drawing;
using System.Linq;
using System.ServiceProcess;
using System.Windows.Forms;
using gaseous_server.Classes;

namespace gaseous_configurator
{
    public partial class MainForm : Form
    {
        private readonly System.Windows.Forms.Timer _statusTimer = new System.Windows.Forms.Timer();
        private const string ServiceName = "Gaseous Server";

        public MainForm()
        {
            InitializeComponent();
            _statusTimer.Interval = 2000; // 2s
            _statusTimer.Tick += (s, e) => RefreshServiceStatus();
        }

        private void SetActionStatus(Color color, string message)
        {
            lblStatus.ForeColor = color;
            lblStatus.Text = message;
            actionStatusLabel.ForeColor = SystemColors.GrayText;
            actionStatusLabel.Text = message;
        }

        private void MainForm_Load(object? sender, EventArgs e)
        {
            // Display effective config path
            lblPath.Text = "Config: " + Config.ConfigurationPath;

            // Load current DB config values
            txtHost.Text = Config.DatabaseConfiguration.HostName;
            numPort.Value = Config.DatabaseConfiguration.Port;
            txtUser.Text = Config.DatabaseConfiguration.UserName;
            txtPass.Text = Config.DatabaseConfiguration.Password;
            txtDb.Text = Config.DatabaseConfiguration.DatabaseName;

            // Start status polling
            RefreshServiceStatus();
            _statusTimer.Start();
        }

        private void btnSave_Click(object? sender, EventArgs e)
        {
            // Update config and save
            Config.DatabaseConfiguration.HostName = txtHost.Text.Trim();
            Config.DatabaseConfiguration.Port = (int)numPort.Value;
            Config.DatabaseConfiguration.UserName = txtUser.Text.Trim();
            Config.DatabaseConfiguration.Password = txtPass.Text;
            Config.DatabaseConfiguration.DatabaseName = txtDb.Text.Trim();

            try
            {
                Config.UpdateConfig();
                SetActionStatus(Color.DarkGreen, "Saved config to " + Config.ConfigurationPath);
            }
            catch (Exception ex)
            {
                SetActionStatus(Color.DarkRed, "Failed to save: " + ex.Message);
            }
        }

        // Start or install the gaseous-server Windows service by referencing an existing executable
        private async void btnStartService_Click(object? sender, EventArgs e)
        {
            ServiceController? sc = null;
            try
            {
                sc = ServiceController.GetServices().FirstOrDefault(s => s.ServiceName.Equals(ServiceName, StringComparison.OrdinalIgnoreCase));
                if (sc == null)
                {
                    SetActionStatus(Color.DarkGray, "Installing service...");

                    // Locate gaseous-server.exe; prefer the same directory as the configurator
                    var baseDir = System.IO.Path.GetDirectoryName(Application.ExecutablePath) ?? AppContext.BaseDirectory;
                    string exePath = System.IO.Path.Combine(baseDir, "gaseous-server.exe");

                    if (!System.IO.File.Exists(exePath))
                    {
                        // Try common dev locations as a convenience
                        var candidates = new[]
                        {
                            System.IO.Path.Combine(baseDir, "..", "gaseous-server", "bin", "Release", "net8.0", "gaseous-server.exe"),
                            System.IO.Path.Combine(baseDir, "..", "gaseous-server", "bin", "Debug", "net8.0", "gaseous-server.exe")
                        };
                        exePath = candidates.FirstOrDefault(System.IO.File.Exists) ?? exePath;
                    }

                    if (!System.IO.File.Exists(exePath))
                    {
                        using var ofd = new OpenFileDialog
                        {
                            Title = "Locate gaseous-server.exe",
                            Filter = "gaseous-server.exe|gaseous-server.exe|Executable (*.exe)|*.exe",
                            CheckFileExists = true,
                            Multiselect = false
                        };
                        if (ofd.ShowDialog(this) == DialogResult.OK)
                        {
                            exePath = ofd.FileName;
                        }
                        else
                        {
                            throw new Exception("Server executable not found. Please install gaseous-server and try again.");
                        }
                    }

                    // Create the service via sc.exe create referencing the existing executable
                    var scCreate = new System.Diagnostics.ProcessStartInfo("sc.exe", $"create \"{ServiceName}\" binPath= \"{exePath}\" start= auto DisplayName= \"{ServiceName}\"")
                    {
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };
                    using (var p2 = System.Diagnostics.Process.Start(scCreate))
                    {
                        if (p2 == null) throw new Exception("Failed to start service install");
                        await p2.WaitForExitAsync();
                        if (p2.ExitCode != 0)
                        {
                            var err = await p2.StandardError.ReadToEndAsync();
                            throw new Exception("Service install failed: " + err);
                        }
                    }

                    // Refresh controller
                    sc = new ServiceController(ServiceName);
                }

                // Start service if not running
                sc.Refresh();
                if (sc.Status == ServiceControllerStatus.Stopped || sc.Status == ServiceControllerStatus.Paused)
                {
                    SetActionStatus(Color.DarkGray, "Starting service...");
                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                }

                SetActionStatus(Color.DarkGreen, "Service running");
                RefreshServiceStatus();
            }
            catch (Exception ex)
            {
                SetActionStatus(Color.DarkRed, "Service action failed: " + ex.Message);
            }
            finally
            {
                sc?.Dispose();
            }
        }

        private void btnStopService_Click(object? sender, EventArgs e)
        {
            try
            {
                using var sc = new ServiceController(ServiceName);
                sc.Refresh();
                if (sc.Status == ServiceControllerStatus.Running || sc.Status == ServiceControllerStatus.Paused)
                {
                    SetActionStatus(Color.DarkGray, "Stopping service...");
                    sc.Stop();
                    sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                }
                SetActionStatus(Color.DarkGreen, "Service stopped");
            }
            catch (Exception ex)
            {
                SetActionStatus(Color.DarkRed, "Stop failed: " + ex.Message);
            }
            finally
            {
                RefreshServiceStatus();
            }
        }

        private void btnRestartService_Click(object? sender, EventArgs e)
        {
            try
            {
                using var sc = new ServiceController(ServiceName);
                sc.Refresh();
                SetActionStatus(Color.DarkGray, "Restarting service...");
                if (sc.Status == ServiceControllerStatus.Running || sc.Status == ServiceControllerStatus.Paused)
                {
                    sc.Stop();
                    sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                }
                sc.Start();
                sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                SetActionStatus(Color.DarkGreen, "Service running");
            }
            catch (Exception ex)
            {
                SetActionStatus(Color.DarkRed, "Restart failed: " + ex.Message);
            }
            finally
            {
                RefreshServiceStatus();
            }
        }

        private void RefreshServiceStatus()
        {
            try
            {
                using var sc = new ServiceController(ServiceName);
                sc.Refresh();
                var status = sc.Status;
                var text = status.ToString();

                // Reflect buttons enabled state
                btnStartService.Enabled = status == ServiceControllerStatus.Stopped || status == ServiceControllerStatus.Paused;
                btnStopService.Enabled = status == ServiceControllerStatus.Running || status == ServiceControllerStatus.Paused;
                btnRestartService.Enabled = status == ServiceControllerStatus.Running || status == ServiceControllerStatus.Paused;

                // Show status in status bar
                serviceStatusLabel.ForeColor = SystemColors.GrayText;
                serviceStatusLabel.Text = "Service: " + text;
            }
            catch
            {
                // Service likely not installed
                btnStartService.Enabled = true;
                btnStopService.Enabled = false;
                btnRestartService.Enabled = false;
                serviceStatusLabel.ForeColor = SystemColors.GrayText;
                serviceStatusLabel.Text = "Service: Not installed";
            }
        }

        private void btnRemoveService_Click(object? sender, EventArgs e)
        {
            try
            {
                // Stop first if running
                using (var sc = new ServiceController(ServiceName))
                {
                    try
                    {
                        sc.Refresh();
                        if (sc.Status == ServiceControllerStatus.Running || sc.Status == ServiceControllerStatus.Paused)
                        {
                            SetActionStatus(Color.DarkGray, "Stopping service...");
                            sc.Stop();
                            sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                        }
                    }
                    catch { /* ignore if not installed */ }
                }

                // sc.exe delete
                var psi = new System.Diagnostics.ProcessStartInfo("sc.exe", $"delete \"{ServiceName}\"")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                using (var p = System.Diagnostics.Process.Start(psi))
                {
                    if (p == null) throw new Exception("Failed to start service removal");
                    p.WaitForExit();
                    if (p.ExitCode != 0)
                    {
                        var err = p.StandardError.ReadToEnd();
                        throw new Exception("Remove failed: " + err);
                    }
                }

                SetActionStatus(Color.DarkGreen, "Service removed");
            }
            catch (Exception ex)
            {
                SetActionStatus(Color.DarkRed, ex.Message);
            }
            finally
            {
                RefreshServiceStatus();
            }
        }

        private void btnOpenLogs_Click(object? sender, EventArgs e)
        {
            try
            {
                var logs = System.IO.Path.Combine(Config.LogPath);
                if (!System.IO.Directory.Exists(logs))
                {
                    System.IO.Directory.CreateDirectory(logs);
                }
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = logs,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch (Exception ex)
            {
                SetActionStatus(Color.DarkRed, "Open logs failed: " + ex.Message);
            }
        }
    }
}
