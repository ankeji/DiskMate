# DiskMate · C-Laufwerk-Reiniger für Windows

[简体中文](README.md) · [繁體中文](README.zh-TW.md) · [English](README.en.md) · [日本語](README.ja.md) · [한국어](README.ko.md) · **Deutsch** · [Français](README.fr.md) · [Español](README.es.md) · [Português](README.pt.md) · [Русский](README.ru.md)

[![build](https://github.com/ankeji/DiskMate/actions/workflows/build.yml/badge.svg)](https://github.com/ankeji/DiskMate/actions/workflows/build.yml)
[![release](https://github.com/ankeji/DiskMate/actions/workflows/release.yml/badge.svg)](https://github.com/ankeji/DiskMate/releases)
[![license](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

Aufräumtool für das C-Laufwerk unter Windows: **„Scan starten“ drücken → die bereinigbaren Posten werden einzeln aufgelistet → jeder zeigt Risiko und was das Löschen bewirkt → du wählst aus → bestätigen und bereinigen**.

Eine einzelne exe, kein Installer, keine zusätzliche Laufzeit (nur das mitgelieferte .NET Framework 4.x).

**[Neueste Version herunterladen](https://github.com/ankeji/DiskMate/releases/latest)** · **[Problem melden](https://github.com/ankeji/DiskMate/issues)**

![Startbildschirm](screenshot.png)

## Merkmale

- **Fasst die Festplatte nicht von selbst an.** Beim Start erscheint nur der Willkommensbildschirm; gelesen wird erst nach „Scan starten“ (oder F5).
- **Abbrechbarer Scan.** 13 Schritte mit Fortschritt; jederzeit abbrechbar, ohne Änderungen zu hinterlassen.
- **Drei Risikostufen.** Jeder Posten ist mit niedrig / mittel / hoch markiert, inklusive „was wird gelöscht“ und „was passiert danach“; vorausgewählt sind nur risikoarme Posten.
- **Vier Kennzahlen-Karten.** Gesamt, niedrig, mittel, hoch – Klick filtert.
- **Detailbereich rechts.** Auswirkung, Empfehlung, echte Pfade, „Ordner öffnen“.
- **Komplett selbst gezeichnete Oberfläche.** Keine Standard-Controls, keine Fremdbibliotheken – gleiches Aussehen bei jeder DPI.
- **10 Oberflächensprachen**, Standard ist vereinfachtes Chinesisch; Umschalten wirkt sofort und wird gespeichert.
- **Updates in der App.** Ein Klick unten links: prüfen → laden → prüfen (SHA-256) → ersetzen → neu starten.

## Seiten

| Seite | Inhalt |
|---|---|
| Bereinigungs-Check | 13 Schritte: temporäre Dateien, Update-Caches, Logs, Papierkorb, Ruhezustandsdatei, alte Treiber u. a. |
| Große Dateien | Durchsucht Profil- und typische Großdatei-Ordner, sortiert nach Größe |
| Protokoll | Vollständiges Scan-/Bereinigungsprotokoll, kopierbar |

## Sprachen

| Sprache | Code | Umfang |
|---|---|---|
| 简体中文 | `zh-CN` | Oberfläche + alle Posten (Standard) |
| 繁體中文 | `zh-TW` | Oberfläche + alle Posten |
| English | `en-US` | Oberfläche + alle Posten |
| 日本語 / 한국어 / Deutsch / Français / Español / Português / Русский | `de-DE` usw. | nur Oberfläche; Postentexte fallen auf Englisch zurück |

## Updates in der App

1. Unten links steht dauerhaft `v2.1.0 · Nach Updates suchen`; gibt es eine neuere Version, wird daraus ein blauer Knopf „Auf v2.1.x aktualisieren“.
2. Er zeigt Versionsvergleich und Änderungen; „Herunterladen und installieren“ startet den Download mit Fortschrittsbalken.
3. Danach wird die Datei per SHA-256 geprüft; ein kleines Batch-Skript tauscht die exe nach dem Beenden aus und startet die neue Version.

Zwei Quellen werden geprüft, die höhere Version gewinnt:

- `https://raw.githubusercontent.com/ankeji/DiskMate/main/version.txt`
- `https://api.github.com/repos/ankeji/DiskMate/releases/latest` (Fallback, ohne Anmeldung 60 Anfragen/Stunde/IP)

Release in einer Zeile: `powershell -ExecutionPolicy Bypass -File scripts\release.ps1 2.1.1 "Änderungen" "notes"` — der gepushte Tag lässt GitHub Actions bauen und veröffentlichen.

## Bauen

Es genügt das .NET Framework 4.x von Windows 10/11:

```bat
git clone https://github.com/ankeji/DiskMate.git
cd DiskMate
build.bat
```

`build.bat` nutzt das mitgelieferte `csc.exe`: erst `_build.exe` ohne Icon, damit `app.ico` erzeugen, dann `DiskMate.exe` mit Icon neu bauen. Kein Visual Studio, kein NuGet.

Selbsttest: `DiskMate.exe --selftest` (rendert alle Seiten, prüft Überlappungen und fehlende Übersetzungen).

## Struktur

```
DiskMate/
├── DiskMate.exe            Build-Ergebnis
├── build.bat               Build mit einem Klick
├── version.txt             Update-Manifest
├── src/                    Quellen (11 .cs + app.manifest)
├── .github/workflows/      CI und automatische Releases
├── scripts/                push.bat · release.ps1
└── README*.md              Dokumentation in 10 Sprachen
```

## Lizenz

MIT — siehe [LICENSE](LICENSE).