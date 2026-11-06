using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;

namespace GVPN
{
    public class VpnListForm : Form
    {
        private DataGridView gridView;
        private Button connectButton;
        private Button autoConnectButton;
        private Button closeButton;
        private Label statusLabel;
        private VpnManager vpnManager;
        private Logger logger;
        private List<VpnServer> vpnServers;

        public VpnListForm(VpnManager vpnManager, Logger logger)
        {
            this.vpnManager = vpnManager;
            this.logger = logger;
            this.vpnServers = new List<VpnServer>();

            SetupForm();
            SetupGrid();
            SetupButtons();
            LoadVpnServers();
        }

        private void SetupForm()
        {
            this.Text = "GVPN - Available VPN Servers";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Icon = CreateIcon();
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new Size(600, 400);
        }

        private void SetupGrid()
        {
            gridView = new DataGridView();
            gridView.Dock = DockStyle.Fill;
            gridView.AllowUserToAddRows = false;
            gridView.AllowUserToDeleteRows = false;
            gridView.ReadOnly = true;
            gridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridView.RowHeadersVisible = false;
            gridView.BackgroundColor = SystemColors.Window;
            gridView.AlternatingRowsDefaultCellStyle.BackColor = Color.WhiteSmoke;

            gridView.Columns.Add("IP", "VPN Server IP");
            gridView.Columns.Add("Country", "Country");
            gridView.Columns.Add("Ping", "Ping (ms)");
            gridView.Columns.Add("Speed", "Speed (bps)");
            gridView.Columns.Add("Sessions", "Sessions");

            this.Controls.Add(gridView);
        }

        private void SetupButtons()
        {
            Panel footerPanel = new Panel();
            footerPanel.Dock = DockStyle.Bottom;
            footerPanel.AutoSize = true;
            footerPanel.Padding = new Padding(10);

            statusLabel = new Label();
            statusLabel.Text = "Loading servers...";
            statusLabel.AutoSize = true;
            statusLabel.Dock = DockStyle.Top;
            footerPanel.Controls.Add(statusLabel);

            FlowLayoutPanel buttonPanel = new FlowLayoutPanel();
            buttonPanel.Dock = DockStyle.Top;
            buttonPanel.AutoSize = true;
            buttonPanel.WrapContents = false;
            buttonPanel.Padding = new Padding(5);

            autoConnectButton = new Button();
            autoConnectButton.Text = "Auto-Connect (Closest)";
            autoConnectButton.Width = 150;
            autoConnectButton.Height = 30;
            autoConnectButton.Margin = new Padding(5);
            autoConnectButton.Click += AutoConnect_Click;
            buttonPanel.Controls.Add(autoConnectButton);

            connectButton = new Button();
            connectButton.Text = "Connect Selected";
            connectButton.Width = 120;
            connectButton.Height = 30;
            connectButton.Margin = new Padding(5);
            connectButton.Click += Connect_Click;
            buttonPanel.Controls.Add(connectButton);

            closeButton = new Button();
            closeButton.Text = "Close";
            closeButton.Width = 100;
            closeButton.Height = 30;
            closeButton.Margin = new Padding(5);
            closeButton.Click += (s, e) => this.Close();
            buttonPanel.Controls.Add(closeButton);

            footerPanel.Controls.Add(buttonPanel);
            this.Controls.Add(footerPanel);
        }

        private void LoadVpnServers()
        {
            statusLabel.Text = "Fetching VPN servers...";
            statusLabel.Refresh();

            System.Threading.Thread thread = new System.Threading.Thread(() =>
            {
                try
                {
                    var result = vpnManager.GetAllVpnServers();
                    vpnServers = result;

                    this.Invoke((MethodInvoker)delegate
                    {
                        PopulateGrid();
                        statusLabel.Text = "Loaded " + vpnServers.Count + " servers. Select one and click Connect.";
                    });
                }
                catch (Exception ex)
                {
                    logger.Log("Error loading VPN servers: " + ex.Message);
                    this.Invoke((MethodInvoker)delegate
                    {
                        statusLabel.Text = "Error loading servers: " + ex.Message;
                    });
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }

        private void PopulateGrid()
        {
            gridView.Rows.Clear();

            foreach (VpnServer server in vpnServers)
            {
                gridView.Rows.Add(
                    server.Host,
                    server.Country,
                    server.Ping,
                    FormatSpeed(server.Speed),
                    server.Sessions
                );
            }
        }

        private string FormatSpeed(long speed)
        {
            if (speed >= 1000000000)
                return (speed / 1000000000) + " Gbps";
            if (speed >= 1000000)
                return (speed / 1000000) + " Mbps";
            if (speed >= 1000)
                return (speed / 1000) + " Kbps";
            return speed + " bps";
        }

        private void AutoConnect_Click(object sender, EventArgs e)
        {
            if (vpnServers.Count == 0)
            {
                MessageBox.Show("No servers available.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            VpnServer closest = null;
            int lowestPing = int.MaxValue;
            foreach (VpnServer server in vpnServers)
            {
                if (server.Ping < lowestPing)
                {
                    lowestPing = server.Ping;
                    closest = server;
                }
            }

            if (closest != null)
            {
                statusLabel.Text = "Connecting to " + closest.Host + " (" + closest.Country + ")...";
                statusLabel.Refresh();

                if (vpnManager.ConnectVpn(closest.Host))
                {
                    MessageBox.Show("Connected to " + closest.Host + " (" + closest.Country + ")", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.Close();
                }
                else
                {
                    statusLabel.Text = "Connection failed. See logs for details.";
                    MessageBox.Show("Failed to connect. Check vpn.log for details.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void Connect_Click(object sender, EventArgs e)
        {
            if (gridView.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a VPN server.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int selectedIndex = gridView.SelectedRows[0].Index;
            if (selectedIndex >= 0 && selectedIndex < vpnServers.Count)
            {
                VpnServer selected = vpnServers[selectedIndex];
                statusLabel.Text = "Connecting to " + selected.Host + " (" + selected.Country + ")...";
                statusLabel.Refresh();

                if (vpnManager.ConnectVpn(selected.Host))
                {
                    MessageBox.Show("Connected to " + selected.Host + " (" + selected.Country + ")", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.Close();
                }
                else
                {
                    statusLabel.Text = "Connection failed. See logs for details.";
                    MessageBox.Show("Failed to connect. Check vpn.log for details.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private System.Drawing.Icon CreateIcon()
        {
            using (var bitmap = new Bitmap(16, 16))
            {
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.Clear(Color.Transparent);
                    g.FillEllipse(new SolidBrush(Color.LimeGreen), 2, 2, 12, 12);
                }
                return Icon.FromHandle(bitmap.GetHicon());
            }
        }
    }
}