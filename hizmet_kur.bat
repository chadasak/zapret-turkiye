@echo off
setlocal enabledelayedexpansion
cd /d "%~dp0"

REM Allow --silent to skip interactive pauses (use: hizmet_kur.bat --silent)
set "SILENT=0"
if /I "%~1"=="--silent" set "SILENT=1"

REM Log dosya yolu (quoted at set to preserve spaces)
set "LOGFILE=%~dp0kurulum.log"

REM Yonetici kontrolu
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo [!] Yonetici olarak calistirin!
    echo [%date% %time%] [HATA] Yonetici haklari alinmadi >> "%LOGFILE%"
    if %SILENT% equ 0 pause
    exit /b 1
)

echo [*] Kurulum basliyor...
echo [%date% %time%] ========== KURULUM BASLADI ========== >> "%LOGFILE%"

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

echo [*] Gerekli dosyalar kontrol ediliyor...
if not exist "%~dp0bin\winws.exe" (
    echo [HATA] bin\winws.exe bulunamadi!
    echo [%date% %time%] [HATA] bin\winws.exe dosyasi bulunamadi - Kurulum iptal >> "%LOGFILE%"
    if %SILENT% equ 0 pause
    exit /b 1
)
echo [+] bin\winws.exe bulundu
echo [%date% %time%] [OK] bin\winws.exe bulundu >> "%LOGFILE%"

if not exist "%~dp0bin\WinDivert64.sys" (
    echo [HATA] bin\WinDivert64.sys bulunamadi!
    echo [%date% %time%] [HATA] bin\WinDivert64.sys dosyasi bulunamadi - Kurulum iptal >> "%LOGFILE%"
    if %SILENT% equ 0 pause
    exit /b 1
)
echo [+] bin\WinDivert64.sys bulundu
echo [%date% %time%] [OK] bin\WinDivert64.sys bulundu >> "%LOGFILE%"

if not exist "%~dp0zapret_gorev.cmd" (
    echo [HATA] zapret_gorev.cmd bulunamadi!
    echo [%date% %time%] [HATA] zapret_gorev.cmd dosyasi bulunamadi - Kurulum iptal >> "%LOGFILE%"
    if %SILENT% equ 0 pause
    exit /b 1
)
echo [+] zapret_gorev.cmd bulundu
echo [%date% %time%] [OK] zapret_gorev.cmd bulundu >> "%LOGFILE%"

echo [*] Web kaynakli dosya engeli temizleniyor...
powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference='Stop'; try { Get-ChildItem -LiteralPath '%~dp0' -Recurse -File | Unblock-File; exit 0 } catch { exit 1 }" >nul 2>&1
if %errorlevel% equ 0 (
    echo [+] Dosya engeli temizlendi
    echo [%date% %time%] [OK] MOTW temizligi tamamlandi >> "%LOGFILE%"
) else (
    echo [INFO] Dosya engeli temizlenemedi (politika kisiti olabilir)
    echo [%date% %time%] [INFO] MOTW temizligi atlandi >> "%LOGFILE%"
)

