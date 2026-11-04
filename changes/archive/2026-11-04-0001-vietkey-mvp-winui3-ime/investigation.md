# Investigation — 0001-vietkey-mvp-winui3-ime

## How it works today

- The repository `d:\git-dev\vietkey` is a greenfield project initialized with the Forge store (`docs/system/`, `.forge/`).
- No source code or claims exist yet (`docs/system/derived/inventory.json` has 0 source files).
- Host environment verification (`dotnet --list-sdks` and `forge doctor`):
  - OS: Windows 11 (`10.0.26300`).
  - Installed .NET SDKs: `8.0.427`, `9.0.305`, `10.0.403`.
  - Forge C# grammar parser is active and available.

## Where the request lands

The greenfield implementation will land across a 4-project .NET solution (`VietKey.sln`):

1. `src/VietKey.Core/` — Pure C# class library (zero Win32/OS dependencies):
   - `SyllableBuffer` & `VietnameseTransformer`: Deterministic state machine for Telex & VNI composition, free diacritic marking, vowel cluster tone placement (modern `oà`/`uý` and classic `òa`/`úy`), double-key undo (`aa` -> `â`, `aaa` -> `aa`), and `w` -> `ư` / `[[` handling.
   - `VietnameseSpellChecker`: Phonotactic validator (initial consonant, medial, nucleus vowel cluster, final consonant, tone compatibility, and foreign letter detection `f, j, w, z`) that triggers instant raw-key restoration when typing English words (e.g., `window`, `with`, `function`).
   - `MacroTable`: Fast prefix/exact lookup table for user-defined text expansions triggered on word boundary (Space/Enter/punctuation).
   - `AppConfig` & `ConfigStore`: JSON serialization (using `System.Text.Json` source generation for AOT/trimming compatibility and zero reflection overhead) reading/writing `%AppData%\VietKey\config.json` and `%AppData%\VietKey\macros.json`.
2. `src/VietKey.Engine/` — Lightweight Win32 background process (`OutputType=WinExe`, publishable with NativeAOT / Trimmed single-file):
   - `LowLevelKeyboardHook`: `SetWindowsHookExW(WH_KEYBOARD_LL)` filtering injected keys (`LLKHF_INJECTED`) and modifier shortcuts (`Ctrl+Shift` or `Alt+Z`).
   - `InputInjector`: Batched `SendInput` (`KEYEVENTF_UNICODE` + `VK_BACK`) with per-application compatibility profiles (`AppCompatibilityProfile`) for Chromium (`chrome.exe`, `msedge.exe`, `brave.exe`), Electron (`Code.exe`, `ms-teams.exe`), and Office (`WINWORD.EXE`, `EXCEL.EXE`).
   - `ForegroundAppTracker`: Uses `GetForegroundWindow` + `GetWindowThreadProcessId` + process name cache (`QueryFullProcessImageNameW`) to power Smart Switch (per-app Vi/En memory) and select the right injection strategy without querying process metadata on every keystroke.
   - `TrayIconHost`: Pure Win32 `Shell_NotifyIconW` message-only window (`HWND_MESSAGE`) with V/E icon indicator, quick context menu, and single-instance mutex (`Local\VietKeyEngineMutex`) + named reload event (`Local\VietKeyConfigReloadEvent`).
3. `src/VietKey.UI/` — WinUI 3 desktop application (Windows App SDK):
   - Launched on-demand by `VietKey.Engine` when the user opens Settings.
   - Reads/writes `%AppData%\VietKey\config.json` and `macros.json`, then signals `Local\VietKeyConfigReloadEvent` so `VietKey.Engine` applies changes immediately in < 1 ms.
   - Exits completely when closed so 0 MB of WinUI 3 / XAML / DirectComposition memory remains in RAM during background typing.
4. `tests/VietKey.Core.Tests/` — xUnit test suite covering Telex/VNI transformations, tone repositioning, English auto-restore, macro expansion, app compatibility batch calculation, and config persistence.

## What surprised you

1. **WinUI 3 Background RAM vs. User Criterion #1 ("Nhẹ, tiêu thụ ít RAM"):**
   - Even an empty WinUI 3 window hidden to the system tray keeps `Microsoft.ui.xaml.dll`, DirectComposition surfaces, and WebView/XAML caches mapped in memory (~40–70 MB Working Set).
   - Splitting `VietKey.Engine` (pure Win32 loop + `VietKey.Core`) from `VietKey.UI` (WinUI 3 on-demand settings app) solves this completely: background typing uses ~3–8 MB RAM while still giving the user a 100% native Windows 11 Fluent / Mica WinUI 3 interface when configuring settings.
2. **Why Chrome Address Bar, Google Search, VS Code, and Teams lag or eat characters with naive hooks:**
   - **Issue A (Chromium Inline Autocomplete):** In Chrome/Edge address bars and Google Search boxes, typing `d` autocompletes `docs.google.com` with `ocs.google.com` highlighted. When the user presses `d` again to produce `đ`, a naive IME sends `VK_BACK` + `đ`. The `VK_BACK` only deletes the highlighted `ocs.google.com` selection instead of the first `d`, resulting in `dđ`!
   - **Solution:** For Chromium/browser executables, prepend an empty/non-breaking control action (sending a single `VK_BACK` after neutralizing selection via `Shift+Left` or injecting a zero-width/dummy char + `VK_BACK` batch in a single atomic `SendInput` call) when replacing characters.
   - **Issue B (Hook Timeout & Lag in VS Code / Teams / Word):** Doing process-name lookups (`Process.GetProcessById`) or file I/O inside `LowLevelKeyboardProc` causes keystroke stutter and risks Windows silently unhooking the callback (`LowLevelHooksTimeout`).
   - **Solution:** Cache `HWND -> ProcessProfile` via `WinEventHook` (`EVENT_SYSTEM_FOREGROUND`) or an O(1) last-HWND cache, and send all backspaces + replacement UTF-16 code units in a **single batched `SendInput` array** with `LLKHF_INJECTED` bypass.

## What you could not determine

- All initial ambiguities (language choice, dual-process RAM architecture, and MVP feature set including Free Marking, Auto-Restore English, Smart Switch, and Macros) were resolved with the user at Gate G1.
