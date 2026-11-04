# settings-and-tray

## MODIFIED Requirements

### Requirement: REQ-settings-3 - On-demand WinUI 3 Settings application with zero idle RAM footprint
The `VietKey.UI` process SHALL provide a Windows 11 desktop settings window (`WinExe`) to configure all typing options, hotkeys, Windows startup registration (`HKCU\Software\Microsoft\Windows\CurrentVersion\Run` via `"Khởi động cùng Windows"` and `"Không hiển thị hộp thoại khi khởi động"`), and the macro dictionary, SHALL automatically ensure `VietKey.Engine` is running when launched interactively, and SHALL terminate its process completely when the user closes the Settings window so the UI consumes 0 MB of RAM during background typing.

#### Scenario: Opening Settings from System Tray or Visual Studio displays interactive window and syncs controls
- **WHEN** the user launches `VietKey.UI` (directly from Visual Studio `F5`, via `--show-ui`, or from the `VietKey.Engine` tray menu)
- **THEN** `VietKey.UI` initializes its desktop window controls from `%AppData%\VietKey\config.json` and `macros.json`, allows interactive editing of all toggles (including `"Khởi động cùng Windows"` and `"Không hiển thị hộp thoại khi khởi động"`), dropdowns, and macros, and applies changes back to `SettingsViewModel`

#### Scenario: Startup registration respects HideDialogOnStartup option
- **WHEN** `"Khởi động cùng Windows"` (`StartWithWindows`) is enabled
- **THEN** if `"Không hiển thị hộp thoại khi khởi động"` (`HideDialogOnStartup`) is checked, the startup command launches `VietKey.Engine.exe` silently in the background; if unchecked, the startup command passes `--show-ui` so the Settings dialog is shown on Windows startup

#### Scenario: Closing the WinUI 3 Settings window terminates the UI process
- **WHEN** the user closes the `VietKey.UI` window
- **THEN** `VietKey.UI.exe` flushes any pending configuration changes, signals `Local\VietKeyConfigReloadEvent`, and terminates its process completely while `VietKey.Engine` continues running in the System Tray
