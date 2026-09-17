@echo off
setlocal enabledelayedexpansion
cd /d "%~dp0"

REM Allow --silent to skip interactive pauses (use: zapret_bypass.bat --silent)
set "SILENT=0"
if /I "%~1"=="--silent" set "SILENT=1"

REM Log file path
set "LOGFILE=%~dp0zapret.log"

if not exist "%~dp0bin\winws.exe" (
  echo [ERROR] bin\winws.exe not found.
  echo [%date% %time%] [ERROR] bin\winws.exe not found >> "%LOGFILE%"
  if %SILENT% equ 0 pause
  exit /b 2
)

if not exist "%~dp0bin\WinDivert64.sys" (
  echo [ERROR] bin\WinDivert64.sys not found.
  echo [%date% %time%] [ERROR] bin\WinDivert64.sys not found >> "%LOGFILE%"
  if %SILENT% equ 0 pause
  exit /b 3
)

net session >nul 2>&1
if %errorlevel% neq 0 (
  echo [ERROR] Run this as administrator.
  echo [%date% %time%] [ERROR] no administrator rights >> "%LOGFILE%"
  if %SILENT% equ 0 pause
  exit /b 5
)

echo Zapret: light game/app + heavy web hybrid mode...
echo [%date% %time%] [INFO] Zapret bypass started >> "%LOGFILE%"

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

echo [*] Flushing DNS cache...
ipconfig /flushdns >nul 2>&1
echo [%date% %time%] [OK] DNS cache flushed >> "%LOGFILE%"

echo [*] Running winws.exe...

"%~dp0bin\winws.exe" ^
  --wf-tcp=80,443 ^
  --dpi-desync=fake,split2 ^
  --dpi-desync-autottl=2 ^
  --dpi-desync-fooling=md5sig ^
  --dpi-desync-split-pos=sniext+4 ^
  --dpi-desync-repeats=2 ^
  --new ^
  --wf-udp=51820 ^
  --dpi-desync=fake ^
  --dpi-desync-repeats=2 ^
  --dpi-desync-any-protocol ^
  --dpi-desync-cutoff=d2 ^
  --dpi-desync-autottl=2

set "EXIT_CODE=%errorlevel%"

if %EXIT_CODE% neq 0 (
  echo [ERROR] winws.exe exit code: %EXIT_CODE%
  echo [%date% %time%] [ERROR] winws.exe exited - code: %EXIT_CODE% >> "%LOGFILE%"
) else (
  echo [+] Exited normally
  echo [%date% %time%] [OK] exited normally >> "%LOGFILE%"
)

if %SILENT% equ 0 (
  pause
)
exit /b %EXIT_CODE%
