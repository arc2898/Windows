# Contributing

1. Keep features modular under `src/AeroDesk/Models` and `src/AeroDesk/Services`.
2. Avoid polling loops, external services, injected DLLs, Explorer patching, and mandatory admin privileges.
3. Store user data only under `%APPDATA%\AeroDesk`; do not write it into the installation directory.
4. Preserve a usable non-blurred fallback if a Windows build rejects a composition effect.
5. Run `dotnet build -c Release` on Windows and validate startup, tray exit, widget restoration, note persistence, and uninstall before proposing a release.
6. Do not claim lower resource use without measurements made on the same system and workload.