echo [*] Defender istisnalari kontrol ediliyor...
powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference='Stop'; if (-not (Get-Command Add-MpPreference -ErrorAction SilentlyContinue)) { exit 2 }; $folder=[IO.Path]::GetFullPath('%~dp0').TrimEnd('\'); Add-MpPreference -ExclusionPath $folder -ErrorAction Stop; Add-MpPreference -ExclusionProcess 'winws.exe' -ErrorAction SilentlyContinue; exit 0" >nul 2>&1
set "LAST_ERR=%errorlevel%"
if %LAST_ERR% equ 0 (
    echo [+] Defender istisnalari eklendi
    echo [%date% %time%] [OK] Defender istisnalari eklendi >> "%LOGFILE%"
) else (
    if %LAST_ERR% equ 2 (
        echo [INFO] Defender cmdlet bulunamadi, istisna atlandi
        echo [%date% %time%] [INFO] Defender cmdlet bulunamadi >> "%LOGFILE%"
    ) else (
        echo [INFO] Defender istisnasi eklenemedi (izin/politika kisiti olabilir)
        echo [%date% %time%] [INFO] Defender istisnasi eklenemedi >> "%LOGFILE%"
    )
)

echo [*] Eski gorev kontrol ediliyor ve kaldiriliyorsa...
schtasks /delete /tn "ZapretDPI" /f >nul 2>&1
echo [+] Eski gorev temizlendi
echo [%date% %time%] [OK] Eski gorev temizlendi >> "%LOGFILE%"

echo [*] Eski calisan winws.exe kapatiliyor...
taskkill /f /im winws.exe >nul 2>&1
echo [+] Kapandi
echo [%date% %time%] [OK] Calisan processler kapatildi >> "%LOGFILE%"

echo [*] Gorev zamanlayicida olusturuluyor...
set "TASKCMD=%~dp0zapret_gorev.cmd"
:: Quote the /tr argument explicitly to avoid nested-quote problems when path contains spaces
schtasks /create /tn "ZapretDPI" /tr "\"%TASKCMD%\"" /sc onlogon /rl highest /ru "SYSTEM" /f >nul 2>&1
set "LAST_ERR=%errorlevel%"
if %LAST_ERR% neq 0 (
    echo [HATA] Gorev olusturulamadi! Kod: %LAST_ERR%
    echo [%date% %time%] [HATA] Gorev olusturulamadi! Kod: %LAST_ERR% >> "%LOGFILE%"
    if %SILENT% equ 0 pause
    exit /b 1
)
echo [+] Gorev olusturuldu
echo [%date% %time%] [OK] Zamanlayici gorevi olusturuldu >> "%LOGFILE%"

echo [*] Zapret servis baslatiliyor...
schtasks /run /tn "ZapretDPI" >nul 2>&1
set "LAST_ERR=%errorlevel%"
if %LAST_ERR% neq 0 (
    echo [HATA] Gorev calistirilamadi! Kod: %LAST_ERR%
    echo [%date% %time%] [HATA] Zamanlayici gorevi calistirilmadi! Kod: %LAST_ERR% >> "%LOGFILE%"
    if %SILENT% equ 0 pause
    exit /b 1
)
echo [+] Servis baslatildi
echo [%date% %time%] [OK] Zapret servisi baslatildi >> "%LOGFILE%"

echo [*] DNS cache temizleniyor...
ipconfig /flushdns >nul 2>&1
echo [+] DNS temizlendi
echo [%date% %time%] [OK] DNS cache temizlendi >> "%LOGFILE%"

echo [*] Firewall kurali temizleniyor...
netsh advfirewall firewall delete rule name="Zapret" program="%~dp0bin\winws.exe" >nul 2>&1
set "LAST_ERR=%errorlevel%"
if %LAST_ERR% equ 0 (
    echo [+] Eski firewall kurali temizlendi
    echo [%date% %time%] [OK] Eski firewall kurali temizlendi >> "%LOGFILE%"
) else (
    echo [INFO] Eski firewall kurali yoktu
    echo [%date% %time%] [INFO] Eski firewall kurali mevcut degildi >> "%LOGFILE%"
)

echo [*] Firewall kurali ekleniyor...
netsh advfirewall firewall add rule name="Zapret" dir=in action=allow program="%~dp0bin\winws.exe" enable=yes >nul 2>&1
set "LAST_ERR=%errorlevel%"
if %LAST_ERR% neq 0 (
    echo [HATA] Firewall kurali eklenemedi! Kod: %LAST_ERR%
    echo [%date% %time%] [HATA] Firewall kurali eklenemedi! Kod: %LAST_ERR% >> "%LOGFILE%"
    if %SILENT% equ 0 pause
    exit /b 1
)
echo [+] Firewall kurali eklendi
echo [%date% %time%] [OK] Firewall kurali eklendi >> "%LOGFILE%"

echo.
echo [OK] Zapret basariyla kuruldu ve basladi!
echo [OK] PC acildiginda otomatik calisacak.
echo [INFO] Kapatmak icin: hizmet_kaldir.bat
echo [INFO] Sorun giderme icin bakiniz: kurulum.log
echo [%date% %time%] ========== KURULUM BASARI ILE TAMAMLANDI ========== >> "%LOGFILE%"

if %SILENT% equ 0 (
    pause
)

exit /b 0
