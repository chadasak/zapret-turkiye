@echo off
setlocal enabledelayedexpansion
cd /d "%~dp0"

REM Allow --silent to skip interactive pauses (use: install.bat --silent)
set "SILENT=0"
if /I "%~1"=="--silent" set "SILENT=1"

REM Log file path (quoted at set to preserve spaces)
set "LOGFILE=%~dp0zapret.log"

REM Administrator check
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] Run this as administrator.
    echo [%date% %time%] [ERROR] no administrator rights >> "%LOGFILE%"
    if %SILENT% equ 0 pause
    exit /b 1
)

echo [*] Starting install...
echo [%date% %time%] ========== INSTALL STARTED ========== >> "%LOGFILE%"

echo [*] Checking DNS setting...
powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference='SilentlyContinue'; $servers = @(); try { $servers = Get-DnsClientServerAddress -AddressFamily IPv4 -ErrorAction Stop | ForEach-Object { $_.ServerAddresses }; } catch { $servers = @(); }; $servers = $servers | Select-Object -Unique; $allowed = @('1.1.1.1','1.0.0.1','8.8.8.8','8.8.4.4','9.9.9.9','9.9.9.10','208.67.222.222','208.67.220.220'); if ($servers | Where-Object { $_ -in $allowed }) { exit 0 } else { $servers | ForEach-Object { $_ }; exit 1 }" >nul 2>&1
set "LAST_ERR=%errorlevel%"
if %LAST_ERR% neq 0 (
    echo [WARN] No public DNS detected.
    echo [WARN] Without a public DNS this may not work in Turkey.
    echo [WARN] Recommended DNS: 1.1.1.1 (backup: 1.0.0.1)
    echo [WARN] Example: netsh interface ipv4 set dns name="Wi-Fi" static 1.1.1.1 primary
    echo [%date% %time%] [WARN] no public DNS detected - 1.1.1.1 recommended >> "%LOGFILE%"
) else (
    echo [+] DNS setting looks fine.
    echo [%date% %time%] [OK] DNS setting verified >> "%LOGFILE%"
)

echo [*] Checking required files...
if not exist "%~dp0bin\winws.exe" (
    echo [ERROR] bin\winws.exe not found.
    echo [%date% %time%] [ERROR] bin\winws.exe not found - install aborted >> "%LOGFILE%"
    if %SILENT% equ 0 pause
    exit /b 1
)
echo [+] bin\winws.exe found
echo [%date% %time%] [OK] bin\winws.exe found >> "%LOGFILE%"

if not exist "%~dp0bin\WinDivert64.sys" (
    echo [ERROR] bin\WinDivert64.sys not found.
    echo [%date% %time%] [ERROR] bin\WinDivert64.sys not found - install aborted >> "%LOGFILE%"
    if %SILENT% equ 0 pause
    exit /b 1
)
echo [+] bin\WinDivert64.sys found
echo [%date% %time%] [OK] bin\WinDivert64.sys found >> "%LOGFILE%"

if not exist "%~dp0zapret_task.cmd" (
    echo [ERROR] zapret_task.cmd not found.
    echo [%date% %time%] [ERROR] zapret_task.cmd not found - install aborted >> "%LOGFILE%"
    if %SILENT% equ 0 pause
    exit /b 1
)
echo [+] zapret_task.cmd found
echo [%date% %time%] [OK] zapret_task.cmd found >> "%LOGFILE%"

echo [*] Removing mark-of-the-web from files...
powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference='Stop'; try { Get-ChildItem -LiteralPath '%~dp0' -Recurse -File | Unblock-File; exit 0 } catch { exit 1 }" >nul 2>&1
if %errorlevel% equ 0 (
    echo [+] Files unblocked
    echo [%date% %time%] [OK] MOTW cleanup done >> "%LOGFILE%"
) else (
    echo [INFO] Could not unblock files (policy restriction?)
    echo [%date% %time%] [INFO] MOTW cleanup skipped >> "%LOGFILE%"
)

