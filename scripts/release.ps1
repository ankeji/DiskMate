<#
  发版脚本：改版本号 -> 编译 -> 提交 -> 打 tag -> 推送
  推送成功后 GitHub Actions 会自动编译并创建 Release（.github/workflows/release.yml）

  用法：
    powershell -ExecutionPolicy Bypass -File scripts\release.ps1 2.1.1 "修复xx" "Fixed xx"
#>
param(
    [Parameter(Mandatory = $true)][string]$Version,
    [string]$NotesZh = "",
    [string]$NotesEn = ""
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

if ($Version -notmatch '^\d+\.\d+(\.\d+)?$') { throw "版本号要写成 2.1.1 这种形式" }
if ($Version -notmatch '\.\d+$') { $Version = "$Version.0" }

# 1. 改版本号
$update = Join-Path $root 'src\Update.cs'
$text = [IO.File]::ReadAllText($update, [Text.Encoding]::UTF8)
$text = [regex]::Replace($text, 'public const string Version = "[^"]+";', "public const string Version = `"$Version`";")
[IO.File]::WriteAllText($update, $text, (New-Object Text.UTF8Encoding($false)))
Write-Host "[1/5] version = $Version"

# 2. 编译 + 自检
cmd /c "build.bat /nopause"
if ($LASTEXITCODE -ne 0) { throw "build failed" }
& ".\DiskMate.exe" --selftest
if ($LASTEXITCODE -ne 0) { throw "selftest failed" }
Write-Host "[2/5] build + selftest ok"

# 3. 生成本地 version.txt（指向本 tag 的 Release 资产）
& ".\DiskMate.exe" "--writeversion=version.txt|https://github.com/ankeji/DiskMate/releases/download/v$Version/DiskMate.exe|$NotesZh|$NotesEn"
Write-Host "[3/5] version.txt generated"

# 4. 提交
git add -A
git commit -m "release: v$Version"
Write-Host "[4/5] committed"

# 5. tag + push（tag 触发 Actions 发版）
git tag "v$Version"
git push origin main
git push origin "v$Version"
Write-Host "[5/5] pushed v$Version -> https://github.com/ankeji/DiskMate/actions"