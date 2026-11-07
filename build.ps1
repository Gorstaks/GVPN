# GVPN build script
#   .\build.ps1            -> bin\Release\GVPN.exe (single exe, OpenVPN embedded)
#   .\build.ps1 -Release   -> also copies it to releases\<version>\GVPN.exe
# Version comes from AssemblyInfo.cs (AssemblyInformationalVersion).
param(
    [switch]$Release
)

$ErrorActionPreference = "Stop"
$projectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$outputDir = Join-Path $projectDir "bin\Release"
$outputExe = Join-Path $outputDir "GVPN.exe"

$versionMatch = Select-String -Path (Join-Path $projectDir "AssemblyInfo.cs") -Pattern 'AssemblyInformationalVersion\("([^"]+)"\)'
if (-not $versionMatch) { throw "Version not found in AssemblyInfo.cs" }
$version = $versionMatch.Matches[0].Groups[1].Value

New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
Write-Host "Building GVPN $version..." -ForegroundColor Cyan

$cscExe = @(
    "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\csc.exe",
    "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\Roslyn\csc.exe",
    "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
    "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $cscExe) { throw "csc.exe not found. Install .NET Framework 4.8 or Visual Studio." }

$sourceFiles = @(
    "Program.cs", "VpnTrayApplication.cs", "VpnListForm.cs", "VpnManager.cs",
    "ConfigSanitizer.cs", "KillSwitch.cs", "SelfSetup.cs", "BundledOpenVpn.cs", "AssemblyInfo.cs", "Logger.cs"
) | ForEach-Object { Join-Path $projectDir $_ }

# Bundled OpenVPN client + TAP-Windows6 driver are embedded in GVPN.exe as resources named
# "openvpn/<relative path>" and unpacked on first run to %ProgramFiles%\GVPN\openvpn
# (BundledOpenVpn.cs). Every vendor binary must carry a valid signature before it is embedded.
$openVpnSrc = Join-Path $projectDir "thirdparty\openvpn"
if (-not (Test-Path (Join-Path $openVpnSrc "bin\openvpn.exe"))) {
    throw "Bundled OpenVPN missing at $openVpnSrc (see thirdparty\openvpn\README.txt)"
}
$resourceArgs = @()
Get-ChildItem $openVpnSrc -Recurse -File | ForEach-Object {
    if ($_.Extension -in '.exe', '.dll', '.sys', '.cat') {
        $sig = Get-AuthenticodeSignature $_.FullName
        if ($sig.Status -ne 'Valid') { throw "Bundled file has no valid signature: $($_.FullName) ($($sig.Status))" }
    }
    $relative = $_.FullName.Substring($openVpnSrc.Length + 1).Replace('\', '/')
    $resourceArgs += "/resource:$($_.FullName),openvpn/$relative"
}

# Microsoft.CSharp: KillSwitch/SelfSetup use 'dynamic' for the Firewall and Task Scheduler COM APIs.
$references = @("System.dll", "System.Core.dll", "System.Windows.Forms.dll", "System.Drawing.dll", "Microsoft.CSharp.dll") |
    ForEach-Object { "/reference:$_" }

# requireAdministrator: openvpn.exe configures the TAP adapter/routes, the kill switch edits
# firewall rules, and first run installs the TAP driver.
$manifest = Join-Path $projectDir "app.manifest"

& $cscExe /nologo /target:winexe /out:"$outputExe" /platform:anycpu /optimize+ /win32manifest:"$manifest" $references $resourceArgs $sourceFiles
if ($LASTEXITCODE -ne 0) { throw "Build failed with exit code $LASTEXITCODE" }

# A stale openvpn\ folder beside the exe would take precedence over the embedded copy.
$staleBeside = Join-Path $outputDir "openvpn"
if (Test-Path $staleBeside) { Remove-Item $staleBeside -Recurse -Force }

Write-Host ("Build successful: $outputExe ({0:N1} MB)" -f ((Get-Item $outputExe).Length / 1MB)) -ForegroundColor Green

if ($Release) {
    # A release is the single exe; users download it and run it.
    $releaseDir = Join-Path $projectDir "releases\$version"
    New-Item -ItemType Directory -Path $releaseDir -Force | Out-Null
    Get-ChildItem $releaseDir -Force | Remove-Item -Recurse -Force
    Copy-Item $outputExe -Destination $releaseDir
    Write-Host "Release created: releases\$version\GVPN.exe" -ForegroundColor Green
}
