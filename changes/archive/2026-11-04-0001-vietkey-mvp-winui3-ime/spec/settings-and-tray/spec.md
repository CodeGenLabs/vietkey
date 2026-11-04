## Purpose

Single-instance Win32 System Tray host, global language toggle hotkeys, portable local JSON configuration storage with live event-driven reload, and an on-demand WinUI 3 Settings application with zero idle RAM footprint.

## ADDED Requirements

### Requirement: REQ-settings-1 - Lightweight System Tray host and global toggle hotkeys
The `VietKey.Engine` process SHALL run as a single-instance headless background daemon hosting a native Win32 System Tray icon (`Shell_NotifyIconW`) that displays the current mode (`V` for Vietnamese, `E` for English), toggles mode on left-click or configured global hotkey (`Ctrl+Shift` or `Alt+Z`), and exposes a context menu to switch input method, open the WinUI 3 Settings app, or exit.

#### Scenario: Global hotkey toggles between Vietnamese and English modes
- **WHEN** the user presses the configured toggle hotkey (`Alt+Z` or `Ctrl+Shift` release without intervening keys)
- **THEN** the engine flips the active mode between `Vietnamese` and `English`, clears the syllable buffer, updates the System Tray icon immediately, and persists the per-app state if Smart Switch is enabled

#### Scenario: Duplicate engine launch is prevented by single-instance mutex
- **WHEN** a second instance of `VietKey.Engine` is started while `Local\VietKeyEngineMutex` is already held
- **THEN** the second instance exits immediately with code `0` without registering a duplicate keyboard hook or tray icon

### Requirement: REQ-settings-2 - Local JSON configuration persistence and zero-downtime live reload
The system SHALL store all user settings (`config.json`) and macros (`macros.json`) as human-readable UTF-8 JSON files under `%AppData%\VietKey\` without any database or network dependency, and `VietKey.Engine` SHALL reload configuration in memory whenever the named event `Local\VietKeyConfigReloadEvent` is signaled.

#### Scenario: Missing or corrupted configuration file falls back to safe defaults
- **WHEN** `VietKey.Engine` starts and `%AppData%\VietKey\config.json` does not exist or contains malformed JSON
- **THEN** the engine initializes with default settings (`InputMethod=Telex`, `VietnameseEnabled=true`, `ModernTone=true`, `SpellCheck=true`, `AutoRestoreEnglish=true`, `SmartSwitch=true`, `BrowserAutocompleteFix=true`, `ToggleHotkey=AltZ`) and writes a valid default `config.json`

#### Scenario: Live reload upon named event signal
- **WHEN** `VietKey.UI` saves updated settings to `%AppData%\VietKey\config.json` and signals `Local\VietKeyConfigReloadEvent`
- **THEN** `VietKey.Engine` reloads the new settings into memory within 50 ms without dropping the keyboard hook or restarting the process

### Requirement: REQ-settings-3 - On-demand WinUI 3 Settings application with zero idle RAM footprint
The `VietKey.UI` process SHALL provide a Windows 11 WinUI 3 window to configure all typing options, hotkeys, Windows startup registration (`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`), and the macro dictionary, and SHALL terminate its process completely when the user closes the Settings window so WinUI 3 consumes 0 MB of RAM during background typing.

#### Scenario: Opening Settings from System Tray launches WinUI 3 process on demand
- **WHEN** the user selects `Cài đặt (Settings)` from the `VietKey.Engine` tray menu
- **THEN** `VietKey.Engine` spawns `VietKey.UI.exe` (or brings its existing window to the foreground if already open) displaying the current configuration from `%AppData%\VietKey\config.json`

#### Scenario: Closing the WinUI 3 Settings window terminates the UI process
- **WHEN** the user closes the `VietKey.UI` window
- **THEN** `VietKey.UI.exe` flushes any pending configuration changes, signals `Local\VietKeyConfigReloadEvent`, and terminates its process completely while `VietKey.Engine` continues running in the System Tray

## Reference parity - EVKey / UniKey settings and lifecycle

- System Tray V/E indicator with left-click toggle and right-click context menu - ADDED, REQ-settings-1
- Global toggle hotkeys (`Alt+Z`, `Ctrl+Shift`) - ADDED, REQ-settings-1
- Portable local file configuration without database or cloud account - ADDED, REQ-settings-2
- Modern Windows 11 WinUI 3 Settings UI with on-demand process lifecycle - ADDED, REQ-settings-3
- Macro editor (add, edit, delete text expansions) - ADDED, REQ-settings-3
- Start with Windows toggle (`HKCU\...\Run`) - ADDED, REQ-settings-3
- Per-application manual exclusion list editor UI - deferred: Smart Switch (`REQ-os-3`) automatically remembers English/Vietnamese state per application without manual process path entry
