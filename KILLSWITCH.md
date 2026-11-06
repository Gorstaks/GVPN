# GVPN Kill Switch Guide

## What It Does

The **Kill Switch** in GVPN is a **passive safety net** that:

1. **Monitors** your VPN connection continuously
2. **Auto-blocks ALL traffic** if VPN disconnects (using Windows Firewall rules)
3. **Prevents data leaks** completely

Kill Switch is:
- ✅ **Always monitoring** - doesn't interrupt your connection
- ✅ **Automatic** - activates instantly if VPN drops
- ✅ **Non-destructive** - doesn't kill your VPN or disconnect you
- ❌ **NOT manual** - don't click it to disconnect (it's a safety feature, not a tool)

Once enabled and VPN drops, your computer cannot communicate with any external network except:
- Loopback (127.0.0.1) - local communication only
- DNS (UDP port 53) - so you can resolve domain names (but not connect to them)

## Usage

### Enable Kill Switch

1. Right-click **GVPN** tray icon
2. Click **"Kill Switch: OFF"** to toggle to **"Kill Switch: ON"**
3. Confirm the activation message

Now the kill switch is armed. Your VPN works normally, but:
- **If VPN disconnects**, all traffic is instantly blocked
- **If VPN stays connected**, everything works normally

### Disable Kill Switch

1. Right-click **GVPN** tray icon
2. Click **"Kill Switch: ON"** to toggle to **"Kill Switch: OFF"**
3. Confirm the deactivation message

Network is restored to normal.

### What Happens if VPN Drops (Kill Switch Enabled)

1. Kill Switch **detects VPN disconnection**
2. **Alert popup** shows: "VPN DISCONNECTED! Kill Switch activated - all traffic blocked."
3. **All outbound traffic blocked** instantly (firewall rules applied)
4. Your computer is **completely isolated** from the internet

**To restore**: Disable kill switch (right-click tray → Kill Switch: ON → disable), or restart computer.

## How It Works

### Kill Switch Firewall Rules

When VPN disconnects **and kill switch is enabled**, GVPN creates these Windows Firewall rules:

| Rule Name | Direction | Action | Effect |
|-----------|-----------|--------|--------|
| GVPN Kill Switch | Outbound | **BLOCK** | Blocks ALL outbound traffic |
| GVPN Kill Switch - Loopback | Outbound | Allow | Allows local connections (127.0.0.1) |
| GVPN Kill Switch - DNS | Outbound | Allow | Allows DNS queries (UDP:53) |

These rules are enforced on **ALL network profiles** (Public, Private, Domain).

### Timeline Example

1. **11:00** - You enable kill switch, VPN is connected ✅
2. **11:05** - Everything works normally, kill switch monitoring in background
3. **11:23** - VPN connection drops (ISP issue, router crash, etc.)
4. **11:23:02** - Kill switch **detects disconnection**
5. **11:23:03** - Kill switch **blocks all traffic immediately**
6. **11:23:05** - Alert popup appears on screen
7. **Data leak prevented** ✅

Without kill switch, step 3 would have resulted in all data leaking over unencrypted connection.

### Verification

To check if kill switch is active:

```powershell
netsh advfirewall firewall show rule name="GVPN Kill Switch"
```

If it shows rules with `Enabled: Yes`, the kill switch is active.

## Restoring Network Access

If kill switch blocks your traffic, restore it via:

**Option 1: GVPN UI (Easiest)**
1. Right-click GVPN tray icon
2. Click "Kill Switch: ON" → "Kill Switch: OFF"

**Option 2: Command Prompt (Admin)**
```powershell
netsh advfirewall firewall delete rule name="GVPN Kill Switch"
netsh advfirewall firewall delete rule name="GVPN Kill Switch - Loopback"
netsh advfirewall firewall delete rule name="GVPN Kill Switch - DNS"
```

**Option 3: Restart Computer**
Restart Windows (all firewall rules will persist until manually removed).

**Option 4: Windows Firewall Settings (Manual)**
1. Settings → Network & Internet → Windows Defender Firewall
2. Advanced settings → Outbound Rules
3. Find and delete GVPN rules manually

## Requirements

- **Administrator privileges** - Must run GVPN as admin for firewall rules to apply
  - Right-click `GVPN.exe` → **Run as Administrator**
- **Windows Firewall** - Must be enabled (GVPN will enable it if not)
- **Windows 7+** - netsh commands work on all modern Windows versions

## Safety Notes

⚠️ **Kill Switch is a Safety Feature**

- It's designed to **prevent data leaks**, not to disconnect you
- Once enabled, it **only activates if VPN drops**
- It **doesn't interrupt your VPN** or your work
- It's a **passive monitor**, not an active tool

**DO use kill switch when:**
- ✅ You want guaranteed privacy (if VPN drops, you won't leak)
- ✅ You're on untrusted networks (cafes, airports, hotels)
- ✅ You're handling sensitive data (passwords, private documents)
- ✅ You're in a restrictive country where surveillance is high

**DON'T use kill switch when:**
- ❌ You want to disable the VPN manually (just use "Disconnect" button)
- ❌ You want to block traffic intentionally (use Windows Firewall directly)
- ❌ You need to access local network resources frequently

⚠️ **Not a Complete Isolation**

The kill switch blocks **outbound traffic only**. It does NOT:
- Block inbound connections (incoming packets)
- Disconnect already-established sessions
- Kill running processes
- Prevent IP leaks from Layer 2/3 (physical layer vulnerabilities)

For complete isolation, physically disconnect your network adapter.

## Troubleshooting

| Problem | Solution |
|---------|----------|
| Kill Switch not working | Run GVPN as Administrator (right-click → Run as Administrator) |
| VPN drops but kill switch doesn't activate | Check logs in `vpn.log` for errors; may need admin privileges |
| Can't remove kill switch rules | Run Command Prompt as Administrator, paste netsh commands above |
| Still see network activity | Restart computer to ensure rules apply |
| Can't open GVPN after kill switch | Restart computer or remove firewall rules via Command Prompt |

## Logs

All kill switch activity is logged to `vpn.log`:

```
2026-11-07 12:00:00 - Kill switch ENABLED - will block all traffic if VPN disconnects
2026-11-07 12:01:00 - VPN status: Connected
2026-11-07 12:05:00 - VPN disconnected while kill switch enabled - blocking all traffic!
2026-11-07 12:05:01 - Enabling kill switch - blocking all traffic
2026-11-07 12:05:02 - Kill switch enabled - all traffic blocked except loopback and DNS
2026-11-07 12:10:00 - Kill switch DISABLED
2026-11-07 12:10:01 - Disabling kill switch
2026-11-07 12:10:02 - Kill switch disabled
```

Check the log if kill switch doesn't work as expected.

## Legal & Ethical Notes

- Use kill switch only on **your own computer**
- Kill switch is for **privacy protection only**
- Do not use to disrupt others' networks or systems
- Some organizations may monitor/block this tool
- Always ensure you have legitimate reasons before enabling

## Questions?

Check `vpn.log` in the GVPN directory for detailed error messages if something goes wrong.