echo [*] Checking Defender exclusions...
powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference='Stop'; if (-not (Get-Command Add-MpPreference -ErrorAction SilentlyContinue)) { exit 2 }; $folder=[IO.Path]::GetFullPath('%~dp0').TrimEnd('\'); Add-MpPreference -ExclusionPath $folder -ErrorAction Stop; Add-MpPreference -ExclusionProcess 'winws.exe' -ErrorAction SilentlyContinue; exit 0" >nul 2>&1
set "LAST_ERR=%errorlevel%"
if %LAST_ERR% equ 0 (
    echo [+] Defender exclusions added
    echo [%date% %time%] [OK] Defender exclusions added >> "%LOGFILE%"
) else (
    if %LAST_ERR% equ 2 (
        echo [INFO] Defender cmdlets missing, exclusion skipped
        echo [%date% %time%] [INFO] Defender cmdlets missing >> "%LOGFILE%"
    ) else (
        echo [INFO] Could not add Defender exclusion (permission/policy?)
        echo [%date% %time%] [INFO] could not add Defender exclusion >> "%LOGFILE%"
    )
)

echo [*] Removing old scheduled task...
schtasks /delete /tn "ZapretDPI" /f >nul 2>&1
echo [+] Old task removed
echo [%date% %time%] [OK] old task removed >> "%LOGFILE%"

echo [*] Stopping any running winws.exe...
taskkill /f /im winws.exe >nul 2>&1
echo [+] Stopped
echo [%date% %time%] [OK] running processes stopped >> "%LOGFILE%"

echo [*] Creating scheduled task...
set "TASKCMD=%~dp0zapret_task.cmd"
:: Quote the /tr argument explicitly to avoid nested-quote problems when path contains spaces
schtasks /create /tn "ZapretDPI" /tr "\"%TASKCMD%\"" /sc onlogon /rl highest /ru "SYSTEM" /f >nul 2>&1
set "LAST_ERR=%errorlevel%"
if %LAST_ERR% neq 0 (
    echo [ERROR] Could not create task, code: %LAST_ERR%
    echo [%date% %time%] [ERROR] Could not create task, code: %LAST_ERR% >> "%LOGFILE%"
    if %SILENT% equ 0 pause
    exit /b 1
)
echo [+] Task created
echo [%date% %time%] [OK] scheduled task created >> "%LOGFILE%"

echo [*] Starting Zapret...
schtasks /run /tn "ZapretDPI" >nul 2>&1
set "LAST_ERR=%errorlevel%"
if %LAST_ERR% neq 0 (
    echo [ERROR] Could not run task, code: %LAST_ERR%
    echo [%date% %time%] [ERROR] scheduled task did not run, code: %LAST_ERR% >> "%LOGFILE%"
    if %SILENT% equ 0 pause
    exit /b 1
)
echo [+] Started
echo [%date% %time%] [OK] Zapret started >> "%LOGFILE%"

echo [*] Flushing DNS cache...
ipconfig /flushdns >nul 2>&1
echo [+] DNS cache flushed
echo [%date% %time%] [OK] DNS cache flushed >> "%LOGFILE%"

echo [*] Removing old firewall rule...
netsh advfirewall firewall delete rule name="Zapret" program="%~dp0bin\winws.exe" >nul 2>&1
set "LAST_ERR=%errorlevel%"
if %LAST_ERR% equ 0 (
    echo [+] Old firewall rule removed
    echo [%date% %time%] [OK] old firewall rule removed >> "%LOGFILE%"
) else (
    echo [INFO] No old firewall rule
    echo [%date% %time%] [INFO] no old firewall rule >> "%LOGFILE%"
)

echo [*] Adding firewall rule...
netsh advfirewall firewall add rule name="Zapret" dir=in action=allow program="%~dp0bin\winws.exe" enable=yes >nul 2>&1
set "LAST_ERR=%errorlevel%"
if %LAST_ERR% neq 0 (
    echo [ERROR] Could not add firewall rule, code: %LAST_ERR%
    echo [%date% %time%] [ERROR] Could not add firewall rule, code: %LAST_ERR% >> "%LOGFILE%"
    if %SILENT% equ 0 pause
    exit /b 1
)
echo [+] Firewall rule added
echo [%date% %time%] [OK] firewall rule added >> "%LOGFILE%"

echo.
echo [OK] Zapret installed and running.
echo [OK] It will start automatically at boot.
echo [INFO] To remove it: uninstall.bat
echo [INFO] For troubleshooting see: zapret.log
echo [%date% %time%] ========== INSTALL COMPLETED ========== >> "%LOGFILE%"

if %SILENT% equ 0 (
    pause
)

exit /b 0
