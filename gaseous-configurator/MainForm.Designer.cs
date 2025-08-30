namespace gaseous_configurator
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblHost = new System.Windows.Forms.Label();
            this.txtHost = new System.Windows.Forms.TextBox();
            this.lblPort = new System.Windows.Forms.Label();
            this.numPort = new System.Windows.Forms.NumericUpDown();
            this.lblUser = new System.Windows.Forms.Label();
            this.txtUser = new System.Windows.Forms.TextBox();
            this.lblPass = new System.Windows.Forms.Label();
            this.txtPass = new System.Windows.Forms.TextBox();
            this.lblDb = new System.Windows.Forms.Label();
            this.txtDb = new System.Windows.Forms.TextBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.btnStartService = new System.Windows.Forms.Button();
            this.lblPath = new System.Windows.Forms.Label();
            this.btnStopService = new System.Windows.Forms.Button();
            this.btnRestartService = new System.Windows.Forms.Button();
            this.btnRemoveService = new System.Windows.Forms.Button();
            this.btnOpenLogs = new System.Windows.Forms.Button();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.serviceStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.actionStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            ((System.ComponentModel.ISupportInitialize)(this.numPort)).BeginInit();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblHost
            // 
            this.lblHost.AutoSize = true;
            this.lblHost.Location = new System.Drawing.Point(12, 45);
            this.lblHost.Name = "lblHost";
            this.lblHost.Size = new System.Drawing.Size(53, 15);
            this.lblHost.TabIndex = 0;
            this.lblHost.Text = "Hostname";
            // 
            // txtHost
            // 
            this.txtHost.Location = new System.Drawing.Point(120, 42);
            this.txtHost.Name = "txtHost";
            this.txtHost.Size = new System.Drawing.Size(240, 23);
            this.txtHost.TabIndex = 1;
            // 
            // lblPort
            // 
            this.lblPort.AutoSize = true;
            this.lblPort.Location = new System.Drawing.Point(12, 77);
            this.lblPort.Name = "lblPort";
            this.lblPort.Size = new System.Drawing.Size(29, 15);
            this.lblPort.TabIndex = 2;
            this.lblPort.Text = "Port";
            // 
            // numPort
            // 
            this.numPort.Location = new System.Drawing.Point(120, 75);
            this.numPort.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.numPort.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numPort.Name = "numPort";
            this.numPort.Size = new System.Drawing.Size(120, 23);
            this.numPort.TabIndex = 3;
            this.numPort.Value = new decimal(new int[] {
            3306,
            0,
            0,
            0});
            // 
            // lblUser
            // 
            this.lblUser.AutoSize = true;
            this.lblUser.Location = new System.Drawing.Point(12, 109);
            this.lblUser.Name = "lblUser";
            this.lblUser.Size = new System.Drawing.Size(63, 15);
            this.lblUser.TabIndex = 4;
            this.lblUser.Text = "User name";
            // 
            // txtUser
            // 
            this.txtUser.Location = new System.Drawing.Point(120, 106);
            this.txtUser.Name = "txtUser";
            this.txtUser.Size = new System.Drawing.Size(240, 23);
            this.txtUser.TabIndex = 5;
            // 
            // lblPass
            // 
            this.lblPass.AutoSize = true;
            this.lblPass.Location = new System.Drawing.Point(12, 141);
            this.lblPass.Name = "lblPass";
            this.lblPass.Size = new System.Drawing.Size(57, 15);
            this.lblPass.TabIndex = 6;
            this.lblPass.Text = "Password";
            // 
            // txtPass
            // 
            this.txtPass.Location = new System.Drawing.Point(120, 138);
            this.txtPass.Name = "txtPass";
            this.txtPass.PasswordChar = '•';
            this.txtPass.Size = new System.Drawing.Size(240, 23);
            this.txtPass.TabIndex = 7;
            // 
            // lblDb
            // 
            this.lblDb.AutoSize = true;
            this.lblDb.Location = new System.Drawing.Point(12, 173);
            this.lblDb.Name = "lblDb";
            this.lblDb.Size = new System.Drawing.Size(90, 15);
            this.lblDb.TabIndex = 8;
            this.lblDb.Text = "Database name";
            // 
            // txtDb
            // 
            this.txtDb.Location = new System.Drawing.Point(120, 170);
            this.txtDb.Name = "txtDb";
            this.txtDb.Size = new System.Drawing.Size(240, 23);
            this.txtDb.TabIndex = 9;
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(416, 200);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(90, 30);
            this.btnSave.TabIndex = 11;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lblStatus.Location = new System.Drawing.Point(12, 240);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(0, 15);
            this.lblStatus.TabIndex = 13;
            // 
            // lblPath
            // 
            this.lblPath.AutoSize = false;
            this.lblPath.AutoEllipsis = true;
            this.lblPath.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lblPath.Location = new System.Drawing.Point(12, 12);
            this.lblPath.Name = "lblPath";
            this.lblPath.Size = new System.Drawing.Size(370, 15);
            this.lblPath.TabIndex = 14;
            // 
            // btnStartService
            // 
            this.btnStartService.Location = new System.Drawing.Point(204, 200);
            this.btnStartService.Name = "btnStartService";
            this.btnStartService.Size = new System.Drawing.Size(110, 30);
            this.btnStartService.TabIndex = 10;
            this.btnStartService.Text = "Start Service";
            this.btnStartService.UseVisualStyleBackColor = true;
            this.btnStartService.Click += new System.EventHandler(this.btnStartService_Click);
            // 
            // btnStopService
            // 
            this.btnStopService.Location = new System.Drawing.Point(108, 200);
            this.btnStopService.Name = "btnStopService";
            this.btnStopService.Size = new System.Drawing.Size(90, 30);
            this.btnStopService.TabIndex = 15;
            this.btnStopService.Text = "Stop";
            this.btnStopService.UseVisualStyleBackColor = true;
            this.btnStopService.Click += new System.EventHandler(this.btnStopService_Click);
            // 
            // btnRestartService
            // 
            this.btnRestartService.Location = new System.Drawing.Point(12, 200);
            this.btnRestartService.Name = "btnRestartService";
            this.btnRestartService.Size = new System.Drawing.Size(90, 30);
            this.btnRestartService.TabIndex = 16;
            this.btnRestartService.Text = "Restart";
            this.btnRestartService.UseVisualStyleBackColor = true;
            this.btnRestartService.Click += new System.EventHandler(this.btnRestartService_Click);
            // 
            // btnRemoveService
            // 
            this.btnRemoveService.Location = new System.Drawing.Point(320, 200);
            this.btnRemoveService.Name = "btnRemoveService";
            this.btnRemoveService.Size = new System.Drawing.Size(90, 30);
            this.btnRemoveService.TabIndex = 17;
            this.btnRemoveService.Text = "Remove";
            this.btnRemoveService.UseVisualStyleBackColor = true;
            this.btnRemoveService.Click += new System.EventHandler(this.btnRemoveService_Click);
            // 
            // btnOpenLogs
            // 
            this.btnOpenLogs.Location = new System.Drawing.Point(398, 8);
            this.btnOpenLogs.Name = "btnOpenLogs";
            this.btnOpenLogs.Size = new System.Drawing.Size(110, 26);
            this.btnOpenLogs.TabIndex = 18;
            this.btnOpenLogs.Text = "Open Logs";
            this.btnOpenLogs.UseVisualStyleBackColor = true;
            this.btnOpenLogs.Click += new System.EventHandler(this.btnOpenLogs_Click);
            // 
            // statusStrip
            // 
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.serviceStatusLabel,
            this.actionStatusLabel});
            this.statusStrip.Location = new System.Drawing.Point(0, 248);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(520, 22);
            this.statusStrip.SizingGrip = false;
            this.statusStrip.TabIndex = 19;
            this.statusStrip.Text = "statusStrip";
            // 
            // serviceStatusLabel
            // 
            this.serviceStatusLabel.ForeColor = System.Drawing.SystemColors.GrayText;
            this.serviceStatusLabel.Name = "serviceStatusLabel";
            this.serviceStatusLabel.Size = new System.Drawing.Size(0, 17);
            this.serviceStatusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // actionStatusLabel
            // 
            this.actionStatusLabel.ForeColor = System.Drawing.SystemColors.GrayText;
            this.actionStatusLabel.Name = "actionStatusLabel";
            this.actionStatusLabel.Size = new System.Drawing.Size(0, 17);
            this.actionStatusLabel.Spring = true;
            this.actionStatusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(520, 270);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.btnOpenLogs);
            this.Controls.Add(this.btnRemoveService);
            this.Controls.Add(this.btnRestartService);
            this.Controls.Add(this.btnStopService);
            this.Controls.Add(this.btnStartService);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.lblPath);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.txtDb);
            this.Controls.Add(this.lblDb);
            this.Controls.Add(this.txtPass);
            this.Controls.Add(this.lblPass);
            this.Controls.Add(this.txtUser);
            this.Controls.Add(this.lblUser);
            this.Controls.Add(this.numPort);
            this.Controls.Add(this.lblPort);
            this.Controls.Add(this.txtHost);
            this.Controls.Add(this.lblHost);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Gaseous Configurator";
            this.Load += new System.EventHandler(this.MainForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.numPort)).EndInit();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private System.Windows.Forms.Label lblHost;
        private System.Windows.Forms.TextBox txtHost;
        private System.Windows.Forms.Label lblPort;
        private System.Windows.Forms.NumericUpDown numPort;
        private System.Windows.Forms.Label lblUser;
        private System.Windows.Forms.TextBox txtUser;
        private System.Windows.Forms.Label lblPass;
        private System.Windows.Forms.TextBox txtPass;
        private System.Windows.Forms.Label lblDb;
        private System.Windows.Forms.TextBox txtDb;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Label lblStatus;
    private System.Windows.Forms.Button btnStartService;
    private System.Windows.Forms.Label lblPath;
    private System.Windows.Forms.Button btnStopService;
    private System.Windows.Forms.Button btnRestartService;
    private System.Windows.Forms.Button btnRemoveService;
    private System.Windows.Forms.Button btnOpenLogs;
    private System.Windows.Forms.StatusStrip statusStrip;
    private System.Windows.Forms.ToolStripStatusLabel serviceStatusLabel;
    private System.Windows.Forms.ToolStripStatusLabel actionStatusLabel;
    }
}
