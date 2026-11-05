@echo off
chcp 65001 >nul
setlocal
cd /d "%~dp0.."

rem ============================================================
rem 首次推送 / 日常推送都跑这个脚本
rem 如果 git 连不上 github（国内常见），先开代理再执行：
rem   git config --global http.proxy  http://127.0.0.1:7890
rem   git config --global https.proxy http://127.0.0.1:7890
rem 取消代理：
rem   git config --global --unset http.proxy
rem ============================================================

if not exist ".git" (
  git init
  git branch -M main
)

git remote remove origin 2>nul
git remote add origin https://github.com/ankeji/DiskMate.git

git add -A
git diff --staged --quiet || git commit -m "chore: update workspace"

echo.
echo [push] git push -u origin main
git push -u origin main
if errorlevel 1 (
  echo.
  echo [FAILED] push failed. Check network / proxy / credentials.
  pause
  exit /b 1
)

echo.
echo [OK] pushed to https://github.com/ankeji/DiskMate
pause