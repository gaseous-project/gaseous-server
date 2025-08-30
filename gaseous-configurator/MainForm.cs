using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.ServiceProcess;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using gaseous_server.Classes;

namespace gaseous_configurator
{
    public partial class MainForm : Form
    {
        private readonly System.Windows.Forms.Timer _statusTimer = new System.Windows.Forms.Timer();
        private const string ServiceName = "Gaseous Server";
        private static readonly HttpClient _http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
        {
            Timeout = TimeSpan.FromSeconds(2)
        };
    private bool _statusBusy;

        public MainForm()
        {
            InitializeComponent();
            _statusTimer.Interval = 2000; // 2s
            _statusTimer.Tick += async (s, e) => await RefreshServiceStatusAsync();
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
            // Load server web port
            try { numWebPort.Value = Math.Clamp(Config.ServerPort, 1, 65535); } catch { numWebPort.Value = 5198; }

            // Start status polling
            _ = RefreshServiceStatusAsync();
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
            Config.ServerPort = (int)numWebPort.Value;

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
                await RefreshServiceStatusAsync();
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
                _ = RefreshServiceStatusAsync();
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
                _ = RefreshServiceStatusAsync();
            }
        }

        private async Task RefreshServiceStatusAsync()
        {
            if (_statusBusy) return;
            _statusBusy = true;
            try
            {
                using var sc = new ServiceController(ServiceName);
                sc.Refresh();
                var status = sc.Status;
                string text;

                // Reflect buttons enabled state
                btnStartService.Enabled = status == ServiceControllerStatus.Stopped || status == ServiceControllerStatus.Paused;
                btnStopService.Enabled = status == ServiceControllerStatus.Running || status == ServiceControllerStatus.Paused;
                btnRestartService.Enabled = status == ServiceControllerStatus.Running || status == ServiceControllerStatus.Paused;

                // Settings should be read-only while the service is running. Also lock while transitioning.
                var allowEdit = status == ServiceControllerStatus.Stopped;
                SetConfigInputsEnabled(allowEdit);

                // Status text mapping per requirement
                if (status == ServiceControllerStatus.StartPending)
                {
                    text = "Starting";
                }
                else if (status == ServiceControllerStatus.Running)
                {
                    // Try to find listening port for the service process
                    var pid = GetServiceProcessId(ServiceName);
                    int? port = null;
                    if (pid.HasValue && pid.Value > 0)
                    {
                        port = await TryGetListeningPortAsync(pid.Value);
                    }

                    if (port.HasValue)
                    {
                        // Probe health endpoint
                        var ready = await IsHostReadyAsync(port.Value);
                        text = ready ? $"Started - Port {port.Value}" : "Started - waiting for host";
                    }
                    else
                    {
                        text = "Started - waiting for host";
                    }
                }
                else
                {
                    text = status.ToString();
                }

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
                SetConfigInputsEnabled(true);
                serviceStatusLabel.ForeColor = SystemColors.GrayText;
                serviceStatusLabel.Text = "Service: Not installed";
            }
            finally
            {
                _statusBusy = false;
            }
        }

        private async void btnRemoveService_Click(object? sender, EventArgs e)
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
                            // Wait for a clean stop with a generous timeout
                            await WaitForServiceStoppedAsync(TimeSpan.FromMinutes(2));
                        }
                        else if (sc.Status == ServiceControllerStatus.StopPending || sc.Status == ServiceControllerStatus.StartPending)
                        {
                            SetActionStatus(Color.DarkGray, "Waiting for service to stop...");
                            await WaitForServiceStoppedAsync(TimeSpan.FromMinutes(2));
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
                await RefreshServiceStatusAsync();
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

        private void SetConfigInputsEnabled(bool enabled)
        {
            txtHost.ReadOnly = !enabled;
            txtUser.ReadOnly = !enabled;
            txtPass.ReadOnly = !enabled;
            txtDb.ReadOnly = !enabled;
            numPort.Enabled = enabled;
            numWebPort.Enabled = enabled;
            btnSave.Enabled = enabled;
        }

        private void btnOpenBrowser_Click(object? sender, EventArgs e)
        {
            try
            {
                var port = (int)numWebPort.Value;
                var url = $"http://localhost:{port}/";
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
                SetActionStatus(Color.Gray, $"Launching {url}");
            }
            catch (Exception ex)
            {
                SetActionStatus(Color.DarkRed, "Open web failed: " + ex.Message);
            }
        }

        private async Task<bool> IsHostReadyAsync(int port)
        {
            try
            {
                var url = $"http://localhost:{port}/api/v1.1/HealthCheck";
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                using var resp = await _http.SendAsync(req);
                var code = (int)resp.StatusCode;
                // Consider 2xx and 3xx as "ready" (many setups redirect HTTP->HTTPS)
                return code >= 200 && code < 400;
            }
            catch
            {
                return false;
            }
        }

        private async Task<int?> TryGetListeningPortAsync(int pid)
        {
            try
            {
                var psi = new ProcessStartInfo("netstat.exe", "-ano -p tcp")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                using var p = Process.Start(psi);
                if (p == null) return null;
                var output = await p.StandardOutput.ReadToEndAsync();
                await p.WaitForExitAsync();

                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                var re = new Regex("^\\s*TCP\\s+(\\S+):(\\d+)\\s+(\\S+):(\\*|\\d+)\\s+LISTENING\\s+(\\d+)\\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                var candidates = lines
                    .Select(l => re.Match(l))
                    .Where(m => m.Success && int.TryParse(m.Groups[5].Value, out var pidCol) && pidCol == pid)
                    .Select(m => new { Local = m.Groups[1].Value, Port = int.Parse(m.Groups[2].Value) })
                    .ToList();
                if (!candidates.Any()) return null;

                // Prefer 5000/5001 if present, else lowest port
                var preferred = candidates.FirstOrDefault(c => c.Port == 5000) ?? candidates.FirstOrDefault(c => c.Port == 5001);
                if (preferred != null) return preferred.Port;
                return candidates.Min(c => c.Port);
            }
            catch
            {
                return null;
            }
        }

        private async Task WaitForServiceStoppedAsync(TimeSpan timeout)
        {
            var start = DateTime.UtcNow;
            while (DateTime.UtcNow - start < timeout)
            {
                try
                {
                    using var sc = new ServiceController(ServiceName);
                    sc.Refresh();
                    if (sc.Status == ServiceControllerStatus.Stopped)
                    {
                        return;
                    }
                    await Task.Delay(1000);
                }
                catch
                {
                    // Service might be gone already
                    return;
                }
            }
        }

        private int? GetServiceProcessId(string name)
        {
            try
            {
                // Use SC to query the service extended info and parse PID
                var psi = new ProcessStartInfo("sc.exe", $"queryex \"{name}\"")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                using var p = Process.Start(psi);
                if (p == null) return null;
                var output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                // Look for a line like: PID                : 1234
                var m = Regex.Match(output, @"PID\s*:\s*(\d+)", RegexOptions.IgnoreCase);
                if (m.Success && int.TryParse(m.Groups[1].Value, out var pid)) return pid;
            }
            catch
            {
                // ignore
            }
            return null;
        }
    }
}
