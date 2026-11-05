# DiskMate · Limpador de disco C: para Windows

[简体中文](README.md) · [繁體中文](README.zh-TW.md) · [English](README.en.md) · [日本語](README.ja.md) · [한국어](README.ko.md) · [Deutsch](README.de.md) · [Français](README.fr.md) · [Español](README.es.md) · **Português** · [Русский](README.ru.md)

[![build](https://github.com/ankeji/DiskMate/actions/workflows/build.yml/badge.svg)](https://github.com/ankeji/DiskMate/actions/workflows/build.yml)
[![release](https://github.com/ankeji/DiskMate/actions/workflows/release.yml/badge.svg)](https://github.com/ankeji/DiskMate/releases)
[![license](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

Ferramenta de desktop para limpar o disco C: no Windows: **clique em «Iniciar verificação» → os itens limpáveis são listados um a um → cada um mostra o risco e o que a exclusão faz → você marca o que quiser → confirma e limpa**.

Um único exe, sem instalação e sem runtime extra (apenas o .NET Framework 4.x do Windows).

**[Baixar a versão mais recente](https://github.com/ankeji/DiskMate/releases/latest)** · **[Relatar um problema](https://github.com/ankeji/DiskMate/issues)**

![Tela de boas-vindas](screenshot.png)

## Destaques

- **Não mexe no disco sozinho.** O app abre na tela de boas-vindas; nada é lido antes de «Iniciar verificação» (ou F5).
- **Verificação cancelável.** 13 etapas com progresso; cancelar não deixa alterações.
- **Três níveis de risco.** Cada item recebe baixo / médio / alto, com «o que é apagado» e «o que acontece depois»; só os de baixo risco vêm marcados.
- **Quatro cartões de métricas.** Total, baixo, médio, alto; clique filtra.
- **Painel de detalhes.** Impacto, recomendação, caminhos reais e «Abrir pasta».
- **Interface desenhada à mão.** Sem controles nativos nem bibliotecas de terceiros, mesma aparência em qualquer DPI.
- **10 idiomas de interface**, chinês simplificado por padrão, troca imediata e memorizada.
- **Atualização integrada.** Um clique no canto inferior esquerdo: verificar → baixar → validar (SHA-256) → substituir → reiniciar.

## Páginas

| Página | Conteúdo |
|---|---|
| Verificação de limpeza | 13 etapas: temporários, caches de atualização, logs, lixeira, arquivo de hibernação, drivers antigos e mais |
| Arquivos grandes | Varre o perfil e as pastas de arquivos grandes, ordenado por tamanho |
| Log | Histórico completo de verificação e limpeza, copiável |

## Idiomas

| Idioma | Código | Cobertura |
|---|---|---|
| 简体中文 | `zh-CN` | interface + todos os itens (padrão) |
| 繁體中文 | `zh-TW` | interface + todos os itens |
| English | `en-US` | interface + todos os itens |
| 日本語 / 한국어 / Deutsch / Français / Español / Português / Русский | `pt-BR` etc. | apenas interface; textos dos itens caem para inglês |

## Atualização integrada

1. No canto inferior esquerdo fica `v2.1.0 · Verificar atualizações`; havendo versão nova o botão fica azul «Atualizar para v2.1.x».
2. Mostra a comparação de versões e as notas; «Baixar e instalar» inicia o download com barra de progresso.
3. Ao terminar, o arquivo é validado com SHA-256 e um pequeno script em lote troca o exe após o app fechar e abre a nova versão.

Duas fontes são consultadas e a versão mais alta vence:

- `https://raw.githubusercontent.com/ankeji/DiskMate/main/version.txt`
- `https://api.github.com/repos/ankeji/DiskMate/releases/latest` (reserva, 60 requisições/hora/IP sem autenticação)

Publicar em um comando: `powershell -ExecutionPolicy Bypass -File scripts\release.ps1 2.1.1 "mudanças" "notes"` — a tag enviada faz o GitHub Actions compilar e publicar a Release.

## Compilar

Basta o .NET Framework 4.x do Windows 10/11:

```bat
git clone https://github.com/ankeji/DiskMate.git
cd DiskMate
build.bat
```

`build.bat` usa o `csc.exe` do sistema: compila um `_build.exe` sem ícone, gera `app.ico` e recompila `DiskMate.exe` com o ícone. Sem Visual Studio e sem NuGet.

Autoteste: `DiskMate.exe --selftest` (renderização das páginas, sobreposições, traduções faltando).

## Estrutura

```
DiskMate/
├── DiskMate.exe            binário compilado
├── build.bat               compilação em um clique
├── version.txt             manifesto de atualização
├── src/                    fontes (11 .cs + app.manifest)
├── .github/workflows/      CI e releases automáticas
├── scripts/                push.bat · release.ps1
└── README*.md              documentação em 10 idiomas
```

## Licença

MIT — veja [LICENSE](LICENSE).