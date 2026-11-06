using System;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;
using System.Net;
using System.IO;
using System.Collections.Generic;

namespace GVPN
{
    public partial class VpnTrayApplication : Form
    {
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private Thread vpnMonitorThread;
        private bool isRunning = false;
        private bool isConnected = false;
        private bool killSwitchEnabled = false;
        private Logger logger;
        private VpnManager vpnManager;

        public VpnTrayApplication()
        {
            logger = new Logger("vpn.log");
            vpnManager = new VpnManager(logger);

            // Minimal form setup - will be hidden
            this.Text = "GVpn";
            this.Icon = CreateIcon();
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.WindowState = FormWindowState.Minimized;
            this.Size = new System.Drawing.Size(1, 1);
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new System.Drawing.Point(-32000, -32000);

            // Setup tray icon
            SetupTrayIcon();

            // Start monitoring
            isRunning = true;
            vpnMonitorThread = new Thread(VpnMonitorLoop)
            {
                IsBackground = true,
                Name = "VpnMonitor"
            };
            vpnMonitorThread.Start();

            logger.Log("Application started");
        }

        private void SetupTrayIcon()
        {
            trayMenu = new ContextMenuStrip();

            var statusItem = new ToolStripMenuItem("Status: Disconnected");
            statusItem.Name = "StatusItem";
            statusItem.Enabled = false;
            trayMenu.Items.Add(statusItem);

            trayMenu.Items.Add(new ToolStripSeparator());

            var connectItem = new ToolStripMenuItem("&Connect Now", null, ConnectManual_Click);
            trayMenu.Items.Add(connectItem);

            var disconnectItem = new ToolStripMenuItem("&Disconnect", null, Disconnect_Click);
            trayMenu.Items.Add(disconnectItem);

            trayMenu.Items.Add(new ToolStripSeparator());

            var killSwitchItem = new ToolStripMenuItem("&Kill Switch: OFF", null, ToggleKillSwitch_Click);
            killSwitchItem.Name = "KillSwitchItem";
            trayMenu.Items.Add(killSwitchItem);

            trayMenu.Items.Add(new ToolStripSeparator());

            var logsItem = new ToolStripMenuItem("&View Logs", null, ViewLogs_Click);
            trayMenu.Items.Add(logsItem);

            var exitItem = new ToolStripMenuItem("E&xit", null, Exit_Click);
            trayMenu.Items.Add(exitItem);

            trayIcon = new NotifyIcon();
            trayIcon.Icon = CreateIcon();
            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Visible = true;
            trayIcon.MouseDoubleClick += TrayIcon_DoubleClick;
        }

        private System.Drawing.Icon CreateIcon()
        {
            // Create a simple colored icon (green for connected, red for disconnected)
            using (var bitmap = new System.Drawing.Bitmap(16, 16))
            {
                using (var g = System.Drawing.Graphics.FromImage(bitmap))
                {
                    g.Clear(System.Drawing.Color.Transparent);
                    g.FillEllipse(
                        new System.Drawing.SolidBrush(isConnected ? System.Drawing.Color.LimeGreen : System.Drawing.Color.Red),
                        2, 2, 12, 12);
                }
                return System.Drawing.Icon.FromHandle(bitmap.GetHicon());
            }
        }

        private void UpdateTrayIcon()
        {
            try
            {
                var statusItem = trayMenu.Items["StatusItem"] as ToolStripMenuItem;
                if (statusItem != null)
                {
                    string status = isConnected ? "Connected" : "Disconnected";
                    if (killSwitchEnabled)
                        status = status + " (Kill Switch ON)";
                    statusItem.Text = "Status: " + status;
                }

                var killSwitchItem = trayMenu.Items["KillSwitchItem"] as ToolStripMenuItem;
                if (killSwitchItem != null)
                    killSwitchItem.Text = "&Kill Switch: " + (killSwitchEnabled ? "ON" : "OFF");

                trayIcon.Icon = CreateIcon();
                string tooltip = "GVPN - " + (isConnected ? "Connected" : "Disconnected");
                if (killSwitchEnabled)
                    tooltip = tooltip + " (Kill Switch ON)";
                trayIcon.Text = tooltip;
            }
            catch (Exception ex)
            {
                logger.Log("Error updating tray: " + ex.Message);
            }
        }

