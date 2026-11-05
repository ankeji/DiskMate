# GitHub 仓库与发版配置说明

这个仓库已经配好三件事：**推送**、**CI 编译**、**打 tag 自动发版（含应用内更新清单）**。

## 1. 目录里都配了什么

```
.github/workflows/build.yml      push / PR 时编译 + 自检 + 上传产物
.github/workflows/release.yml    推 v* tag 时编译 + 自检 + 生成 version.txt + 建 Release + 回写 version.txt
scripts/push.bat                 首次/日常推送（自动建 origin、提交、push）
scripts/release.ps1              改版本号 → 编译 → 自检 → 提交 → 打带说明的 tag → 推送
version.txt                      应用内更新清单（CI 会自动覆盖成正式版数据）
docs/release-notes-*.md          发版说明（tag 正文 + Release 正文）
.gitattributes                   行尾规范（bat/cs 用 CRLF，md/yml 用 LF）
.gitignore                       忽略 exe、app.ico、update-cache、settings.ini 等
```

## 2. 首次推送

```bat
scripts\push.bat
```

等价于：

```bat
git remote add origin https://github.com/ankeji/DiskMate.git
git branch -M main
git push -u origin main
```

> 推送时如果要输账号密码：GitHub 现在只认 **Personal Access Token**（Settings → Developer settings → Tokens，勾 `repo`），密码处粘贴 token。
> Windows 可以把 token 存进凭据管理器：`git config --global credential.helper manager`。

**网络问题**：本机 git 走的是 `http.proxy=http://127.0.0.1:7892`，代理没开时会报
`Failed to connect to 127.0.0.1 port 7892` 或 `OpenSSL SSL_connect: SSL_ERROR_SYSCALL`。开代理后立刻恢复；不想用代理就：

```bat
git config --global --unset http.proxy
git config --global --unset https.proxy
```

## 3. 发版（推荐流程）

```bat
powershell -ExecutionPolicy Bypass -File scripts\release.ps1 2.1.1 "修复窄窗口布局" "Fixed narrow layout"
```

它做的事：

1. 把 `src/Update.cs` 的 `AppInfo.Version` 改成 `2.1.1`
2. `build.bat /nopause` 编译，`DiskMate.exe --selftest` 自检
3. 生成 `docs/release-notes-v2.1.1.md`
4. 提交 + 打**带说明的** tag `v2.1.1`
5. 推送 main 和 tag

推送后 GitHub Actions 会：

1. `build.bat /nopause` 编译（windows-latest 自带 .NET Framework）
2. `DiskMate.exe --selftest`
3. `git tag -l --format="%(contents)" v2.1.1 > release-notes.txt`
4. `DiskMate.exe "--writeversion=version.txt|<Release 资产地址>|@release-notes.txt"`
5. 创建 Release，附 `DiskMate.exe` 和 `version.txt`
6. 把新的 `version.txt` 提交回 `main`（应用内更新读的就是这个文件）

> **为什么参数要加引号**：`--writeversion` 用 `|` 分隔字段，而 `cmd` 把 `|` 当管道符，不加引号命令会被截断（曾经出现过 `url=` 空值就是这个原因）。

## 4. 验证发版是否成功

```bat
:: 1. 看版本清单（应含 url= 和 sha256=）
curl https://raw.githubusercontent.com/ankeji/DiskMate/main/version.txt

:: 2. 看 Release 是否带资产
curl -I https://github.com/ankeji/DiskMate/releases/download/v2.1.1/DiskMate.exe

:: 3. 用老版本程序自测（应提示有新版本）
DiskMate.exe --checkupdate
```

## 5. 应用内更新怎么找到新版本

程序按顺序查两条源，取版本号更大的那个：

| 顺序 | 地址 | 说明 |
|---|---|---|
| 1 | `https://raw.githubusercontent.com/ankeji/DiskMate/main/version.txt` | 无频率限制，正常发版走这条 |
| 2 | `https://api.github.com/repos/ankeji/DiskMate/releases/latest` | 兜底 + 提供 Release 正文作为更新说明；未登录 60 次/小时/IP |

两条都访问失败才提示「检查更新失败」；只要有一条通，就按「已是最新 / 有新版本」正常提示。

## 6. 常见问题

| 现象 | 原因 / 处理 |
|---|---|
| Actions 一直排队 | 免费额度并发限制，等 1–3 分钟；也可以 `git push -f origin vX.Y.Z` 重跑 |
| Release 里没有 exe | 编译步骤失败，看 Actions 日志里 `[FAILED]` 那一段 |
| `version.txt` 里 `url=` 是空的 | `--writeversion` 参数没加引号，cmd 把 `\|` 当管道（本项目已修） |
| 老版本点更新提示「检查更新失败」 | `sha256` 校验不过（文件被改动）或两条源都访问不到；`--checkupdate` 看具体报错 |
| 更新后还是老版本 | 目标 exe 在只读目录或被占用；下载目录里会留下 `*.exe.failed` |
| 推送报 403 | token 过期或没有 `repo` 权限 |