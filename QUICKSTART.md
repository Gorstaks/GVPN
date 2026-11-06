# GVpnCSharp - Quick Start Guide

## What You Have

A lightweight Windows VPN manager based on your Python GVpn script, now written in C# 5.0 for .NET 4.8. It runs as a system tray icon and automatically maintains VPN connectivity.

## Files

```
GVpnCSharp/
├── Program.cs                  # Entry point
├── VpnTrayApplication.cs       # UI and monitoring loop
├── VpnManager.cs              # VPN operations (connect/disconnect/status)
├── Logger.cs                  # Thread-safe logging
├── GVpnCSharp.csproj          # Project file for MSBuild
├── build.ps1                  # PowerShell build script
├── README.md                  # Full documentation
└── bin/Release/
    └── GVpn.exe               # Compiled executable
```

## Build & Run

### Build (if you modify code):
```powershell
cd E:\Gorstak\GVpnCSharp
.\build.ps1
```

### Run:
```powershell
.\bin\Release\GVpn.exe
```

Or double-click `GVpn.exe` in File Explorer.

## First Time Setup

Before running, create a VPN connection in Windows:

1. **Settings** → **Network & Internet** → **VPN** → **Add a VPN connection**
2. Fill in your VPN details (provider, server, username, password)
3. **Name it "MyVPN"** (the app looks for this exact name)
4. Save

Alternatively, if you already have a different VPN connection, edit `VpnManager.cs` line 13:
```csharp
private const string VpnName = "MyVPN";  // Change to your VPN name
```
Then rebuild with `.\build.ps1`

## Features (Click Tray Icon → Right-Click Menu)

| Option | Does What |
|--------|-----------|
| **Status** | Shows Connected/Disconnected |
| **Connect Now** | Manually fetch closest VPN and connect |
| **Disconnect** | Close VPN connection |
| **Kill Switch** | Disconnect and exit app (use carefully!) |
| **View Logs** | Open `vpn.log` for debugging |
| **Exit** | Close app gracefully |

## How It Works

1. **Starts** → Hides in system tray (green icon if connected, red if not)
2. **Every 30 seconds** → Checks if VPN is connected
3. **If disconnected** → Fetches list of public VPN servers from VPNGate.net
4. **Picks closest server** (lowest ping) and connects automatically
5. **Logs everything** to `vpn.log` in the working directory

## Customization

### Edit VPN credentials
File: `VpnManager.cs`, lines 12-14
```csharp
private const string VpnName = "MyVPN";      // Your VPN connection name
private const string VpnUser = "vpn";        // Your username
private const string VpnPass = "vpn";        // Your password
```

### Change monitor interval
File: `VpnTrayApplication.cs`, line 127
```csharp
Thread.Sleep(30000);  // Change 30000 to milliseconds you want (e.g., 60000 = 1 min)
```

Then rebuild: `.\build.ps1`

## Troubleshooting

| Problem | Solution |
|---------|----------|
| **"csc.exe not found"** | Install Visual Studio or Windows SDK |
| **VPN won't connect** | Ensure "MyVPN" exists in Settings → Network → VPN |
| **App won't start** | Right-click GVpn.exe → Properties → Compatibility → Run as Administrator |
| **Can't see tray icon** | Icon may be hidden; check hidden icons in taskbar |
| **No logs** | Check `vpn.log` exists in app's working directory |

## Technical Details

- **Language**: C# 5.0
- **Framework**: .NET 4.8
- **Compiler**: csc.exe (includes with .NET Framework)
- **UI**: Windows Forms
- **VPN Backend**: Uses Windows RAS (Remote Access Service) via `rasdial.exe`
- **Server Source**: VPNGate.net API (free public VPN list)

## Exe Size

**~20 KB** - Just a tiny executable, no runtime files needed (uses built-in .NET 4.8)

## Notes

- The app runs in the background with NO window
- Double-click tray icon to see status popup
- Kill Switch disconnects VPN and closes app (can't be undone)
- Logs are appended, so `vpn.log` grows over time - delete it occasionally
- Works Windows 7+

## Support

For issues, check the full README.md or review `vpn.log` for error messages.
