# settings-and-tray

## Purpose

Single-instance Win32 System Tray host, global language toggle hotkeys, portable local JSON configuration storage with live event-driven reload, and an on-demand WinUI 3 Settings application with zero idle RAM footprint.

### Requirement: REQ-settings-1 - Lightweight System Tray host and global toggle hotkeys
The `VietKey.Engine` process SHALL run as a single-instance Per-Monitor DPI v2 aware headless background daemon with a Win32 message window and message loop (`GetMessageW` / `DispatchMessageW`) hosting a native System Tray icon (`Shell_NotifyIconW`) that renders razor-sharp DPI-native hollow `V` and `E` icons via GDI (`EVFont.ttf` + `CreateDIBSection` + `CreateIconIndirect`), automatically switches between a white outline (`#FAFAFA`) on dark taskbars and a black outline (`#000000`) on light taskbars (`SystemUsesLightTheme`), toggles mode on single or rapid consecutive left-clicks without opening the Settings window, toggles mode reliably on the configured global hotkey (`Ctrl+Shift` or `Alt+Z`) even after prior `Alt+Tab`, `Win`, `Shift+Key`, or `Ctrl+Key` combinations, and exposes a popup context menu to switch input method, open the Settings app, or exit.

#### Scenario: Global hotkey toggles between Vietnamese and English modes reliably after prior modifier combinations
- **WHEN** the user presses `Alt+Tab`, `Win`, `Shift+Letter`, or `Ctrl+C` and subsequently presses the configured toggle hotkey (`Alt+Z` or `Ctrl+Shift` release without intervening keys)
- **THEN** the engine flips the active mode between `Vietnamese` and `English`, clears the syllable buffer, updates the System Tray icon between `V` and `E` immediately, and persists the per-app state if Smart Switch is enabled

#### Scenario: System Tray icon adapts outline color between dark and light taskbar themes
- **WHEN** the Windows taskbar theme switches between dark (`SystemUsesLightTheme = 0`) and light (`SystemUsesLightTheme = 1`)
- **THEN** `TrayIconHost` switches its active `V`/`E` `HICON` handle between the white-outlined (`#FAFAFA`) and black-outlined (`#000000`) native GDI icons and updates `Shell_NotifyIconW`

#### Scenario: Duplicate engine launch is prevented by single-instance mutex
- **WHEN** a second instance of `VietKey.Engine` is started while `Local\VietKeyEngineMutex` is already held
- **THEN** the second instance exits immediately with code `0` without registering a duplicate keyboard hook or tray icon

#### Scenario: System Tray mouse callback handles rapid left-click toggles without opening Settings and right-click context menu
- **WHEN** the hidden tray message window receives `WM_APP_TRAY` with `WM_LBUTTONUP`, `WM_LBUTTONDBLCLK`, or `WM_RBUTTONUP`
- **THEN** `WM_LBUTTONUP` toggles between `Vietnamese` and `English` modes immediately, `WM_LBUTTONDBLCLK` does not open the Settings dialog during rapid toggling, and `WM_RBUTTONUP` opens the popup context menu with commands for toggling language, switching `Telex`/`VNI`, opening Settings, or exiting the message loop

### Requirement: REQ-settings-2 - Local JSON configuration persistence and zero-downtime live reload
The system SHALL store all user settings (`config.json`) and macros (`macros.json`) as human-readable UTF-8 JSON files under `%AppData%\VietKey\` without any database or network dependency, SHALL synchronize cross-process reads and writes so rapid consecutive saves and concurrent live reloads never throw `UnauthorizedAccessException` or `IOException`, and `VietKey.Engine` SHALL reload configuration in memory whenever the named event `Local\VietKeyConfigReloadEvent` is signaled.

#### Scenario: Missing or corrupted configuration file falls back to safe defaults
- **WHEN** `VietKey.Engine` starts and `%AppData%\VietKey\config.json` does not exist or contains malformed JSON
- **THEN** the engine initializes with default settings (`InputMethod=Telex`, `VietnameseEnabled=true`, `ModernTone=true`, `SpellCheck=true`, `AutoRestoreEnglish=true`, `SmartSwitch=true`, `BrowserAutocompleteFix=true`, `ToggleHotkey=AltZ`) and writes a valid default `config.json`

#### Scenario: Live reload upon named event signal
- **WHEN** `VietKey.UI` saves updated settings to `%AppData%\VietKey\config.json` and signals `Local\VietKeyConfigReloadEvent`
- **THEN** `VietKey.Engine` reloads the new settings into memory within 50 ms without dropping the keyboard hook or restarting the process

#### Scenario: Rapid consecutive E/V mode toggles and concurrent reload reads complete without file lock contention
- **WHEN** the user rapidly toggles `E -> V -> E` in `VietKey.UI` while `VietKey.Engine` (`ConfigReloadCoordinator`) concurrently reloads `config.json` and `macros.json`
- **THEN** `QuickToggleModeButton` saves once per click, `ConfigStore` serializes cross-process file access safely without throwing `UnauthorizedAccessException` or `IOException`, and `VietKey.Engine` converges on the latest configuration state

### Requirement: REQ-settings-3 - On-demand WinUI 3 Settings application with zero idle RAM footprint
The `VietKey.UI` process SHALL provide a compact, non-scrolling tabbed Windows 11 desktop settings window (`WinExe`) displaying the fixed full-bleed blue VietKey app logo (`app-logo.ico` / `app-logo.png`) on the Windows Taskbar and executable icon (independent of the System Tray `V`/`E` mode icon), an unclipped header card with the logo banner, active mode status text, and `V`/`E` badge (without a redundant quick-toggle button), and concise tab labels (`Cài đặt chung`, `Chính tả`, `Gõ tắt`, `Gõ thử`) to configure all typing options, hotkeys, Windows startup registration (`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`), the macro dictionary, and live sandbox typing without a main vertical scrollbar, SHALL automatically ensure `VietKey.Engine` is running when launched interactively, and SHALL terminate its process completely when the user closes the Settings window so the UI consumes 0 MB of RAM during background typing.

#### Scenario: Opening Settings from System Tray or Visual Studio displays interactive tabbed window without text clipping or vertical scrollbar
- **WHEN** the user launches `VietKey.UI` (directly from Visual Studio `F5`, via `--show-ui`, or from the `VietKey.Engine` tray menu)
- **THEN** `VietKey.UI` initializes its tabbed desktop window controls (`Cài đặt chung`, `Chính tả`, `Gõ tắt`, `Gõ thử`) and header status text without text clipping or a vertical `ScrollViewer`, displays the fixed blue VietKey app logo on the Taskbar, allows interactive editing of all toggles, dropdowns, and macros, and applies changes back to `SettingsViewModel`

#### Scenario: Closing the WinUI 3 Settings window terminates the UI process
- **WHEN** the user closes the `VietKey.UI` window
- **THEN** `VietKey.UI.exe` flushes any pending configuration changes, signals `Local\VietKeyConfigReloadEvent`, and terminates its process completely while `VietKey.Engine` continues running in the System Tray
