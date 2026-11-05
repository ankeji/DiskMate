# DiskMate · C 槽管家

[简体中文](README.md) · **繁體中文** · [English](README.en.md) · [日本語](README.ja.md) · [한국어](README.ko.md) · [Deutsch](README.de.md) · [Français](README.fr.md) · [Español](README.es.md) · [Português](README.pt.md) · [Русский](README.ru.md)

[![build](https://github.com/ankeji/DiskMate/actions/workflows/build.yml/badge.svg)](https://github.com/ankeji/DiskMate/actions/workflows/build.yml)
[![release](https://github.com/ankeji/DiskMate/actions/workflows/release.yml/badge.svg)](https://github.com/ankeji/DiskMate/releases)
[![license](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

Windows 桌面 C 槽清理工具：**按下「開始掃描」→ 逐項列出可清理內容 → 標註風險與刪除後果 → 自行勾選 → 確認後清理**。

單一 exe，免安裝、無需執行階段（只用系統內建 .NET Framework 4.x）。

**[下載最新版](https://github.com/ankeji/DiskMate/releases/latest)** · **[問題回報](https://github.com/ankeji/DiskMate/issues)**

![歡迎頁](screenshot.png)

## 特色

- **不會自動動磁碟**：開啟後停在歡迎頁，按下「開始掃描」（或 F5）才開始。
- **掃描可取消**：13 個步驟逐項統計並顯示目前進度，隨時可取消，取消不留任何修改。
- **風險分級**：每項標註低／中／高風險，寫清楚「會刪掉什麼」與「刪了會怎樣」，預設只勾低風險。
- **四張指標卡**：可清理總量、低／中／高風險，點卡片即篩選。
- **右側詳情面板**：刪除後果、建議、實際路徑，可一鍵開啟所在資料夾。
- **自繪介面**：無原生控制項、無第三方套件，深淺一致的圓角卡片與向量圖示。
- **10 種介面語言**，預設簡體中文，切換後立即生效並記住。
- **應用程式內更新**：左下角一鍵檢查 → 下載 → 校驗 → 替換 → 自動重啟。

## 三個頁面

| 頁面 | 內容 |
|---|---|
| 清理體檢 | 13 步掃描暫存檔、更新快取、日誌、資源回收桶、休眠檔、舊驅動等 |
| 大檔案 | 掃描使用者目錄與常見大檔目錄，依大小排序，方便搬移或刪除 |
| 日誌 | 掃描與清理的完整記錄，可複製 |

## 介面語言

| 語言 | 代碼 | 覆蓋範圍 |
|---|---|---|
| 簡體中文 | zh-CN | 介面 + 全部清理項文案（預設） |
| 繁體中文 | zh-TW | 介面 + 全部清理項文案 |
| English | en-US | 介面 + 全部清理項文案 |
| 日本語 / 한국어 / Deutsch / Français / Español / Português / Русский | ja-JP 等 | 介面文案；清理項名稱與說明回退英文 |

## 應用程式內更新

1. 左下角常駐 `v2.1.0 · 檢查更新`，有新版本時變成藍色「更新至 v2.1.x」。
2. 按下後顯示版本對比與更新內容，按下「下載並安裝」開始下載。
3. 下載完成後以 SHA-256 校驗，通過後啟動替換腳本：等程式結束 → 覆蓋 exe → 自動重啟新版。

更新來源（取版本較高者）：

- `https://raw.githubusercontent.com/ankeji/DiskMate/main/version.txt`
- `https://api.github.com/repos/ankeji/DiskMate/releases/latest`（備援，未登入限 60 次/小時/IP）

發版：`powershell -ExecutionPolicy Bypass -File scripts\release.ps1 2.1.1 "修正A" "Fixed A"`，推 tag 後由 GitHub Actions 自動建立 Release。

## 編譯

需要 Windows 內建的 .NET Framework 4.x（Win10/11 預設就有）：

```bat
git clone https://github.com/ankeji/DiskMate.git
cd DiskMate
build.bat
```

`build.bat` 呼叫系統內建的 `csc.exe`：先編出無圖示的 `_build.exe` → 產生 `app.ico` → 帶圖示重新編譯 `DiskMate.exe`。不需要 Visual Studio、不需要 NuGet。

自檢：`DiskMate.exe --selftest`（渲染各頁、檢查版面重疊、檢查多語言缺詞）。

## 目錄

```
DiskMate/
├── DiskMate.exe            編譯產物
├── build.bat               一鍵編譯
├── version.txt             更新清單
├── src/                    原始碼（11 個 .cs + app.manifest）
├── .github/workflows/      CI 與自動發版
├── scripts/                push.bat · release.ps1
└── README*.md              10 種語言說明
```

## 授權

MIT，見 [LICENSE](LICENSE)。