@echo off
chcp 65001 >nul
setlocal
cd /d "%~dp0"

rem =====================================================================
rem  Usage:
rem    build.bat            build BOTH (default)
rem    build.bat prod       prod only -> DiskMate.exe      (https://c.softinstall.top)
rem    build.bat dev        dev only  -> DiskMate-dev.exe  (http://127.0.0.1:8099)
rem    build.bat both /nopause
rem  Only the prod package is published to GitHub.
rem  NOTE: keep this file ASCII-only + CRLF, otherwise cmd mis-parses it.
rem =====================================================================

set "NOPAUSE="
set "TARGET=both"
for %%A in (%*) do (
  if /i "%%~A"=="/nopause" set "NOPAUSE=1"
  if /i "%%~A"=="prod"     set "TARGET=prod"
  if /i "%%~A"=="dev"      set "TARGET=dev"
  if /i "%%~A"=="both"     set "TARGET=both"
)

set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" (
  echo [ERROR] .NET Framework 4.x compiler not found.
  if not defined NOPAUSE pause
  exit /b 1
)

set "SRC=src\DiskMate.cs src\Controls.cs src\Dialogs.cs src\I18n.cs src\LangMore.cs src\Logo.cs src\Json.cs src\Update.cs src\UpdateDialog.cs src\FlatScroll.cs src\Pages.cs src\MainForm.cs src\Program.cs src\Donate.cs src\Contact.cs src\Api.cs src\Session.cs src\AccountDialog.cs src\WeChat.cs src\Announcement.cs src\FeatureGate.cs src\Feedback.cs src\FeedbackPage.cs"
set "REFS=/reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll"
set "FLAGS=/nologo /target:winexe /platform:anycpu /optimize+ /codepage:65001 /win32manifest:src\app.manifest"

rem a running exe locks the output file (csc -> CS0016)
set "FIND=%SystemRoot%\System32\find.exe"
tasklist /fi "imagename eq DiskMate.exe" 2>nul | "%FIND%" /i "DiskMate.exe" >nul
if not errorlevel 1 (
  echo [i] closing running DiskMate.exe ...
  taskkill /im DiskMate.exe /f >nul 2>nul
  taskkill /im DiskMate-dev.exe /f >nul 2>nul
  ping -n 2 127.0.0.1 >nul
)

echo [1/3] Building bootstrap (no icon) ...
"%CSC%" %FLAGS% /out:"_build.exe" %REFS% %SRC%
if errorlevel 1 (
  echo.
  echo [FAILED] build errors above.
  if not defined NOPAUSE pause
  exit /b 1
)

echo [2/3] Generating app.ico ...
"_build.exe" --makeicon "app.ico" >nul

set "ICONOPT=/win32icon:app.ico /resource:weixin.jpg /resource:zhifubao.jpg /resource:contact.jpg"

if /i "%TARGET%"=="dev"  goto build_dev
if /i "%TARGET%"=="prod" goto build_prod

echo [3/3] Building PROD (DiskMate.exe) ...
"%CSC%" %FLAGS% %ICONOPT% /out:"DiskMate.exe" %REFS% %SRC%
if errorlevel 1 goto failed

echo [3/3] Building DEV  (DiskMate-dev.exe) ...
"%CSC%" %FLAGS% %ICONOPT% /define:DISKMATE_DEV /out:"DiskMate-dev.exe" %REFS% %SRC%
if errorlevel 1 goto failed
goto done

:build_prod
echo [3/3] Building PROD (DiskMate.exe) ...
"%CSC%" %FLAGS% %ICONOPT% /out:"DiskMate.exe" %REFS% %SRC%
if errorlevel 1 goto failed
goto done

:build_dev
echo [3/3] Building DEV  (DiskMate-dev.exe) ...
"%CSC%" %FLAGS% %ICONOPT% /define:DISKMATE_DEV /out:"DiskMate-dev.exe" %REFS% %SRC%
if errorlevel 1 goto failed
goto done

:failed
echo.
echo [FAILED] build errors above.
if not defined NOPAUSE pause
exit /b 1

:done
del /q "_build.exe" 2>nul
echo.
if exist "DiskMate.exe" for %%F in (DiskMate.exe) do echo [OK] prod  %%F  %%~zF bytes  to https://c.softinstall.top
if exist "DiskMate-dev.exe" for %%F in (DiskMate-dev.exe) do echo [OK] dev   %%F  %%~zF bytes  to http://127.0.0.1:8099
echo.
echo Check: DiskMate.exe --buildinfo      ^(prints version / channel / api^)
echo Test:  DiskMate.exe --selftest
echo Icon:  DiskMate.exe --makeicon app.ico
if not defined NOPAUSE pause
exit /b 0
