@echo off
setlocal enabledelayedexpansion
cd /d "%~dp0"

REM Log dosya yolu
set LOGFILE=%~dp0kurulum.log

REM Yönetici kontrolü
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo [!] Yonetici olarak calistirin!
    echo [%date% %time%] [HATA] Yonetici haklari alinmadi >> "!LOGFILE!"
    pause
    exit /b 1
)

echo [*] Kurulum basliyor...
echo [%date% %time%] ========== KURULUM BASLADI ========== >> "!LOGFILE!"

echo [*] Gerekli dosyalar kontrol ediliyor...
if not exist "%~dp0bin\winws.exe" (
    echo [HATA] bin\winws.exe bulunamadi!
    echo [%date% %time%] [HATA] bin\winws.exe dosyasi bulunamadi - Kurulum iptal >> "!LOGFILE!"
    pause
    exit /b 1
)
echo [+] bin\winws.exe bulundu
echo [%date% %time%] [OK] bin\winws.exe bulundu >> "!LOGFILE!"

echo [*] Eski gorev kontrol ediliyor ve kaldiriliyorsa...
schtasks /delete /tn "ZapretDPI" /f >nul 2>&1
echo [+] Eski gorev temizlendi
echo [%date% %time%] [OK] Eski gorev temizlendi >> "!LOGFILE!"

echo [*] Eski calisan winws.exe kapatiliyor...
taskkill /f /im winws.exe >nul 2>&1
echo [+] Kapandi
echo [%date% %time%] [OK] Calisan processler kapatildi >> "!LOGFILE!"

echo [*] Gorev zamanlayicida olusturuluyor...
schtasks /create /tn "ZapretDPI" /tr "wscript.exe \"%~dp0zapret_sessiz.vbs\"" /sc onlogon /rl highest /f >nul 2>&1

if %errorlevel% neq 0 (
    echo [HATA] Gorev olusturulamadi!
    echo Hata kodu: %errorlevel%
    echo [%date% %time%] [HATA] Gorev olusturulamadi! Kod: %errorlevel% >> "!LOGFILE!"
    pause
    exit /b 1
)
echo [+] Gorev olusturuldu
echo [%date% %time%] [OK] Zamanlayici gorevi olusturuldu >> "!LOGFILE!"

echo [*] Zapret servis baslatiliyor...
schtasks /run /tn "ZapretDPI" >nul 2>&1

if %errorlevel% neq 0 (
    echo [HATA] Gorev calistirilamadi!
    echo Hata kodu: %errorlevel%
    echo [%date% %time%] [HATA] Zamanlayici gorevi calistirilmadi! Kod: %errorlevel% >> "!LOGFILE!"
    pause
    exit /b 1
)
echo [+] Servis baslatildi
echo [%date% %time%] [OK] Zapret servisi baslatildi >> "!LOGFILE!"

echo [*] DNS cache temizleniyor...
ipconfig /flushdns >nul 2>&1
echo [+] DNS temizlendi
echo [%date% %time%] [OK] DNS cache temizlendi >> "!LOGFILE!"

echo [*] Firewall kurali ekleniyor...
netsh advfirewall firewall add rule name="Zapret" dir=in action=allow program="%~dp0bin\winws.exe" enable=yes >nul 2>&1
echo [+] Firewall kurali eklendi
echo [%date% %time%] [OK] Firewall kurali eklendi >> "!LOGFILE!"

echo.
echo [OK] Zapret basariyla kuruldu ve basladi!
echo [OK] PC acildiginda otomatik calisacak.
echo [INFO] Kapatmak icin: hizmet_kaldir.bat
echo [INFO] Sorun giderme icin bakiniz: kurulum.log
echo [%date% %time%] ========== KURULUM BASARI ILE TAMAMLANDI ========== >> "!LOGFILE!"

pause
