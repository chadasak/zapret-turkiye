@echo off
setlocal enabledelayedexpansion
cd /d "%~dp0"

REM Log dosya yolu
set LOGFILE=%~dp0kurulum.log

REM Yönetici kontrolü
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo [!] Yonetici olarak calistirin!
    echo [%date% %time%] [HATA] Kaldirma: Yonetici haklari alinmadi >> "!LOGFILE!"
    pause
    exit /b 1
)

echo [*] Zapret durduruluyor ve kaldiriliyor...
echo [%date% %time%] ========== KALDIR ISLEMI BASLADI ========== >> "!LOGFILE!"

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

echo [*] Firewall kuralı kaldırılıyor...
netsh advfirewall firewall delete rule name="Zapret" program="%~dp0bin\winws.exe" >nul 2>&1
if %errorlevel% equ 0 (
    echo [+] Firewall kurali kaldirildi
    echo [%date% %time%] [OK] Firewall kurali kaldirildi >> "!LOGFILE!"
) else (
    echo [INFO] Firewall kurali zaten yok
    echo [%date% %time%] [INFO] Firewall kurali bulunamadi >> "!LOGFILE!"
)

echo.
echo [OK] Zapret basariyla kaldirildi.
echo [INFO] Sorun giderme icin bakiniz: kurulum.log
echo [%date% %time%] ========== KALDIR ISLEMI TAMAMLANDI ========== >> "!LOGFILE!"

pause
