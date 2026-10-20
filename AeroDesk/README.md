# AeroDesk

**AeroDesk is a lightweight Windows 11 glass-widget host.** It is an independent WPF application and does not require Windhawk, Rainmeter, injected DLLs, Explorer patching, or a background service.

> Active development. The Windows CI workflow builds the application and installer on a Windows runner. Actual desktop UI testing and resource benchmarks still need to be performed on a Windows PC.

## Features

- Windows 11 Mica/system-backdrop request, with best-effort acrylic fallback for app-owned windows.
- Floating, draggable, resizable widgets: clock, date, quick notes, memory/system monitor, network throughput, and battery status.
- Persistent widget positions, sizes, visibility, and notes in `%APPDATA%\AeroDesk\settings.json`.
- Debounced note and layout saves.
- Tray icon, per-user Windows startup toggle, topmost/opacity/glass settings.
- Self-contained Windows x64 publish, installer with built-in uninstaller, and portable ZIP.

## Resource design

No continuously running Windows service. Widgets use timer-driven updates: clock every second, network every two seconds, system metrics every five seconds, battery every ten seconds. Network enumeration and memory polling run only when the corresponding widget is open. No third-party runtime NuGet dependencies.

Claims of lower CPU/RAM use than Windhawk or Rainmeter require repeatable measurements on the same PC; this project does not claim unverified benchmark results.

## Requirements

- Windows 11 x64 (Windows 10 may work with reduced visual effects).
- .NET 8 SDK to build locally; the self-contained published build does not require a separately installed .NET runtime.

## Build from source

From the repository's `AeroDesk` directory on Windows:

```powershell
dotnet build .\src\AeroDesk\AeroDesk.csproj -c Release
dotnet run --project .\src\AeroDesk\AeroDesk.csproj -c Release
```

Publish a self-contained portable build:

```powershell
dotnet publish .\src\AeroDesk\AeroDesk.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o .\artifacts\publish
```

`.\scripts\Build-Release.ps1` publishes the app and invokes Inno Setup when `ISCC.exe` is installed. GitHub Actions installs the packaging tool automatically.

## Windows installer

Each GitHub Actions run creates:

- `AeroDesk-Setup-0.2.0-x64.exe` — per-user installer.
- Standard Windows uninstaller listed under **Settings → Apps → Installed apps**.
- `AeroDesk-Portable-win-x64.zip` — self-contained portable build.

**No ISO is generated.** The installer is not currently code-signed, so Windows SmartScreen may display a warning. Uninstalling offers to preserve or delete the user's notes and settings.

## Limitations

- AeroDesk styles only windows it owns. Windows does not provide a supported API to restore Windows 7 Aero Glass for all third-party windows.
- Desktop WorkerW parenting and desktop-icon layering are not implemented yet.
- The app has not yet been benchmarked against Windhawk or Rainmeter on the same machine.
- Test the UI on a Windows 11 desktop; successful CI compilation alone does not verify visual behavior.

## Configuration and privacy

Settings and notes are stored locally for the current Windows user in `%APPDATA%\AeroDesk`. Startup integration writes only the current user's `Run` registry key. No administrator permissions are intended to be required.

## Roadmap

- [ ] Test across supported Windows 11 builds and graphics configurations.
- [ ] Add desktop-only layering after focused compatibility testing.
- [ ] Add widget layout import/export and a more capable calendar.
- [ ] Add repeatable idle CPU/RAM benchmarks.
- [ ] Add code signing.
