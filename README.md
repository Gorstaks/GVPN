# GVpnCSharp - VPN Manager with Tray Icon

A minimal C# 4.8 / .NET Framework Windows Forms application that provides VPN management with a system tray icon and kill switch functionality.

## Features

- **System Tray Icon** - Minimalist UI that lives in the taskbar
- **Auto-reconnect** - Automatically fetches the closest VPN server and maintains connection
- **Kill Switch** - Blocks ALL traffic if VPN disconnects (firewall-based, requires admin)
- **Status Monitoring** - Real-time VPN status indicator (green = connected, red = disconnected)
- **Manual Connect/Disconnect** - One-click VPN operations
- **Logging** - Detailed logs saved to `vpn.log`
- **Lightweight** - Compiles to a single tiny executable with no external dependencies (uses System assemblies)

## Requirements

- Windows 7 or later
- .NET Framework 4.8
- Visual Studio or Windows SDK (for csc.exe compiler)
- VPN connection named "MyVPN" configured in Windows RAS (Remote Access Service)

## Setup

### 1. Create VPN Connection

Before running the application, you need to set up a VPN connection in Windows:

```powershell
# Open Settings > Network & Internet > VPN > Add a VPN connection
# Or use this command to create a RAS connection (modify as needed)
```

Or manually create via Settings:
1. Open Settings
2. Go to Network & Internet > VPN
3. Click "Add a VPN connection"
4. Fill in your VPN details
5. Name it "MyVPN" (the app looks for this name)

### 2. Build the Project

```powershell
# Navigate to the project directory
cd E:\Gorstak\GVpnCSharp

# Run the build script
.\build.ps1
```

Or compile manually with csc:

```powershell
csc /target:winexe /out:bin\Release\GVpn.exe `
  /reference:System.dll /reference:System.Windows.Forms.dll /reference:System.Drawing.dll `
  Program.cs VpnTrayApplication.cs VpnManager.cs Logger.cs
```

### 3. Run the Application

```powershell
.\bin\Release\GVpn.exe
```

The application will:
- Hide in the system tray
- Automatically attempt to fetch the closest VPN server
- Monitor connection every 30 seconds
- Auto-reconnect if disconnected

## Usage

### Tray Icon Menu

Right-click the tray icon to access:

- **Status** - Shows current connection status
- **Connect Now** - Manually connect to the closest VPN
- **Disconnect** - Disconnect from VPN
- **Kill Switch** - Blocks ALL outbound traffic using Windows Firewall (prevents data leaks if VPN drops)
- **View Logs** - Open `vpn.log` in default editor
- **Exit** - Close application gracefully

### Double-Click Tray Icon

Shows a popup with current connection status.

## Configuration

Edit these constants in `VpnManager.cs` if needed:

```csharp
private const string VpnName = "MyVPN";      // Name of VPN connection
private const string VpnUser = "vpn";        // VPN username
private const string VpnPass = "vpn";        // VPN password
```

## VPN Servers

The application fetches public VPN servers from:
- **Source**: https://www.vpngate.net/api/iphone/
- **Selection**: Automatically selects the server with the lowest ping (closest to you)

## Logs

Application logs are saved to `vpn.log` in the working directory with entries like:

```
2026-11-06 14:23:45 - Application started
2026-11-06 14:23:46 - Selected VPN: 1.2.3.4 (US, ping: 45ms)
2026-11-06 14:23:50 - VPN connected to 1.2.3.4
```

## Architecture

- **Program.cs** - Entry point, creates main form
- **VpnTrayApplication.cs** - Main form with tray icon UI and monitoring loop
- **VpnManager.cs** - Handles all VPN operations (connect, disconnect, status, fetch servers)
- **Logger.cs** - Thread-safe logging utility

## Compilation Details

- **Language**: C# 5.0
- **Framework**: .NET Framework 4.8
- **Output Type**: Windows Executable (WinExe)
- **References**: System, System.Core, System.Windows.Forms, System.Drawing
- **Compiler**: csc.exe (C# Compiler from Visual Studio)

## Known Limitations

- Requires a pre-configured RAS VPN connection named "MyVPN"
- Windows only (uses `rasdial.exe` for VPN operations)
- Kill switch just disconnects and exits; doesn't block all traffic permanently
- VPN credentials hardcoded in source (for trusted environments only)

## Legal Disclaimer

**IMPORTANT - READ BEFORE USE**

GVPN is provided as-is for educational and personal use only. By using this software, you agree to the following:

### Usage Restrictions
- Use GVPN **only on systems you own or have explicit permission to use**
- Do **NOT** use GVPN to circumvent security measures, firewalls, or access controls that you are not authorized to bypass
- Do **NOT** use GVPN for any illegal activities, including but not limited to:
  - Unauthorized access to computer systems
  - Copyright infringement or piracy
  - Fraud, hacking, or cybercrime
  - Evading law enforcement
  - Harassment or attacks on others
  - Any activity that violates local, state, or international laws

### No Warranty
- This software is provided **without any warranty, express or implied**
- The author is **not responsible** for:
  - Data loss or corruption
  - Network issues or connection problems
  - VPN disconnections or reconnection failures
  - Any damage caused by use or misuse of this software
  - Any liability arising from your use of this software

### Liability
- **You are solely responsible** for your use of GVPN
- The author **assumes no liability** for:
  - Any loss of privacy or exposure of data
  - Regulatory violations or legal consequences
  - System damage or downtime
  - Third-party claims

### VPN & Privacy
- Using a VPN does **not guarantee anonymity or privacy**
- VPN providers may log your activity (check their privacy policy)
- Your ISP may still see that you're using a VPN
- Kill switch can fail; never rely on it as your only protection
- **No tool can provide 100% privacy** - use responsibly and legally

### Age Restriction
- You must be **at least 18 years old** (or the legal age of majority in your jurisdiction) to use this software

### Compliance
- **You are responsible for complying** with:
  - All local, state, and national laws
  - Your ISP's terms of service
  - Your employer's or organization's policies
  - The laws of countries whose services you access
  - VPN provider's terms of service

### No Support for Illegal Activity
- The author **does not condone, endorse, or support** any illegal use
- Misuse of this software may result in:
  - Criminal prosecution
  - Civil liability
  - Permanent bans from services
  - Legal consequences

**By downloading and using GVPN, you acknowledge that you have read, understood, and agree to these terms.**

If you do not agree with any of these terms, **do not use this software**.

## Troubleshooting

### "csc.exe not found"
Install Visual Studio or Windows SDK. The build script searches common installation paths.

### VPN connection fails
- Ensure "MyVPN" is created in Windows RAS settings
- Check credentials in `VpnManager.cs`
- Review `vpn.log` for detailed error messages

### Application won't start
- Right-click properties on GVpn.exe → Compatibility → Run as Administrator
- Check Windows Event Viewer for application errors

### VPN server fetch fails
- Verify internet connection
- Check firewall rules allow access to vpngate.net
- VPN Gate may be blocked in your region; use a different VPN first

## License

Public Domain - Use freely
