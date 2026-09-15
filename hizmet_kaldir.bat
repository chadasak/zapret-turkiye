@echo off
setlocal enabledelayedexpansion
cd /d "%~dp0"

REM Log dosya yolu
set "LOGFILE=%~dp0kurulum.log"

REM Yonetici kontrolu
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo [!] Yonetici olarak calistirin!
    echo [%date% %time%] [HATA] Kaldirma: Yonetici haklari alinmadi >> "!LOGFILE!"
    pause
    exit /b 1
)

echo [*] Zapret durduruluyor ve kaldiriliyor...
echo [%date% %time%] ========== KALDIR ISLEMI BASLADI ========== >> "!LOGFILE!"

REM Tepsi uygulamasi ONCE kapanmali: watchdog acikken winws.exe'yi geri baslatir
taskkill /f /im ZapretTray.exe >nul 2>&1
if %errorlevel% equ 0 (
    echo [+] ZapretTray.exe kapatildi
    echo [%date% %time%] [OK] ZapretTray.exe kapatildi >> "!LOGFILE!"
) else (
    echo [INFO] ZapretTray.exe calismiyordu
    echo [%date% %time%] [INFO] ZapretTray.exe calismiyordu >> "!LOGFILE!"
)

taskkill /f /im winws.exe >nul 2>&1
if %errorlevel% equ 0 (
    echo [+] winws.exe islemi sonlandirildi
    echo [%date% %time%] [OK] winws.exe islemi sonlandirildi >> "!LOGFILE!"
) else (
    echo [INFO] Calisir durumda winws.exe bulunamadi
    echo [%date% %time%] [INFO] Calisir winws.exe bulunamadi >> "!LOGFILE!"
)

schtasks /delete /tn "ZapretDPI" /f >nul 2>&1
if %errorlevel% equ 0 (
    echo [+] Zamanlayici gorevi silindi
    echo [%date% %time%] [OK] Zamanlayici gorevi silindi >> "!LOGFILE!"
) else (
    echo [INFO] Zamanlayici gorevi zaten yok
    echo [%date% %time%] [INFO] Zamanlayici gorevi bulunamadi >> "!LOGFILE!"
)

REM Tepsi uygulamasinin acilis gorevi
schtasks /delete /tn "ZapretTrayUI" /f >nul 2>&1
if %errorlevel% equ 0 (
    echo [+] Tepsi acilis gorevi silindi
    echo [%date% %time%] [OK] ZapretTrayUI gorevi silindi >> "!LOGFILE!"
) else (
    echo [INFO] Tepsi acilis gorevi zaten yok
    echo [%date% %time%] [INFO] ZapretTrayUI gorevi bulunamadi >> "!LOGFILE!"
)

echo [*] Firewall kurali kaldiriliyor...
netsh advfirewall firewall delete rule name="Zapret" program="%~dp0bin\winws.exe" >nul 2>&1
if %errorlevel% equ 0 (
    echo [+] Firewall kurali kaldirildi
    echo [%date% %time%] [OK] Firewall kurali kaldirildi >> "!LOGFILE!"
) else (
    echo [INFO] Firewall kurali zaten yok
    echo [%date% %time%] [INFO] Firewall kurali bulunamadi >> "!LOGFILE!"
)

echo [*] Defender istisnalari temizleniyor...
REM Eski surumler klasoru sondaki ters bolu ile eklemis olabilir; iki sekli de kaldir.
powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference='SilentlyContinue'; if (-not (Get-Command Remove-MpPreference -ErrorAction SilentlyContinue)) { exit 2 }; $full=[IO.Path]::GetFullPath('%~dp0'); $trim=$full.TrimEnd('\'); $exe=[IO.Path]::GetFullPath('%~dp0bin\winws.exe'); Remove-MpPreference -ExclusionPath $trim; Remove-MpPreference -ExclusionPath $full; Remove-MpPreference -ExclusionPath ($trim + '\'); Remove-MpPreference -ExclusionProcess 'winws.exe'; Remove-MpPreference -ExclusionProcess $exe; exit 0" >nul 2>&1
set "LAST_ERR=%errorlevel%"
if %LAST_ERR% equ 0 (
    echo [+] Defender istisnalari temizlendi
    echo [%date% %time%] [OK] Defender istisnalari temizlendi >> "!LOGFILE!"
) else if %LAST_ERR% equ 2 (
    echo [INFO] Defender cmdlet bulunamadi, istisna temizleme atlandi
    echo [%date% %time%] [INFO] Defender cmdlet bulunamadi >> "!LOGFILE!"
) else (
    echo [INFO] Defender istisnasi kaldirma atlandi
    echo [%date% %time%] [INFO] Defender istisnasi kaldirma atlandi >> "!LOGFILE!"
)

echo.
echo [OK] Zapret basariyla kaldirildi.
echo [INFO] Sorun giderme icin bakiniz: kurulum.log
echo [%date% %time%] ========== KALDIR ISLEMI TAMAMLANDI ========== >> "!LOGFILE!"

pause
