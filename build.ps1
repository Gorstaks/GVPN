# GVpnCSharp Build Script
# Compiles the C# project using csc (C# Compiler)

$projectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$outputDir = Join-Path $projectDir "bin\Release"
$outputExe = Join-Path $outputDir "GVPN.exe"

# Ensure output directory exists
if (!(Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}

Write-Host "Building GVPN..." -ForegroundColor Cyan

# Find csc.exe location (Visual Studio or Windows SDK)
$cscPaths = @(
    "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\csc.exe",
    "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\Roslyn\csc.exe",
    "C:\Program Files\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\Roslyn\csc.exe",
    "C:\Program Files\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\Roslyn\csc.exe",
    "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
    "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
)

$cscExe = $null
foreach ($path in $cscPaths) {
    if (Test-Path $path) {
        $cscExe = $path
        Write-Host "Found csc.exe: $cscExe" -ForegroundColor Green
        break
    }
}

if (!$cscExe) {
    Write-Host "ERROR: csc.exe not found. Please install Visual Studio or Windows SDK." -ForegroundColor Red
    exit 1
}

# Compile
$sourceFiles = @(
    "Program.cs",
    "VpnTrayApplication.cs",
    "VpnListForm.cs",
    "VpnManager.cs",
    "Logger.cs"
) | ForEach-Object { Join-Path $projectDir $_ }

$references = @(
    "System.dll",
    "System.Core.dll",
    "System.Windows.Forms.dll",
    "System.Drawing.dll",
    "System.Net.Http.dll"
)

$referenceArgs = $references | ForEach-Object { "/reference:$_" }

& $cscExe `
    /target:winexe `
    /out:"$outputExe" `
    /platform:anycpu `
    /optimize+ `
    $referenceArgs `
    $sourceFiles

if ($LASTEXITCODE -eq 0) {
    Write-Host "Build successful!" -ForegroundColor Green
    Write-Host "Output: $outputExe" -ForegroundColor Green
    Write-Host ""
    Write-Host "To run: & '$outputExe'" -ForegroundColor Cyan
} else {
    Write-Host "Build failed with exit code: $LASTEXITCODE" -ForegroundColor Red
    exit 1
}
