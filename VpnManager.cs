using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Collections.Generic;

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

                var psi = new ProcessStartInfo
                {
                    FileName = "rasdial",
                    Arguments = "\"" + VpnName + "\" " + host + " " + VpnUser + " " + VpnPass,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    if (!process.WaitForExit(10000))
                    {
                        process.Kill();
                        logger.Log("VPN connection timed out");
                        return false;
                    }

                    if (process.ExitCode != 0)
                    {
                        string error = process.StandardError.ReadToEnd();
                        logger.Log("VPN connection failed: " + error);
                        return false;
                    }

                    logger.Log("VPN connected to " + host);
                    return true;
                }
            }
            catch (Exception ex)
            {
                logger.Log("Connection error: " + ex.Message);
                return false;
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
                    logger.Log("Successfully connected on attempt " + attempts);
                    return true;
                }

                logger.Log("Failed to connect to " + server.Host + ", trying next server...");
                System.Threading.Thread.Sleep(2000);
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
                    logger.Log("VPN status: " + (isConnected ? "Connected" : "Disconnected"));
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
    }
}
