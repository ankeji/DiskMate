# DiskMate · C ドライブ クリーナー

[简体中文](README.md) · [繁體中文](README.zh-TW.md) · [English](README.en.md) · **日本語** · [한국어](README.ko.md) · [Deutsch](README.de.md) · [Français](README.fr.md) · [Español](README.es.md) · [Português](README.pt.md) · [Русский](README.ru.md)

[![build](https://github.com/ankeji/DiskMate/actions/workflows/build.yml/badge.svg)](https://github.com/ankeji/DiskMate/actions/workflows/build.yml)
[![release](https://github.com/ankeji/DiskMate/actions/workflows/release.yml/badge.svg)](https://github.com/ankeji/DiskMate/releases)
[![license](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

Windows 用の C ドライブ クリーナーです。**「スキャン開始」を押す → 削除できる項目を一覧表示 → リスクと「削除するとどうなるか」を明記 → 自分でチェック → 確認してから削除**、という流れだけを行います。

単一 exe、インストール不要、追加ランタイム不要（Windows 標準の .NET Framework 4.x のみ）。

**[最新版をダウンロード](https://github.com/ankeji/DiskMate/releases/latest)** · **[問題を報告](https://github.com/ankeji/DiskMate/issues)**

![ようこそ画面](screenshot.png)

## 特長

- **勝手にディスクを触りません。** 起動時はようこそ画面のまま。「スキャン開始」（または F5）を押すまで何も読みません。
- **スキャンはキャンセル可能。** 13 ステップで進捗を表示し、いつでも中止できます。中止しても変更は残りません。
- **リスク 3 段階。** 各項目に低・中・高を付け、「何が消えるか」「消した後どうなるか」「おすすめ」を表示。初期状態では低リスクのみ選択。
- **4 つの指標カード。** 合計・低・中・高。カードを押すとそのリスクで絞り込み。
- **右側の詳細パネル。** 影響・助言・実際のパスを表示し、「フォルダーを開く」も可能。
- **完全自前描画 UI。** 標準コントロールも外部ライブラリも使わず、どの DPI でも同じ見た目。
- **10 言語対応**（既定は簡体中文）。切り替えは即時反映され、次回起動時も維持。
- **アプリ内アップデート。** 左下のボタンひとつで 確認 → ダウンロード → 検証 → 置換 → 再起動。

## ページ

| ページ | 内容 |
|---|---|
| クリーン診断 | 一時ファイル、更新キャッシュ、ログ、ごみ箱、休止状態ファイル、古いドライバーなど 13 ステップ |
| 大きいファイル | ユーザーフォルダーと定番の大容量フォルダーを走査し、サイズ順に表示 |
| ログ | スキャンと削除の全記録（コピー可能） |

## 表示言語

| 言語 | コード | 範囲 |
|---|---|---|
| 简体中文 | `zh-CN` | UI + 全項目の文言（既定） |
| 繁體中文 | `zh-TW` | UI + 全項目の文言 |
| English | `en-US` | UI + 全項目の文言 |
| 日本語 / 한국어 / Deutsch / Français / Español / Português / Русский | `ja-JP` など | UI 文言のみ（項目名と説明は英語にフォールバック） |

## アプリ内アップデート

1. 左下に `v2.1.0 · アップデートを確認` を常時表示。新しい版があると青い「v2.1.x に更新」に変わります。
2. 押すとバージョン比較と更新内容を表示し、「ダウンロードしてインストール」でダウンロード開始（進捗バー付き）。
3. 完了後に SHA-256 で検証し、問題なければ置換スクリプトを起動します。アプリ終了後に exe を差し替え、自動で新バージョンを起動します。

確認する配布元（バージョンが高い方を採用）:

- `https://raw.githubusercontent.com/ankeji/DiskMate/main/version.txt`
- `https://api.github.com/repos/ankeji/DiskMate/releases/latest`（予備。未認証は 60 回/時/IP）

リリースは 1 コマンド: `powershell -ExecutionPolicy Bypass -File scripts\release.ps1 2.1.1 "修正内容" "notes"`。タグを push すると GitHub Actions がビルドして Release を作成します。

## ビルド

Windows 10/11 標準の .NET Framework 4.x があれば十分です。

```bat
git clone https://github.com/ankeji/DiskMate.git
cd DiskMate
build.bat
```

`build.bat` は標準の `csc.exe` を使います。アイコン無しの `_build.exe` をビルド → `app.ico` を生成 → アイコン付きで `DiskMate.exe` を再ビルド。Visual Studio も NuGet も不要です。

セルフテスト: `DiskMate.exe --selftest`（各ページの描画・重なり・翻訳漏れを検査）。

## 構成

```
DiskMate/
├── DiskMate.exe            ビルド成果物
├── build.bat               ワンクリックビルド
├── version.txt             更新マニフェスト
├── src/                    ソース（.cs 11 個 + app.manifest）
├── .github/workflows/      CI と自動リリース
├── scripts/                push.bat · release.ps1
└── README*.md              10 言語のドキュメント
```

## サポート

DiskMate は個人開発の無料ツールです（広告なし・同梱ソフトなし・データ収集なし）。何十 GB か空いたら、コーヒーを一杯おごっていただけると励みになります:

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/T1S42A3IBF)

同じ入口はアプリ内にもあります（左下の「サポート」）。中国語 UI では Alipay / WeChat、それ以外は Ko-fi。

## ライセンス

MIT（[LICENSE](LICENSE) 参照）。