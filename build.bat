@echo off
chcp 65001 >nul
setlocal
cd /d "%~dp0"
set "NOPAUSE="
if /i "%~1"=="/nopause" set "NOPAUSE=1"

set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" (
  echo [ERROR] csc.exe not found - .NET Framework 4.x is required.
  if not defined NOPAUSE pause
  exit /b 1
)

echo [1/3] Building bootstrap (no icon) ...
"%CSC%" /nologo /target:winexe /platform:anycpu /optimize+ /codepage:65001 ^
  /win32manifest:"src\app.manifest" ^
  /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll ^
  /out:"_build.exe" ^
  "src\DiskMate.cs" "src\Controls.cs" "src\Dialogs.cs" "src\I18n.cs" "src\LangMore.cs" "src\Logo.cs" "src\Json.cs" "src\Update.cs" "src\UpdateDialog.cs" "src\FlatScroll.cs" ^
  "src\Pages.cs" "src\MainForm.cs" "src\Program.cs" "src\Donate.cs" "src\Contact.cs" ^
  "src\Api.cs" "src\Session.cs" "src\AccountDialog.cs" "src\WeChat.cs"

if errorlevel 1 (
  echo.
  echo [FAILED] build errors above.
  if not defined NOPAUSE pause
  exit /b 1
)

echo [2/3] Generating app.ico ...
"_build.exe" --makeicon "app.ico"

echo [3/3] Building DiskMate.exe with icon ...
"%CSC%" /nologo /target:winexe /platform:anycpu /optimize+ /codepage:65001 ^
  /win32manifest:"src\app.manifest" /win32icon:"app.ico" ^
  /resource:"weixin.jpg" /resource:"zhifubao.jpg" /resource:"contact.jpg" ^
  /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll ^
  /out:"DiskMate.exe" ^
  "src\DiskMate.cs" "src\Controls.cs" "src\Dialogs.cs" "src\I18n.cs" "src\LangMore.cs" "src\Logo.cs" "src\Json.cs" "src\Update.cs" "src\UpdateDialog.cs" "src\FlatScroll.cs" ^
  "src\Pages.cs" "src\MainForm.cs" "src\Program.cs" "src\Donate.cs" "src\Contact.cs" ^
  "src\Api.cs" "src\Session.cs" "src\AccountDialog.cs" "src\WeChat.cs"

if errorlevel 1 (
  echo.
  echo [FAILED] build errors above.
  if not defined NOPAUSE pause
  exit /b 1
)

del /q "_build.exe" 2>nul

echo.
echo [OK] DiskMate.exe built: %CD%\DiskMate.exe
echo.
echo Run:   DiskMate.exe          (GUI)
echo Test:  DiskMate.exe --selftest
echo Icon:  DiskMate.exe --makeicon app.ico
if not defined NOPAUSE pause
exit /b 0
