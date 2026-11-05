<#
  一键发版：改版本号 -> 编译 -> 自检 -> 提交 -> 打带说明的 tag -> 推送
  推送 tag 后 GitHub Actions（.github/workflows/release.yml）自动：
    编译 -> 自检 -> 用标签说明生成 version.txt -> 创建 Release（exe + version.txt）-> 把 version.txt 提交回 main

  用法：
    powershell -ExecutionPolicy Bypass -File scripts\release.ps1 2.1.1 "中文说明（可多行）" "English notes"

  若只想手工发版：
    git tag -a v2.1.1 -F docs\release-notes-v2.1.1.md
    git push origin v2.1.1
#>
param(
    [Parameter(Mandatory = $true)][string]$Version,
    [string]$NotesZh = "",
    [string]$NotesEn = ""
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

if ($Version -notmatch '^\d+\.\d+') { throw "版本号要写成 2.1.1 这种形式" }
if ($Version -notmatch '^\d+\.\d+\.\d+$') { $Version = "$Version.0" }

# 1. 改版本号
$update = Join-Path $root 'src\Update.cs'
$text = [IO.File]::ReadAllText($update, [Text.Encoding]::UTF8)
$text = [regex]::Replace($text, 'public const string Version = "[^"]+";', "public const string Version = `"$Version`";")
[IO.File]::WriteAllText($update, $text, (New-Object Text.UTF8Encoding($false)))
Write-Host "[1/6] version = $Version" -ForegroundColor Cyan

# 2. 编译 + 自检
cmd /c "build.bat /nopause"
if ($LASTEXITCODE -ne 0) { throw "build failed" }
& ".\DiskMate.exe" --selftest
if ($LASTEXITCODE -ne 0) { throw "selftest failed" }
Write-Host "[2/6] build + selftest ok" -ForegroundColor Cyan

# 3. 写发版说明（同时作为 tag 说明和 Release 正文）
if (-not $NotesZh) { $NotesZh = "见仓库提交记录 / see commit history" }
if (-not $NotesEn) { $NotesEn = $NotesZh }
$notesPath = Join-Path $root "docs\release-notes-v$Version.md"
$notesBody = "## DiskMate v$Version`r`n`r`n### 中文`r`n$NotesZh`r`n`r`n### English`r`n$NotesEn`r`n"
[IO.File]::WriteAllText($notesPath, $notesBody, (New-Object Text.UTF8Encoding($false)))
Write-Host "[3/6] notes -> docs\release-notes-v$Version.md" -ForegroundColor Cyan

# 4. 提交
git add -A
git commit -m "release: v$Version" | Out-Null
Write-Host "[4/6] committed" -ForegroundColor Cyan

# 5. 打带说明的 tag
git tag -a "v$Version" -F $notesPath
Write-Host "[5/6] tag v$Version created" -ForegroundColor Cyan

# 6. 推送（main + tag）
git push origin main
git push origin "v$Version"
Write-Host "[6/6] pushed -> https://github.com/ankeji/DiskMate/actions" -ForegroundColor Green