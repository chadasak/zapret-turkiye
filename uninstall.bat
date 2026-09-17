@echo off
setlocal enabledelayedexpansion
cd /d "%~dp0"

REM Log file path
set "LOGFILE=%~dp0zapret.log"

REM Administrator check
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] Run this as administrator.
    echo [%date% %time%] [ERROR] uninstall: no administrator rights >> "!LOGFILE!"
    pause
    exit /b 1
)

echo [*] Stopping and removing Zapret...
echo [%date% %time%] ========== UNINSTALL STARTED ========== >> "!LOGFILE!"

REM The tray app must close FIRST, otherwise its watchdog restarts winws.exe
taskkill /f /im ZapretTray.exe >nul 2>&1
if %errorlevel% equ 0 (
    echo [+] ZapretTray.exe closed
    echo [%date% %time%] [OK] ZapretTray.exe closed >> "!LOGFILE!"
) else (
    echo [INFO] ZapretTray.exe was not running
    echo [%date% %time%] [INFO] ZapretTray.exe was not running >> "!LOGFILE!"
)

taskkill /f /im winws.exe >nul 2>&1
if %errorlevel% equ 0 (
    echo [+] winws.exe stopped
    echo [%date% %time%] [OK] winws.exe stopped >> "!LOGFILE!"
) else (
    echo [INFO] No running winws.exe found
    echo [%date% %time%] [INFO] no running winws.exe found >> "!LOGFILE!"
)

schtasks /delete /tn "ZapretDPI" /f >nul 2>&1
if %errorlevel% equ 0 (
    echo [+] Scheduled task deleted
    echo [%date% %time%] [OK] scheduled task deleted >> "!LOGFILE!"
) else (
    echo [INFO] No scheduled task to delete
    echo [%date% %time%] [INFO] scheduled task not found >> "!LOGFILE!"
)

REM Logon task of the tray app
schtasks /delete /tn "ZapretTrayUI" /f >nul 2>&1
if %errorlevel% equ 0 (
    echo [+] Tray logon task deleted
    echo [%date% %time%] [OK] ZapretTrayUI task deleted >> "!LOGFILE!"
) else (
    echo [INFO] No tray logon task to delete
    echo [%date% %time%] [INFO] ZapretTrayUI task not found >> "!LOGFILE!"
)

echo [*] Removing firewall rule...
netsh advfirewall firewall delete rule name="Zapret" program="%~dp0bin\winws.exe" >nul 2>&1
if %errorlevel% equ 0 (
    echo [+] Firewall rule removed
    echo [%date% %time%] [OK] firewall rule removed >> "!LOGFILE!"
) else (
    echo [INFO] No firewall rule to remove
    echo [%date% %time%] [INFO] firewall rule not found >> "!LOGFILE!"
)

echo [*] Removing Defender exclusions...
REM Older versions may have added the folder with a trailing backslash; remove both forms.
powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference='SilentlyContinue'; if (-not (Get-Command Remove-MpPreference -ErrorAction SilentlyContinue)) { exit 2 }; $full=[IO.Path]::GetFullPath('%~dp0'); $trim=$full.TrimEnd('\'); $exe=[IO.Path]::GetFullPath('%~dp0bin\winws.exe'); Remove-MpPreference -ExclusionPath $trim; Remove-MpPreference -ExclusionPath $full; Remove-MpPreference -ExclusionPath ($trim + '\'); Remove-MpPreference -ExclusionProcess 'winws.exe'; Remove-MpPreference -ExclusionProcess $exe; exit 0" >nul 2>&1
set "LAST_ERR=%errorlevel%"
if %LAST_ERR% equ 0 (
    echo [+] Defender exclusions removed
    echo [%date% %time%] [OK] Defender exclusions removed >> "!LOGFILE!"
) else if %LAST_ERR% equ 2 (
    echo [INFO] Defender cmdlets missing, exclusion cleanup skipped
    echo [%date% %time%] [INFO] Defender cmdlets missing >> "!LOGFILE!"
) else (
    echo [INFO] Defender exclusion removal skipped
    echo [%date% %time%] [INFO] Defender exclusion removal skipped >> "!LOGFILE!"
)

echo.
echo [OK] Zapret removed.
echo [INFO] For troubleshooting see: zapret.log
echo [%date% %time%] ========== UNINSTALL COMPLETED ========== >> "!LOGFILE!"

pause
