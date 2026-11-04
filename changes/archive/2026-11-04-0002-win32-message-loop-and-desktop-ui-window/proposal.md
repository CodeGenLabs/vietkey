# Proposal: 0002-win32-message-loop-and-desktop-ui-window

## Why

When launching the solution from Visual Studio (`F5`), `VietKey.UI` previously initialized only its headless `SettingsViewModel` and exited immediately, while `VietKey.Engine` registered `WH_KEYBOARD_LL` and exited without running a Win32 message loop (`GetMessageW` / `DispatchMessageW`) or attaching a message window for the System Tray icon. We need both processes to run as true Windows Desktop applications (`net8.0-windows`, `WinExe`) with a live Windows 11 Fluent settings window, a Win32 message pump + tray context menu, and Visual Studio startup configuration so running either project brings up the UI and background engine seamlessly.

## What Changes

- **Desktop Settings Window (`src/VietKey.UI/`):**
  - Target `net8.0-windows` (`OutputType=WinExe`, `UseWPF=true`) to render a native Windows 11 Fluent-styled settings window (`MainWindow`) bound to `SettingsViewModel` with live controls for Vietnamese/English mode, Telex/VNI input method, toggle hotkey (`Alt+Z` / `Ctrl+Shift`), modern tone placement, spellcheck & English auto-restore, Smart Switch, browser autocomplete fix, macro table editor, Windows 11 startup toggle, and an inline live typing test box.
  - Automatically start `VietKey.Engine` in the background when `VietKey.UI` launches interactively if the engine is not already running.
- **Win32 Message Loop & System Tray Popup Menu (`src/VietKey.Engine/`):**
  - Target `net8.0-windows` (`OutputType=WinExe`) and add `Win32MessageLoop` to create a hidden message window (`CreateWindowExW`), attach `TrayIconHost` with dynamic `V`/`E` badge icons, handle left-click toggle and right-click popup context menu (`CreatePopupMenu` / `TrackPopupMenuEx`), and run `GetMessageW` / `TranslateMessage` / `DispatchMessageW` until exit.
  - Support `--show-ui` command-line flag (and configure `launchSettings.json` + solution project order in `VietKey.slnx`) so pressing `F5` in Visual Studio immediately opens the Settings UI window alongside the System Tray engine.

## Capabilities

- **Modified:**
  - `docs/system/specs/settings-and-tray/spec.md`

## Claims touched

- None (0 ratified claims in store; conforms to `ADR-0002-dual-process-hook-architecture`).
