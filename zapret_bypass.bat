@echo off
setlocal enabledelayedexpansion
cd /d "%~dp0"

REM Allow --silent to skip interactive pauses (use: zapret_bypass.bat --silent)
set "SILENT=0"
if /I "%~1"=="--silent" set "SILENT=1"

REM Log dosya yolu
set "LOGFILE=%~dp0kurulum.log"

if not exist "%~dp0bin\winws.exe" (
  echo [HATA] bin\winws.exe bulunamadi.
  echo [%date% %time%] [HATA] bin\winws.exe bulunamadi >> "%LOGFILE%"
  if %SILENT% equ 0 pause
  exit /b 2
)

if not exist "%~dp0bin\WinDivert64.sys" (
  echo [HATA] bin\WinDivert64.sys bulunamadi.
  echo [%date% %time%] [HATA] bin\WinDivert64.sys bulunamadi >> "%LOGFILE%"
  if %SILENT% equ 0 pause
  exit /b 3
)

net session >nul 2>&1
if %errorlevel% neq 0 (
  echo [HATA] Yonetici olarak calistirin.
  echo [%date% %time%] [HATA] Yonetici hakki yok >> "%LOGFILE%"
  if %SILENT% equ 0 pause
  exit /b 5
)

echo Zapret: Narin-Oyun/App + Agir-Web Hibrit Mod...
echo [%date% %time%] [INFO] Zapret bypass servisi baslatildi >> "%LOGFILE%"

echo [*] DNS ayari kontrol ediliyor...
powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference='SilentlyContinue'; $servers = @(); try { $servers = Get-DnsClientServerAddress -AddressFamily IPv4 -ErrorAction Stop | ForEach-Object { $_.ServerAddresses }; } catch { $servers = @(); }; $servers = $servers | Select-Object -Unique; $allowed = @('1.1.1.1','1.0.0.1','8.8.8.8','8.8.4.4','9.9.9.9','9.9.9.10','208.67.222.222','208.67.220.220'); if ($servers | Where-Object { $_ -in $allowed }) { exit 0 } else { $servers | ForEach-Object { $_ }; exit 1 }" >nul 2>&1
set "LAST_ERR=%errorlevel%"
if %LAST_ERR% neq 0 (
    echo [WARN] DNS ayari tespit edilmedi.
    echo [WARN] Turkiye'de DNS eklenmezse calismayabilir.
    echo [WARN] Onerilen DNS: 1.1.1.1 (yedek: 1.0.0.1)
    echo [WARN] Ornek: netsh interface ipv4 set dns name="Wi-Fi" static 1.1.1.1 primary
    echo [%date% %time%] [WARN] DNS ayari tespit edilmedi - 1.1.1.1 oneriliyor >> "%LOGFILE%"
) else (
    echo [+] DNS ayari uygun gorundu.
    echo [%date% %time%] [OK] DNS ayari dogrulandi >> "%LOGFILE%"
)

echo [*] DNS cache temizleniyor...
ipconfig /flushdns >nul 2>&1
echo [%date% %time%] [OK] DNS cache temizlendi >> "%LOGFILE%"

echo [*] winws.exe calistiriliyor...

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
  echo [HATA] winws.exe hata kodu: %EXIT_CODE%
  echo [%date% %time%] [HATA] winws.exe kapandi - Hata kodu: %EXIT_CODE% >> "%LOGFILE%"
) else (
  echo [+] Servis normal kapandi
  echo [%date% %time%] [OK] Servis normal kapandi >> "%LOGFILE%"
)

if %SILENT% equ 0 (
  pause
)
exit /b %EXIT_CODE%
