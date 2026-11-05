# 删除除当前版本以外的历史 Release 和 tag（需要带 repo 权限的 token）
# 用法：powershell -ExecutionPolicy Bypass -File scripts\cleanup-releases.ps1 -Token ghp_xxx [-Keep v1.0.1] [-DryRun]
param(
    [Parameter(Mandatory = $true)][string]$Token,
    [string]$Repo = "ankeji/DiskMate",
    [string]$Keep = "v1.0.1",
    [switch]$DryRun
)

$headers = @{
    Authorization = "token $Token"
    "User-Agent"  = "DiskMate-cleanup"
    Accept        = "application/vnd.github+json"
}

function Invoke-Api($method, $url) {
    return Invoke-RestMethod -Method $method -Uri $url -Headers $headers -TimeoutSec 30
}

$releases = Invoke-Api GET "https://api.github.com/repos/$Repo/releases?per_page=100"
foreach ($r in $releases) {
    if ($r.tag_name -eq $Keep) { Write-Host "keep release $($r.tag_name)"; continue }
    Write-Host "delete release $($r.tag_name) (id=$($r.id))"
    if (-not $DryRun) {
        Invoke-Api DELETE "https://api.github.com/repos/$Repo/releases/$($r.id)" | Out-Null
    }
}

$refs = Invoke-Api GET "https://api.github.com/repos/$Repo/git/refs/tags"
foreach ($ref in $refs) {
    if ($ref.ref -eq "refs/tags/$Keep") { Write-Host "keep tag $Keep"; continue }
    Write-Host "delete tag $($ref.ref)"
    if (-not $DryRun) {
        Invoke-Api DELETE "https://api.github.com/repos/$Repo/git/refs/$($ref.ref)" | Out-Null
    }
}

Write-Host "done. keep=$Keep dryRun=$DryRun"