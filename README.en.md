# DiskMate · Windows C: drive cleaner

[简体中文](README.md) · [繁體中文](README.zh-TW.md) · **English** · [日本語](README.ja.md) · [한국어](README.ko.md) · [Deutsch](README.de.md) · [Français](README.fr.md) · [Español](README.es.md) · [Português](README.pt.md) · [Русский](README.ru.md)

[![build](https://github.com/ankeji/DiskMate/actions/workflows/build.yml/badge.svg)](https://github.com/ankeji/DiskMate/actions/workflows/build.yml)
[![release](https://github.com/ankeji/DiskMate/actions/workflows/release.yml/badge.svg)](https://github.com/ankeji/DiskMate/releases)
[![license](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

A Windows desktop cleaner for the C: drive: **press “Start scan” → items are listed one by one → each one shows its risk and what deleting it really does → you tick what you want → confirm and clean**.

Single exe, no installer, no runtime to download (uses the built-in .NET Framework 4.x).

**[Download latest](https://github.com/ankeji/DiskMate/releases/latest)** · **[Report an issue](https://github.com/ankeji/DiskMate/issues)**

![Welcome screen](screenshot.png)

## Highlights

- **It never touches your disk on its own.** The app opens on a welcome screen; nothing is scanned until you press “Start scan” (or F5).
- **Cancellable scan.** 13 steps, each one reports what it is counting; cancel any time and nothing is changed.
- **Risk levels.** Every item is tagged low / medium / high with a plain explanation of what gets deleted and what happens afterwards; only low-risk items are pre-selected.
- **Four metric cards.** Total cleanable, low, medium, high — click a card to filter.
- **Detail panel.** Impact, advice and real paths for the selected item, plus “Open containing folder”.
- **Fully custom-drawn UI.** No native controls, no third-party packages — one hand-drawn look at any DPI.
- **10 interface languages**, Simplified Chinese by default, switched instantly and remembered.
- **In-app updates.** One click at the bottom left: check → download → verify → swap → relaunch.

## Pages

| Page | Contents |
|---|---|
| Clean check | 13 steps covering temp files, update caches, logs, recycle bin, hibernation file, old drivers and more |
| Large files | Scans your profile and the usual big-file folders, sorted by size |
| Log | Full scan/clean log, copyable |

## Interface languages

| Language | Code | Coverage |
|---|---|---|
| 简体中文 | `zh-CN` | UI + every item text (default) |
| 繁體中文 | `zh-TW` | UI + every item text |
| English | `en-US` | UI + every item text |
| 日本語 / 한국어 / Deutsch / Français / Español / Português / Русский | `ja-JP` etc. | UI text; item names and advice fall back to English |

## In-app updates

1. The bottom-left button reads `v2.1.0 · Check for updates` and turns blue (“Update to v2.1.x”) when a newer build exists.
2. It shows the version comparison and release notes; “Download and install” starts the download with a progress bar.
3. When the download finishes it is verified with SHA-256, then a small batch script swaps the exe after the app exits and relaunches it.

Two sources are checked and the higher version wins:

- `https://raw.githubusercontent.com/ankeji/DiskMate/main/version.txt`
- `https://api.github.com/repos/ankeji/DiskMate/releases/latest` (fallback, 60 requests/hour/IP unauthenticated)

Release with one command: `powershell -ExecutionPolicy Bypass -File scripts\release.ps1 2.1.1 "notes" "notes in English"` — pushing the tag makes GitHub Actions build and publish the release.

## Build

Requires the .NET Framework 4.x that ships with Windows 10/11:

```bat
git clone https://github.com/ankeji/DiskMate.git
cd DiskMate
build.bat
```

`build.bat` calls the bundled `csc.exe`: builds an icon-less `_build.exe`, uses it to generate `app.ico`, then recompiles `DiskMate.exe` with the icon embedded. No Visual Studio, no NuGet.

Self test: `DiskMate.exe --selftest` (renders every page, checks layout overlaps and missing translations).

## Layout

```
DiskMate/
├── DiskMate.exe            build output
├── build.bat               one-click build
├── version.txt             update manifest
├── src/                    sources (11 .cs files + app.manifest)
├── .github/workflows/      CI and automatic releases
├── scripts/                push.bat · release.ps1
└── README*.md              docs in 10 languages
```

## Support this project

DiskMate is a free personal project: no ads, no bundled software, no telemetry. If it freed up a few dozen gigabytes for you, you can buy me a coffee:

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/T1S42A3IBF)

## License

MIT — see [LICENSE](LICENSE).