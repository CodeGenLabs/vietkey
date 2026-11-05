# settings-and-tray

## MODIFIED Requirements

### Requirement: REQ-settings-3 - On-demand WinUI 3 Settings application with zero idle RAM footprint
The `VietKey.UI` project SHALL compile to `VietKey.exe` (`<AssemblyName>VietKey</AssemblyName>`) providing a compact, non-scrolling tabbed Windows 11 desktop settings window (`WinExe`) displaying the fixed full-bleed blue VietKey app logo (`app-logo.ico` / `app-logo.png`) on the Windows Taskbar and executable icon (independent of the System Tray `V`/`E` mode icon), an unclipped header card with the logo banner, active mode status text, and `V`/`E` badge (without a redundant quick-toggle button), and concise tab labels (`Cài đặt chung`, `Chính tả`, `Gõ tắt`, `Gõ thử`) to configure all typing options, hotkeys, Windows startup registration (`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`), the macro dictionary, and live sandbox typing without a main vertical scrollbar, SHALL automatically ensure `VietKey.Engine` is running when launched interactively, and SHALL terminate its process completely when the user closes the Settings window so the UI consumes 0 MB of RAM during background typing.

#### Scenario: Opening Settings from System Tray or Visual Studio launches `VietKey.exe` tabbed window without text clipping or vertical scrollbar
- **WHEN** the user launches `VietKey.exe` (directly from Visual Studio `F5`, via `--show-ui`, or from the `VietKey.Engine` tray menu)
- **THEN** `VietKey.exe` initializes its tabbed desktop window controls (`Cài đặt chung`, `Chính tả`, `Gõ tắt`, `Gõ thử`) and header status text without text clipping or a vertical `ScrollViewer`, displays the fixed blue VietKey app logo on the Taskbar, allows interactive editing of all toggles, dropdowns, and macros, and applies changes back to `SettingsViewModel`

#### Scenario: Closing the Settings window terminates the `VietKey.exe` UI process
- **WHEN** the user closes the `VietKey.exe` window
- **THEN** `VietKey.exe` flushes any pending configuration changes, signals `Local\VietKeyConfigReloadEvent`, and terminates its process completely while `VietKey.Engine` continues running in the System Tray
