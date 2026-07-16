# Codex Usage Viewer

[English](README.md) | [中文](README.zh-CN.md)

<!-- GitHub description: A lightweight, local-first Windows widget for viewing ChatGPT Codex usage. -->
<!-- Suggested topics: codex, chatgpt, windows, desktop, widget, usage, quota, wpf, dotnet -->

## Project Introduction

A lightweight, local-first Windows widget for viewing the remaining ChatGPT Codex usage allowance. It is privacy-focused, read-only, and designed around least privilege.

## Features

- Small, borderless, always-on-top WPF widget
- Native green, yellow, and red remaining-allowance indicator
- Dynamic usage windows and reset countdown
- Startup and manual refresh with loading state
- Draggable, resizable, and remembered window geometry
- Close-to-tray with Show, Refresh, About, and Exit actions
- No telemetry, analytics, automatic updates, or background auto-refresh

## Screenshot

![Codex Usage Viewer](screenshot.png)

## Status Colors

The indicator color reflects the remaining quota.

- 🟢 Green: More than 50% remaining
- 🟡 Yellow: 21%–50% remaining
- 🔴 Red: 20% or less remaining

## Requirements

- Windows 10 or Windows 11
- Official Codex installation with `codex` on `PATH`
- A ChatGPT account signed in through Codex
- Windows .NET Framework and WPF components

## Build

This project intentionally uses a lightweight build process and has never depended on a Visual Studio `.sln` or an SDK-style `.csproj`. `build.ps1` is the only build entry point; this is a deliberate design choice, not a missing repository file.

The script:

- Collects every `.cs` file under `src`
- Invokes the Windows-provided .NET Framework `csc.exe`
- References the required assemblies
- Produces the final executable

Build from source:

```powershell
git clone <repository-url>
cd codex-usage-viewer
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

If Visual Studio or `dotnet build` support is needed in the future, a later version may migrate to a standard SDK project structure.

## Usage

Run `dist\CodexUsageViewer.exe`. The widget refreshes once at startup. Use `↻` to refresh, `✕` to hide to the system tray, and the tray menu to Show, Refresh, view About, or Exit.

The displayed value is `100 - usedPercent`. Green means at least 50% remains, yellow means 20–49%, and red means below 20%. If `secondary == null`, the short window displays `5h —`.

## Architecture

```text
MainWindow / TrayController
          ↓
     UsageService
          ↓
 DesktopUsageProvider
          ↓
official codex app-server
          ↓
account/rateLimits/read
```

The UI does not know the data source. App Server communication and parsing remain isolated in `DesktopUsageProvider`.

## Privacy

- The only business request is `account/rateLimits/read`.
- Authentication remains inside the official `codex app-server`.
- No cookie, token, chat, prompt, browser, profile, credential, clipboard, or user-document access.
- No direct HTTP client, third-party endpoint, telemetry, analytics, service, scheduled task, startup entry, registry change, or privilege elevation.
- Usage responses stay in memory. Only window geometry and a static Program Network Audit are persisted.

See [SECURITY.md](SECURITY.md) for the full audit.

## License

Licensed under the [MIT License](LICENSE).

## Disclaimer

This project is an independent open-source utility and is not affiliated with, endorsed by, or maintained by OpenAI.

"ChatGPT", "Codex", and related names are trademarks of OpenAI.
