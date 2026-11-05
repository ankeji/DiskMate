# DiskMate · Nettoyeur de disque C: pour Windows

[简体中文](README.md) · [繁體中文](README.zh-TW.md) · [English](README.en.md) · [日本語](README.ja.md) · [한국어](README.ko.md) · [Deutsch](README.de.md) · **Français** · [Español](README.es.md) · [Português](README.pt.md) · [Русский](README.ru.md)

[![build](https://github.com/ankeji/DiskMate/actions/workflows/build.yml/badge.svg)](https://github.com/ankeji/DiskMate/actions/workflows/build.yml)
[![release](https://github.com/ankeji/DiskMate/actions/workflows/release.yml/badge.svg)](https://github.com/ankeji/DiskMate/releases)
[![license](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

Outil de nettoyage du disque C: sous Windows : **cliquez sur « Lancer l'analyse » → les éléments nettoyables sont listés un par un → chacun indique son risque et ce que la suppression implique → vous cochez → confirmation puis nettoyage**.

Un seul exe, sans installation ni runtime supplémentaire (uniquement le .NET Framework 4.x fourni par Windows).

**[Télécharger la dernière version](https://github.com/ankeji/DiskMate/releases/latest)** · **[Signaler un problème](https://github.com/ankeji/DiskMate/issues)**

![Écran d'accueil](screenshot.png)

## Points clés

- **Aucune lecture du disque au démarrage.** L'application reste sur l'écran d'accueil ; rien n'est analysé avant « Lancer l'analyse » (ou F5).
- **Analyse annulable.** 13 étapes avec progression ; l'annulation ne laisse aucune modification.
- **Trois niveaux de risque.** Chaque élément est marqué faible / moyen / élevé avec « ce qui est supprimé » et « ce qui se passe ensuite » ; seuls les risques faibles sont présélectionnés.
- **Quatre cartes d'indicateurs.** Total, faible, moyen, élevé — un clic filtre.
- **Panneau de détail.** Impact, conseil, chemins réels et « Ouvrir le dossier ».
- **Interface entièrement dessinée à la main.** Aucun contrôle natif, aucune bibliothèque tierce, rendu identique à tous les DPI.
- **10 langues d'interface**, chinois simplifié par défaut, changement immédiat et mémorisé.
- **Mise à jour intégrée.** Un clic en bas à gauche : vérifier → télécharger → vérifier (SHA-256) → remplacer → relancer.

## Pages

| Page | Contenu |
|---|---|
| Bilan de nettoyage | 13 étapes : fichiers temporaires, caches de mise à jour, journaux, corbeille, fichier d'hibernation, anciens pilotes, etc. |
| Gros fichiers | Analyse le profil et les dossiers volumineux, triés par taille |
| Journal | Journal complet des analyses et nettoyages, copiable |

## Langues

| Langue | Code | Couverture |
|---|---|---|
| 简体中文 | `zh-CN` | interface + tous les éléments (par défaut) |
| 繁體中文 | `zh-TW` | interface + tous les éléments |
| English | `en-US` | interface + tous les éléments |
| 日本語 / 한국어 / Deutsch / Français / Español / Português / Русский | `fr-FR` etc. | interface seulement ; textes des éléments en anglais |

## Mise à jour intégrée

1. En bas à gauche : `v2.1.0 · Vérifier les mises à jour` ; une version plus récente transforme le bouton en « Mettre à jour vers v2.1.x ».
2. La fenêtre affiche la comparaison de versions et les notes ; « Télécharger et installer » lance le téléchargement avec barre de progression.
3. Le fichier est vérifié en SHA-256, puis un petit script batch remplace l'exe après la fermeture et relance la nouvelle version.

Deux sources sont consultées, la version la plus haute gagne :

- `https://raw.githubusercontent.com/ankeji/DiskMate/main/version.txt`
- `https://api.github.com/repos/ankeji/DiskMate/releases/latest` (secours, 60 requêtes/heure/IP sans authentification)

Publier en une commande : `powershell -ExecutionPolicy Bypass -File scripts\release.ps1 2.1.1 "correctifs" "notes"` — le tag poussé déclenche la compilation et la Release par GitHub Actions.

## Compilation

Le .NET Framework 4.x de Windows 10/11 suffit :

```bat
git clone https://github.com/ankeji/DiskMate.git
cd DiskMate
build.bat
```

`build.bat` utilise le `csc.exe` fourni : compilation d'un `_build.exe` sans icône, génération de `app.ico`, puis recompilation de `DiskMate.exe` avec l'icône. Ni Visual Studio ni NuGet.

Auto-test : `DiskMate.exe --selftest` (rendu des pages, chevauchements, traductions manquantes).

## Arborescence

```
DiskMate/
├── DiskMate.exe            binaire compilé
├── build.bat               compilation en un clic
├── version.txt             manifeste de mise à jour
├── src/                    sources (11 .cs + app.manifest)
├── .github/workflows/      CI et releases automatiques
├── scripts/                push.bat · release.ps1
└── README*.md              documentation en 10 langues
```

## Soutenir le projet

DiskMate est un projet personnel et gratuit : sans publicité, sans logiciel imposé, sans collecte de données. S'il vous a libéré quelques dizaines de gigaoctets, vous pouvez m'offrir un café :

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/T1S42A3IBF)

## Licence

MIT — voir [LICENSE](LICENSE).