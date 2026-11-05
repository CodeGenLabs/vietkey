# settings-and-tray

## MODIFIED Requirements

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
