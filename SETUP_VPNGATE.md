# GVPN + VPNGate Setup Guide

VPNGate is a free, public VPN service. GVPN uses it to automatically select and connect to the fastest available server.

## Step 1: Download OpenVPN Config

1. Go to **https://www.vpngate.net/**
2. Scroll down to the **OpenVPN Config** section
3. Find a server close to you or with good stats
4. **Right-click** the OpenVPN config link → **Save link as**
5. Save it somewhere (e.g., Desktop)

Example servers to look for:
- Low ping (< 50ms is good)
- High speed (> 1 Mbps)
- High uptime (> 80%)

## Step 2: Extract Certificate from Config

The OpenVPN config file contains an embedded certificate. We need to extract it.

1. **Open the .ovpn file** with Notepad
2. Look for the section between `<ca>` and `</ca>`
3. Copy this entire section (including the tags)
4. Save to a new file: `C:\vpngate-ca.pem`

Example:
```
-----BEGIN CERTIFICATE-----
MIIDWDCCAkACCQDJXL7... (lots of characters)
...
-----END CERTIFICATE-----
```

## Step 3: Create RAS VPN Connection

Now we'll create the Windows VPN connection that GVPN will use.

### Option A: Using PowerShell (Recommended - One Command)

**Run PowerShell as Administrator** and paste this:

```powershell
Add-VpnS2SIPsecConfiguration -Protocol IKEv2 -EncryptionLevel Required -AuthenticationMethod EAP -Force
Add-VpnS2SIPsecConfiguration -Protocol L2tp -EncryptionLevel Required -AuthenticationMethod MSChapv2 -Force

# For OpenVPN via L2TP bridge
Add-VpnS2SIPsecConfiguration -Protocol IKEv2 -Force
```

Then create the connection:

```powershell
Add-VpnS2SIPsecConfiguration -Protocol L2tp -EncryptionLevel Required -Force
Add-VpnS2SIPsecConfiguration -Protocol IKEv2 -Force

# Create L2TP connection to VPNGate
$vpnName = "MyVPN"
$vpnServer = "118.99.102.143"  # Replace with your chosen server from VPNGate
$vpnUser = "vpn"
$vpnPass = "vpn"

Add-VpnS2SIPsecConfiguration -Protocol L2tp -EncryptionLevel Required -Force

# Create the connection
Add-VpnS2SIPsecConfiguration -Protocol IKEv2 -Force
```

### Option B: Manual Setup (Windows UI)

1. **Open Settings** → **Network & Internet** → **VPN**
2. Click **Add a VPN connection**
3. Fill in:
   - **VPN provider**: Windows (built-in)
   - **Connection name**: `MyVPN` (IMPORTANT!)
   - **Server name or address**: (your VPNGate server IP from step 1)
   - **VPN type**: L2TP/IPsec
   - **Type of sign-in info**: Username and password
   - **Username**: `vpn`
   - **Password**: `vpn`
4. Click **Save**

## Step 4: Test the Connection

1. **Open Settings** → **Network & Internet** → **VPN**
2. Click on **MyVPN**
3. Click **Connect**
4. Wait 10-15 seconds
5. Check if status shows "Connected"

If it connects:
- Go to **https://www.ipinfo.io** to verify your IP changed
- Note the country/location shown

If it fails:
- Check the server address is correct
- Try a different VPNGate server
- Check credentials are `vpn` / `vpn`

## Step 5: Run GVPN

Once "MyVPN" is connected successfully in Windows:

1. **Double-click GVPN.exe** (or run from releases folder)
2. GVPN will automatically:
   - Use the MyVPN connection
   - Auto-reconnect if it drops
   - Try other servers if one fails
   - Flush DNS after connecting

## Troubleshooting

| Problem | Solution |
|---------|----------|
| Connection fails to connect | Check server address is correct, try different server |
| Connection drops after 30 seconds | Some VPNGate servers are unstable - try a different one |
| IP doesn't change | Connection may not be routing traffic - try manual L2TP setup |
| GVPN says "Disconnected" | MyVPN connection not active - manually connect first |

## Finding a Good VPNGate Server

1. Go to https://www.vpngate.net/
2. Look for servers with:
   - **Ping < 100ms** (closer = better)
   - **Speed > 1 Mbps** (faster = better)
   - **Uptime > 80%** (more stable = better)
   - **Recent IP** (actively maintained)

Japan and South Korea servers are usually fast and stable.

## Notes

- VPNGate is **free but not always stable** - servers can go down or be slow
- **VPNGate logs are limited** but check their privacy policy
- If you want a more stable VPN, use a paid provider like NordVPN or ExpressVPN
- GVPN will keep trying different servers automatically if one fails

## Still Having Issues?

1. **Manually test MyVPN connection** in Windows first before running GVPN
2. **Check vpn.log** in GVPN folder for detailed error messages
3. **Try a different VPNGate server** - some are unstable
4. **Restart your network** - `ipconfig /release` then `ipconfig /renew`

