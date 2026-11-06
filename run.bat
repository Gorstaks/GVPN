@echo off
REM Simple batch script to run GVpn.exe
REM Supports launching with admin privileges if needed

cd /d "%~dp0"

REM Check if .exe exists
if not exist "bin\Release\GVpn.exe" (
    echo.
    echo GVpn.exe not found! Building first...
    echo.
    powershell -NoProfile -ExecutionPolicy Bypass -File "build.ps1"
    if errorlevel 1 (
        echo.
        echo Build failed. Exiting.
        pause
        exit /b 1
    )
)

REM Run the application
echo Starting GVpn...
start "" "bin\Release\GVpn.exe"
