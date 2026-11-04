# ADR-0002 - Dual-process Win32 hook engine and on-demand WinUI 3 settings

- Date: 2026-11-04
- Status: Accepted

## Context

VietKey must meet three strict priorities on Windows 11:
1. Fast startup and minimal background RAM footprint (< 10 MB while typing).
2. Zero-lag, glitch-free Vietnamese input across Chrome address bar, Google Search autocomplete, VS Code, Microsoft Word, and Microsoft Teams.
3. Simple, maintainable C# architecture using WinUI 3 without external databases, servers, or fragile system-wide DLL injection.

Hosting a WinUI 3 (`Microsoft.UI.Xaml`) window hidden in the System Tray keeps DirectComposition surfaces and XAML runtime caches resident in memory (~40–70 MB working set), violating priority #1. Conversely, implementing a Windows Text Services Framework (TSF) COM DLL requires complex in-process registration across 32-bit/64-bit/ARM64 processes and AppContainers, violating priority #3.

## Decision

1. **Split the runtime into two processes sharing `VietKey.Core`:**
   - `VietKey.Engine.exe`: A headless Win32 process (`OutputType=WinExe`, NativeAOT/Trimmed compatible) that owns the `WH_KEYBOARD_LL` hook, the `SendInput` batch injector, the foreground process cache, and the `Shell_NotifyIconW` System Tray icon. It loads zero XAML or WinUI 3 assemblies.
   - `VietKey.UI.exe`: A standalone WinUI 3 desktop application launched by `VietKey.Engine.exe` only when the user opens Settings, and terminated completely upon window close (`Application.Current.Exit()`).
2. **Use `WH_KEYBOARD_LL` + atomic batched `SendInput` (`KEYEVENTF_UNICODE`) with per-app profiles:**
   - Never block `LowLevelKeyboardProc` with disk I/O or uncached process queries.
   - Emit all `VK_BACK` and UTF-16 `KEYEVENTF_UNICODE` events in a single `SendInput` call.
   - Prepend an autocomplete-neutralizing sequence (`U+200B`/`U+200C` dummy char + extra `VK_BACK` in the same batch) when the cached foreground process matches a Chromium browser (`chrome.exe`, `msedge.exe`, `brave.exe`).
3. **Synchronise settings via flat JSON files and a named Win32 event:**
   - `%AppData%\VietKey\config.json` and `macros.json` are written atomically (write temp file + rename) by `VietKey.UI.exe`, which then signals `Local\VietKeyConfigReloadEvent` so `VietKey.Engine.exe` reloads settings in memory without dropping the hook.

## Alternatives considered

- **Single-process WinUI 3 app hidden to System Tray.** Rejected: Windows App SDK + XAML Islands keep ~45–70 MB of private working set allocated even when all windows are hidden, failing the low-RAM requirement.
- **Windows Text Services Framework (TSF / `ITfTextInputProcessor`) COM IME.** Rejected: requires system-wide COM registration, elevates crash blast radius into host applications, and significantly increases maintenance complexity compared to an out-of-process `WH_KEYBOARD_LL` hook.
- **SQLite or Named Pipe RPC for configuration storage.** Rejected: flat JSON files (`System.Text.Json` source-generated) + a named OS event (`EventWaitHandle`) require zero third-party packages, are human-editable, and reload in < 1 ms.

## Consequences

- Background memory usage during 99% of user uptime stays restricted to `VietKey.Engine.exe` (~3–8 MB with NativeAOT or trimmed .NET).
- Opening the Settings window incurs a ~150–250 ms cold start for `VietKey.UI.exe`, which is an acceptable trade-off for saving ~50 MB of continuous background RAM.
- All core Vietnamese composition, spellcheck, macro, and `SendInput` batch-planning logic lives in `VietKey.Core` with zero OS dependencies and 100% deterministic xUnit testability.
