@echo off
setlocal
cd /d "%~dp0"

set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" (
    echo [ERROR] csc.exe not found. .NET Framework 4 is required.
    pause
    exit /b 1
)

echo [*] Building ZapretTray.exe ...
"%CSC%" /nologo /target:winexe /optimize+ /platform:anycpu /codepage:65001 ^
    /win32manifest:"%~dp0app.manifest" ^
    /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll ^
    /out:"%~dp0..\ZapretTray.exe" ^
    "%~dp0ZapretTray.cs"

if errorlevel 1 (
    echo [ERROR] Build failed.
    pause
    exit /b 1
)

echo [OK] Built: %~dp0..\ZapretTray.exe
if /I not "%~1"=="--silent" pause
exit /b 0
