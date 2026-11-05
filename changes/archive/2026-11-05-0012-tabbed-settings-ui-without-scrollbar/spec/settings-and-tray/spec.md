# settings-and-tray

## MODIFIED Requirements

### Requirement: REQ-settings-3 - On-demand WinUI 3 Settings application with zero idle RAM footprint
The `VietKey.UI` process SHALL provide a compact, non-scrolling tabbed Windows 11 desktop settings window (`WinExe`) displaying the fixed full-bleed blue VietKey app logo (`app-logo.ico` / `app-logo.png`) on the Windows Taskbar and executable icon (independent of the System Tray `V`/`E` mode icon) to configure all typing options, hotkeys, Windows startup registration (`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`), the macro dictionary, and live sandbox typing across dedicated tabs without a main vertical scrollbar, SHALL automatically ensure `VietKey.Engine` is running when launched interactively, and SHALL terminate its process completely when the user closes the Settings window so the UI consumes 0 MB of RAM during background typing.

#### Scenario: Opening Settings from System Tray or Visual Studio displays interactive tabbed window without vertical scrollbar
- **WHEN** the user launches `VietKey.UI` (directly from Visual Studio `F5`, via `--show-ui`, or from the `VietKey.Engine` tray menu)
- **THEN** `VietKey.UI` initializes its tabbed desktop window controls (`Thiết lập chung`, `Chính tả & Thông minh`, `Bảng gõ tắt`, `Gõ thử nghiệm`) without a vertical `ScrollViewer`, displays the fixed blue VietKey app logo on the Taskbar, allows interactive editing of all toggles, dropdowns, and macros, and applies changes back to `SettingsViewModel`

#### Scenario: Closing the WinUI 3 Settings window terminates the UI process
- **WHEN** the user closes the `VietKey.UI` window
- **THEN** `VietKey.UI.exe` flushes any pending configuration changes, signals `Local\VietKeyConfigReloadEvent`, and terminates its process completely while `VietKey.Engine` continues running in the System Tray
