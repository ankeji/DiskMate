# DiskMate · Limpiador de disco C: para Windows

[简体中文](README.md) · [繁體中文](README.zh-TW.md) · [English](README.en.md) · [日本語](README.ja.md) · [한국어](README.ko.md) · [Deutsch](README.de.md) · [Français](README.fr.md) · **Español** · [Português](README.pt.md) · [Русский](README.ru.md)

[![build](https://github.com/ankeji/DiskMate/actions/workflows/build.yml/badge.svg)](https://github.com/ankeji/DiskMate/actions/workflows/build.yml)
[![release](https://github.com/ankeji/DiskMate/actions/workflows/release.yml/badge.svg)](https://github.com/ankeji/DiskMate/releases)
[![license](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

Herramienta de escritorio para limpiar el disco C: en Windows: **pulsa «Iniciar análisis» → se listan los elementos limpiables → cada uno muestra su riesgo y qué implica borrarlo → tú marcas lo que quieras → confirmas y se limpia**.

Un solo exe, sin instalación ni runtime adicional (solo el .NET Framework 4.x incluido en Windows).

**[Descargar la última versión](https://github.com/ankeji/DiskMate/releases/latest)** · **[Reportar un problema](https://github.com/ankeji/DiskMate/issues)**

![Pantalla de bienvenida](screenshot.png)

## Características

- **No toca el disco al arrancar.** La app se queda en la pantalla de bienvenida; no lee nada hasta «Iniciar análisis» (o F5).
- **Análisis cancelable.** 13 pasos con progreso; cancelar no deja ningún cambio.
- **Tres niveles de riesgo.** Cada elemento indica bajo / medio / alto, con «qué se borra» y «qué pasa después»; solo los de bajo riesgo vienen marcados.
- **Cuatro tarjetas de métricas.** Total, bajo, medio, alto; al pulsar una se filtra.
- **Panel de detalle.** Impacto, consejo, rutas reales y «Abrir carpeta».
- **Interfaz dibujada a mano.** Sin controles nativos ni librerías de terceros, mismo aspecto en cualquier DPI.
- **10 idiomas de interfaz**, chino simplificado por defecto, cambio inmediato y se recuerda.
- **Actualización integrada.** Un clic abajo a la izquierda: comprobar → descargar → verificar (SHA-256) → reemplazar → reiniciar.

## Páginas

| Página | Contenido |
|---|---|
| Chequeo de limpieza | 13 pasos: temporales, cachés de actualización, registros, papelera, archivo de hibernación, controladores antiguos y más |
| Archivos grandes | Analiza tu perfil y carpetas de archivos grandes, ordenado por tamaño |
| Registro | Historial completo de análisis y limpieza, copiable |

## Idiomas

| Idioma | Código | Cobertura |
|---|---|---|
| 简体中文 | `zh-CN` | interfaz + todos los elementos (por defecto) |
| 繁體中文 | `zh-TW` | interfaz + todos los elementos |
| English | `en-US` | interfaz + todos los elementos |
| 日本語 / 한국어 / Deutsch / Français / Español / Português / Русский | `es-ES` etc. | solo interfaz; los textos de elementos caen a inglés |

## Actualización integrada

1. Abajo a la izquierda aparece `v2.1.0 · Buscar actualizaciones`; si hay una versión nueva el botón se vuelve azul «Actualizar a v2.1.x».
2. Muestra la comparación de versiones y las notas; «Descargar e instalar» inicia la descarga con barra de progreso.
3. Al terminar se verifica con SHA-256 y un pequeño script por lotes reemplaza el exe tras cerrar y relanza la nueva versión.

Se consultan dos fuentes y gana la versión más alta:

- `https://raw.githubusercontent.com/ankeji/DiskMate/main/version.txt`
- `https://api.github.com/repos/ankeji/DiskMate/releases/latest` (respaldo, 60 peticiones/hora/IP sin autenticar)

Publicar con una orden: `powershell -ExecutionPolicy Bypass -File scripts\release.ps1 2.1.1 "cambios" "notes"` — el tag empujado hace que GitHub Actions compile y publique la Release.

## Compilar

Basta con el .NET Framework 4.x de Windows 10/11:

```bat
git clone https://github.com/ankeji/DiskMate.git
cd DiskMate
build.bat
```

`build.bat` usa el `csc.exe` del sistema: compila un `_build.exe` sin icono, genera `app.ico` y recompila `DiskMate.exe` con el icono. Sin Visual Studio ni NuGet.

Autoprueba: `DiskMate.exe --selftest` (render de páginas, solapamientos, traducciones faltantes).

## Estructura

```
DiskMate/
├── DiskMate.exe            binario compilado
├── build.bat               compilación en un clic
├── version.txt             manifiesto de actualización
├── src/                    fuentes (11 .cs + app.manifest)
├── .github/workflows/      CI y releases automáticas
├── scripts/                push.bat · release.ps1
└── README*.md              documentación en 10 idiomas
```

## Apoyar el proyecto

DiskMate es un proyecto personal y gratuito: sin publicidad, sin software incluido, sin recopilar datos. Si te ha liberado unas decenas de gigabytes, puedes invitarme a un café:

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/T1S42A3IBF)

## Licencia

MIT — ver [LICENSE](LICENSE).