        private void VpnMonitorLoop()
        {
            while (isRunning)
            {
                try
                {
                    bool wasConnected = isConnected;
                    isConnected = vpnManager.CheckVpnStatus();

                    // If VPN was connected but now disconnected
                    if (wasConnected && !isConnected)
                    {
                        logger.Log("VPN DISCONNECTED - wasConnected: true, isConnected: false");
                        
                        // If kill switch is enabled, block all traffic
                        if (killSwitchEnabled)
                        {
                            logger.Log("Kill switch enabled - activating kill switch (no popup)");
                            vpnManager.EnableKillSwitch();
                        }
                        // If kill switch is disabled, try to reconnect immediately
                        else
                        {
                            logger.Log("Kill switch disabled - starting failover reconnection");
                            ReconnectWithFailover();
                        }
                    }
                    // If VPN is still disconnected and kill switch is disabled, keep trying
                    else if (!isConnected && !killSwitchEnabled)
                    {
                        logger.Log("VPN still disconnected - retrying reconnection");
                        ReconnectWithFailover();
                    }

                    this.Invoke((MethodInvoker)delegate
                    {
                        UpdateTrayIcon();
                    });

                    Thread.Sleep(30000); // Check every 30 seconds
                }
                catch (Exception ex)
                {
                    logger.Log("Monitor error: " + ex.Message);
                    Thread.Sleep(5000);
                }
            }
        }

        private void ReconnectWithFailover()
        {
            try
            {
                List<VpnServer> servers = vpnManager.GetAllVpnServers();
                logger.Log("Fetched " + servers.Count + " servers for failover");
                
                if (servers.Count > 0)
                {
                    logger.Log("Starting failover - trying up to 5 servers");
                    if (vpnManager.ConnectVpnWithFailover(servers, 5))
                    {
                        isConnected = true;
                        logger.Log("Failover SUCCESS - reconnected!");
                    }
                    else
                    {
                        logger.Log("Failover FAILED - will retry on next check");
                    }
                }
                else
                {
                    logger.Log("No servers available for failover");
                }
            }
            catch (Exception ex)
            {
                logger.Log("Failover error: " + ex.Message);
            }
        }

        private void ConnectManual_Click(object sender, EventArgs e)
        {
            logger.Log("Manual connect requested - showing VPN list");
            var vpnListForm = new VpnListForm(vpnManager, logger);
            vpnListForm.ShowDialog();
            
            // Refresh status after the form closes
            this.Invoke((MethodInvoker)delegate
            {
                isConnected = vpnManager.CheckVpnStatus();
                UpdateTrayIcon();
            });
        }

        private void Disconnect_Click(object sender, EventArgs e)
        {
            logger.Log("Disconnect requested");
            vpnManager.DisconnectVpn();
            isConnected = false;
            UpdateTrayIcon();
        }

        private void ToggleKillSwitch_Click(object sender, EventArgs e)
        {
            killSwitchEnabled = !killSwitchEnabled;
            
            if (killSwitchEnabled)
            {
                logger.Log("Kill switch ENABLED - will block all traffic if VPN disconnects");
                var killSwitchItem = trayMenu.Items["KillSwitchItem"] as ToolStripMenuItem;
                if (killSwitchItem != null)
                    killSwitchItem.Text = "&Kill Switch: ON";
                    
                MessageBox.Show(
                    "Kill Switch ENABLED\n\nIf your VPN disconnects, all traffic will be automatically blocked to prevent leaks.\n\nTo disable: Right-click tray icon → Kill Switch: ON",
                    "Kill Switch Activated",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else
            {
                logger.Log("Kill switch DISABLED");
                vpnManager.DisableKillSwitch();
                var killSwitchItem = trayMenu.Items["KillSwitchItem"] as ToolStripMenuItem;
                if (killSwitchItem != null)
                    killSwitchItem.Text = "&Kill Switch: OFF";
                    
                MessageBox.Show(
                    "Kill Switch DISABLED\n\nNetwork is restored.",
                    "Kill Switch Deactivated",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }

            UpdateTrayIcon();
        }

        private void ViewLogs_Click(object sender, EventArgs e)
        {
            try
            {
                string logPath = Path.Combine(Directory.GetCurrentDirectory(), "vpn.log");
                if (File.Exists(logPath))
                {
                    Process.Start(logPath);
                }
                else
                {
                    MessageBox.Show("Log file not found.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error opening logs: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Exit_Click(object sender, EventArgs e)
        {
            isRunning = false;
            this.Close();
            Application.Exit();
        }

        private void TrayIcon_DoubleClick(object sender, MouseEventArgs e)
        {
            // Show status in tooltip
            string status = isConnected ? "Connected" : "Disconnected";
            trayIcon.ShowBalloonTip(3000, "GVpn Status", "VPN is " + status, ToolTipIcon.Info);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            isRunning = false;
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }
            base.OnFormClosing(e);
        }
    }
}
