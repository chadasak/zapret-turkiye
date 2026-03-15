@echo off
setlocal enabledelayedexpansion
cd /d "%~dp0"

REM Log dosya yolu
set LOGFILE=%~dp0kurulum.log

echo Zapret: Narin(Oyun/App) + Agir(Web) Hibrit Mod...
echo [%date% %time%] [INFO] Zapret bypass servisi baslatildi >> "!LOGFILE!"

echo [*] DNS cache temizleniyor...
ipconfig /flushdns >nul 2>&1
echo [%date% %time%] [OK] DNS cache temizlendi >> "!LOGFILE!"

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

if %errorlevel% neq 0 (
    echo [HATA] winws.exe hata kodu: %errorlevel%
    echo [%date% %time%] [HATA] winws.exe kapandi - Hata kodu: %errorlevel% >> "!LOGFILE!"
) else (
    echo [+] Servis normal kapandi
    echo [%date% %time%] [OK] Servis normal kapandi >> "!LOGFILE!"
)

pause
