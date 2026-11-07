using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.IO;

namespace GVPN
{
    public class VpnServer
    {
        public string Host { get; set; }
        public string Country { get; set; }
        public int Ping { get; set; }
        public long Speed { get; set; }
        public int Sessions { get; set; }
    }

    public class VpnManager
    {
        private Logger logger;
        private const string VpnName = "MyVPN";
        private const string VpnUser = "vpn";
        private const string VpnPass = "vpn";

        public VpnManager(Logger logger)
        {
            this.logger = logger;
        }

        public void EnsureVpnConnectionExists()
        {
            try
            {
                logger.Log("Checking if MyVPN connection exists...");
                
                // Check if connection exists
                if (VpnConnectionExists())
                {
                    logger.Log("MyVPN connection is ready");
                    return;
                }

                logger.Log("MyVPN not found - creating it now...");
                CreateVpnConnection();
                
                // Verify it was created
                if (VpnConnectionExists())
                {
                    logger.Log("MyVPN connection created successfully");
                }
                else
                {
                    logger.Log("WARNING: MyVPN creation may have failed - attempting to continue");
                }
            }
            catch (Exception ex)
            {
                logger.Log("Error in VPN setup: " + ex.Message);
            }
        }

        private bool VpnConnectionExists()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"Get-VpnConnection -Name 'MyVPN' -ErrorAction SilentlyContinue | Where-Object {$_.Name -eq 'MyVPN'} | Select-Object -ExpandProperty Name\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    if (!process.WaitForExit(5000))
                    {
                        process.Kill();
                        return false;
                    }
                    
