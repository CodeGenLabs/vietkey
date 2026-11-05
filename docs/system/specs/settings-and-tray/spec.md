# settings-and-tray

## Purpose

Single-instance Win32 System Tray host, global language toggle hotkeys, portable local JSON configuration storage with live event-driven reload, and an on-demand WinUI 3 Settings application with zero idle RAM footprint.

### Requirement: REQ-settings-1 - Lightweight System Tray host and global toggle hotkeys
The `VietKey.Engine` process SHALL run as a single-instance Per-Monitor DPI v2 aware headless background daemon with a Win32 message window and message loop (`GetMessageW` / `DispatchMessageW`) hosting a native System Tray icon (`Shell_NotifyIconW`) that renders razor-sharp DPI-native hollow `V` and `E` icons via GDI (`EVFont.ttf` + `CreateDIBSection` + `CreateIconIndirect`), automatically switches between a white outline (`#FAFAFA`) on dark taskbars and a black outline (`#000000`) on light taskbars (`SystemUsesLightTheme`), toggles mode on left-click or configured global hotkey (`Ctrl+Shift` or `Alt+Z`), and exposes a popup context menu to switch input method, open the Settings app, or exit.

#### Scenario: Global hotkey toggles between Vietnamese and English modes
- **WHEN** the user presses the configured toggle hotkey (`Alt+Z` or `Ctrl+Shift` release without intervening keys)
- **THEN** the engine flips the active mode between `Vietnamese` and `English`, clears the syllable buffer, updates the System Tray icon between `V` and `E` immediately, and persists the per-app state if Smart Switch is enabled

#### Scenario: System Tray icon adapts outline color between dark and light taskbar themes
- **WHEN** the Windows taskbar theme switches between dark (`SystemUsesLightTheme = 0`) and light (`SystemUsesLightTheme = 1`)
- **THEN** `TrayIconHost` switches its active `V`/`E` `HICON` handle between the white-outlined (`#FAFAFA`) and black-outlined (`#000000`) native GDI icons and updates `Shell_NotifyIconW`

#### Scenario: Duplicate engine launch is prevented by single-instance mutex
- **WHEN** a second instance of `VietKey.Engine` is started while `Local\VietKeyEngineMutex` is already held
- **THEN** the second instance exits immediately with code `0` without registering a duplicate keyboard hook or tray icon

#### Scenario: System Tray mouse callback handles left-click toggle and right-click context menu
- **WHEN** the hidden tray message window receives `WM_APP_TRAY` with `WM_LBUTTONUP` or `WM_RBUTTONUP`
- **THEN** `WM_LBUTTONUP` toggles between `Vietnamese` and `English` modes immediately, while `WM_RBUTTONUP` opens the popup context menu with commands for toggling language, switching `Telex`/`VNI`, opening Settings, or exiting the message loop

### Requirement: REQ-settings-2 - Local JSON configuration persistence and zero-downtime live reload
The system SHALL store all user settings (`config.json`) and macros (`macros.json`) as human-readable UTF-8 JSON files under `%AppData%\VietKey\` without any database or network dependency, and `VietKey.Engine` SHALL reload configuration in memory whenever the named event `Local\VietKeyConfigReloadEvent` is signaled.

#### Scenario: Missing or corrupted configuration file falls back to safe defaults
- **WHEN** `VietKey.Engine` starts and `%AppData%\VietKey\config.json` does not exist or contains malformed JSON
- **THEN** the engine initializes with default settings (`InputMethod=Telex`, `VietnameseEnabled=true`, `ModernTone=true`, `SpellCheck=true`, `AutoRestoreEnglish=true`, `SmartSwitch=true`, `BrowserAutocompleteFix=true`, `ToggleHotkey=AltZ`) and writes a valid default `config.json`

#### Scenario: Live reload upon named event signal
- **WHEN** `VietKey.UI` saves updated settings to `%AppData%\VietKey\config.json` and signals `Local\VietKeyConfigReloadEvent`
- **THEN** `VietKey.Engine` reloads the new settings into memory within 50 ms without dropping the keyboard hook or restarting the process

### Requirement: REQ-settings-3 - On-demand WinUI 3 Settings application with zero idle RAM footprint
The `VietKey.UI` process SHALL provide a Windows 11 desktop settings window (`WinExe`) to configure all typing options, hotkeys, Windows startup registration (`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`), and the macro dictionary, SHALL automatically ensure `VietKey.Engine` is running when launched interactively, and SHALL terminate its process completely when the user closes the Settings window so the UI consumes 0 MB of RAM during background typing.

#### Scenario: Opening Settings from System Tray or Visual Studio displays interactive window and syncs controls
- **WHEN** the user launches `VietKey.UI` (directly from Visual Studio `F5`, via `--show-ui`, or from the `VietKey.Engine` tray menu)
- **THEN** `VietKey.UI` initializes its desktop window controls from `%AppData%\VietKey\config.json` and `macros.json`, allows interactive editing of all toggles, dropdowns, and macros, and applies changes back to `SettingsViewModel`

#### Scenario: Closing the WinUI 3 Settings window terminates the UI process
- **WHEN** the user closes the `VietKey.UI` window
- **THEN** `VietKey.UI.exe` flushes any pending configuration changes, signals `Local\VietKeyConfigReloadEvent`, and terminates its process completely while `VietKey.Engine` continues running in the System Tray