                    string output = process.StandardOutput.ReadToEnd().Trim();
                    return output.Contains("MyVPN");
                }
            }
            catch
            {
                return false;
            }
        }

        private void CreateVpnConnection()
        {
            try
            {
                // Build the PowerShell command to create VPN connection
                string psCommand = 
                    "$ErrorActionPreference = 'Stop'; " +
                    "Remove-VpnConnection -Name 'MyVPN' -Force -ErrorAction SilentlyContinue | Out-Null; " +
                    "Start-Sleep -Milliseconds 500; " +
                    "Add-VpnConnection -Name 'MyVPN' " +
                    "  -ServerAddress '0.0.0.0' " +
                    "  -TunnelType L2tp " +
                    "  -EncryptionLevel Required " +
                    "  -AuthenticationMethod MSChapv2 " +
                    "  -L2tpPsk 'vpn' " +
                    "  -Force -WarningAction SilentlyContinue | Out-Null; " +
                    "Write-Host 'Created'; " +
                    "Get-VpnConnection -Name 'MyVPN' | Select-Object -ExpandProperty Name";

                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"" + psCommand + "\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    if (!process.WaitForExit(15000))
                    {
                        process.Kill();
                        logger.Log("VPN creation timed out");
                        return;
                    }

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    if (!string.IsNullOrEmpty(output))
                    {
                        logger.Log("VPN creation output: " + output.Trim());
                    }

                    if (!string.IsNullOrEmpty(error) && !error.Contains("deprecated"))
                    {
                        logger.Log("VPN creation notice: " + error.Trim());
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Log("VPN creation error: " + ex.Message);
            }
        }


        private void ExecutePowerShell(string command, int timeoutMs)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"" + command + "\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    if (!process.WaitForExit(timeoutMs))
                    {
                        process.Kill();
                        logger.Log("PowerShell command timed out");
                        return;
                    }

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    if (!string.IsNullOrEmpty(output))
                    {
                        logger.Log("PowerShell output: " + output.Trim());
                    }

                    if (!string.IsNullOrEmpty(error))
                    {
                        logger.Log("PowerShell error: " + error.Trim());
                    }

                    if (process.ExitCode != 0)
                    {
                        logger.Log("PowerShell exit code: " + process.ExitCode);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Log("PowerShell execution error: " + ex.Message);
            }
        }

        public Tuple<string, string> GetPublicVpn()
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    string response = client.DownloadString("https://www.vpngate.net/api/iphone/");

                    List<VpnServer> servers = ParseVpnGateResponse(response);
                    if (servers.Count > 0)
                    {
                        VpnServer best = null;
                        int lowestPing = int.MaxValue;
                        foreach (VpnServer server in servers)
                        {
                            if (server.Ping < lowestPing)
                            {
                                lowestPing = server.Ping;
                                best = server;
                            }
                        }
                        logger.Log("Selected VPN: " + best.Host + " (" + best.Country + ", ping: " + best.Ping + "ms)");
                        return new Tuple<string, string>(best.Host, best.Country);
                    }

                    logger.Log("No valid VPN servers available");
                    return new Tuple<string, string>(null, null);
                }
            }
            catch (Exception ex)
            {
                logger.Log("VPN fetch failed: " + ex.Message);
                return new Tuple<string, string>(null, null);
            }
        }

        public List<VpnServer> GetAllVpnServers()
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    string response = client.DownloadString("https://www.vpngate.net/api/iphone/");
                    List<VpnServer> servers = ParseVpnGateResponse(response);
                    logger.Log("Fetched " + servers.Count + " VPN servers");
                    return servers;
                }
            }
            catch (Exception ex)
            {
                logger.Log("VPN fetch failed: " + ex.Message);
                return new List<VpnServer>();
            }
        }

        public bool ConnectVpn(string host)
        {
            if (string.IsNullOrEmpty(host))
            {
                logger.Log("No VPN host provided");
                return false;
            }

            try
            {
                logger.Log("Connecting to VPN: " + host);

                // Update the MyVPN connection with the new server address
                string updateCmd = 
                    "$ErrorActionPreference = 'SilentlyContinue'; " +
                    "Set-VpnConnection -Name 'MyVPN' -ServerAddress '" + host + "' -Force -ErrorAction SilentlyContinue | Out-Null; " +
                    "Write-Host 'Updated'";

                ExecutePowerShell(updateCmd, 5000);

                // Connect using rasdial with the connection name
                var psi = new ProcessStartInfo
                {
                    FileName = "rasdial",
                    Arguments = "MyVPN " + VpnUser + " " + VpnPass,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    // Increase timeout to 10s to allow connection to establish
                    if (!process.WaitForExit(10000))
                    {
                        process.Kill();
                        logger.Log("Connection attempt timed out after 10s");
                        return false;
                    }

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    if (process.ExitCode != 0)
                    {
                        logger.Log("Connection failed (exit " + process.ExitCode + "): " + error.Substring(0, Math.Min(error.Length, 100)));
                        return false;
                    }

                    // Check if connection was successful
                    if (output.Contains("Connected") || process.ExitCode == 0)
                    {
                        logger.Log("Connected to " + host);
                        
                        // Flush DNS cache on successful connection
                        logger.Log("Flushing DNS cache...");
                        FlushDnsCache();
                        
                        return true;
                    }
                    else
                    {
                        logger.Log("Connection status unclear: " + output.Substring(0, Math.Min(output.Length, 100)));
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Log("Connection error: " + ex.Message);
                return false;
            }
        }

        private void FlushDnsCache()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c ipconfig /flushdns",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    process.WaitForExit(3000);
                    string output = process.StandardOutput.ReadToEnd();
                    
                    if (process.ExitCode == 0)
                    {
                        logger.Log("DNS cache flushed successfully");
                    }
                    else
                    {
                        logger.Log("DNS flush may have failed (exit " + process.ExitCode + ")");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Log("DNS flush error: " + ex.Message);
            }
        }

        public bool ConnectVpnWithFailover(List<VpnServer> servers, int maxAttempts)
        {
            if (servers == null || servers.Count == 0)
            {
                logger.Log("No VPN servers available for failover");
                return false;
            }

            // Sort by ping (best first)
            List<VpnServer> sortedServers = new List<VpnServer>(servers);
            for (int i = 0; i < sortedServers.Count - 1; i++)
            {
                for (int j = i + 1; j < sortedServers.Count; j++)
                {
                    if (sortedServers[j].Ping < sortedServers[i].Ping)
                    {
                        VpnServer temp = sortedServers[i];
                        sortedServers[i] = sortedServers[j];
                        sortedServers[j] = temp;
                    }
                }
            }

            int attempts = 0;
            int maxServerAttempts = System.Math.Min(maxAttempts, sortedServers.Count);

            for (int i = 0; i < maxServerAttempts && attempts < maxAttempts; i++)
            {
                VpnServer server = sortedServers[i];
                attempts++;
                
                logger.Log("Failover attempt " + attempts + "/" + maxAttempts + ": Trying " + server.Host + " (" + server.Country + ", ping: " + server.Ping + "ms)");

                if (ConnectVpn(server.Host))
                {
                    // Give connection time to stabilize before returning
                    logger.Log("Connected - waiting 3 seconds for stabilization...");
                    System.Threading.Thread.Sleep(3000);
                    
                    // Verify connection is still active
                    if (CheckVpnStatus())
                    {
                        logger.Log("Connection verified - still active after 3 seconds");
                        logger.Log("Successfully connected on attempt " + attempts);
                        return true;
                    }
                    else
                    {
                        logger.Log("Connection dropped during stabilization - trying next server");
                    }
                }

                logger.Log("Failed to connect to " + server.Host + ", trying next server...");
                System.Threading.Thread.Sleep(1000);
            }

            logger.Log("All " + maxServerAttempts + " failover attempts failed");
            return false;
        }

        public bool DisconnectVpn()
        {
            try
            {
                logger.Log("Disconnecting VPN");

                var psi = new ProcessStartInfo
                {
                    FileName = "rasdial",
                    Arguments = "\"" + VpnName + "\" disconnect",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    process.WaitForExit(5000);

                    if (process.ExitCode == 0)
                    {
                        logger.Log("VPN disconnected successfully");
                        return true;
                    }
                    else
                    {
                        string error = process.StandardError.ReadToEnd();
                        logger.Log("Disconnect failed: " + error);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Log("Disconnect error: " + ex.Message);
                return false;
            }
        }

        public bool CheckVpnStatus()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "rasdial",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    bool isConnected = output.Contains("Connected");
                    
                    if (isConnected)
                    {
                        logger.Log("VPN status: Connected (verified via rasdial)");
                    }
                    else
                    {
                        logger.Log("VPN status: Disconnected (rasdial shows no active connections)");
                    }
                    
                    return isConnected;
                }
            }
            catch (Exception ex)
            {
                logger.Log("Status check error: " + ex.Message);
                return false;
            }
        }

        private List<VpnServer> ParseVpnGateResponse(string response)
        {
            var servers = new List<VpnServer>();

            try
            {
                string[] lines = response.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                        continue;

                    string[] parts = line.Split(',');
                    
                    // Format: HostName,IP,Score,Ping,Speed,CountryLong,CountryShort,NumVpnSessions,...
                    // We need: IP (1), Ping (3), Speed (4), CountryShort (6), Sessions (7)
                    if (parts.Length > 7)
                    {
                        int ping;
                        long speed;
                        int sessions;
                        string ip = parts[1].Trim();
                        string country = parts[6].Trim();
                        
                        if (int.TryParse(parts[3], out ping) && 
                            long.TryParse(parts[4], out speed) && 
                            int.TryParse(parts[7], out sessions) &&
                            !string.IsNullOrEmpty(ip) && 
                            !string.IsNullOrEmpty(country))
                        {
                            servers.Add(new VpnServer
                            {
                                Host = ip,
                                Country = country,
                                Ping = ping,
                                Speed = speed,
                                Sessions = sessions
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Log("Parse error: " + ex.Message);
            }

            return servers;
        }

        public bool EnableKillSwitch()
        {
            try
            {
                logger.Log("Enabling kill switch - blocking all traffic");
                
                // Disconnect VPN first
                DisconnectVpn();
                System.Threading.Thread.Sleep(1000);

                // Block all outbound traffic
                var commands = new[]
                {
                    "netsh advfirewall set allprofiles state on",
                    "netsh advfirewall firewall add rule name=\"GVPN Kill Switch\" dir=out action=block enable=yes",
                    "netsh advfirewall firewall add rule name=\"GVPN Kill Switch - Loopback\" dir=out action=allow enable=yes remoteip=127.0.0.1",
                    "netsh advfirewall firewall add rule name=\"GVPN Kill Switch - DNS\" dir=out action=allow enable=yes protocol=udp remoteport=53"
                };

                foreach (string cmd in commands)
                {
                    ExecuteCommand(cmd);
                }

                logger.Log("Kill switch enabled - all traffic blocked except loopback and DNS");
                return true;
            }
            catch (Exception ex)
            {
                logger.Log("Kill switch error: " + ex.Message);
                return false;
            }
        }

        public bool DisableKillSwitch()
        {
            try
            {
                logger.Log("Disabling kill switch");

                var commands = new[]
                {
                    "netsh advfirewall firewall delete rule name=\"GVPN Kill Switch\"",
                    "netsh advfirewall firewall delete rule name=\"GVPN Kill Switch - Loopback\"",
                    "netsh advfirewall firewall delete rule name=\"GVPN Kill Switch - DNS\""
                };

                foreach (string cmd in commands)
                {
                    ExecuteCommand(cmd);
                }

                logger.Log("Kill switch disabled");
                return true;
            }
            catch (Exception ex)
            {
                logger.Log("Kill switch disable error: " + ex.Message);
                return false;
            }
        }

        private bool ExecuteCommand(string command)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c " + command,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    process.WaitForExit(5000);
                    if (process.ExitCode != 0)
                    {
                        string error = process.StandardError.ReadToEnd();
                        logger.Log("Command failed: " + command + " - " + error);
                        return false;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                logger.Log("Execute command error: " + ex.Message);
                return false;
            }
        }

        public void LogDiagnostics()
        {
            try
            {
                logger.Log("=== VPN DIAGNOSTICS ===");
                
                // Check rasdial status
                var psi = new ProcessStartInfo
                {
                    FileName = "rasdial",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string[] lines = output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                    foreach (string line in lines)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            logger.Log("RASDIAL: " + line);
                        }
                    }
                }

                logger.Log("=== END DIAGNOSTICS ===");
            }
            catch (Exception ex)
            {
                logger.Log("Diagnostics error: " + ex.Message);
            }
        }
    }
}
