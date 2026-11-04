# settings-and-tray

## MODIFIED Requirements

### Requirement: REQ-settings-1 - Lightweight System Tray host and global toggle hotkeys
The `VietKey.Engine` process SHALL run as a single-instance headless background daemon with a Win32 message window and message loop (`GetMessageW` / `DispatchMessageW`) hosting a native System Tray icon (`Shell_NotifyIconW`) that loads and displays the custom glossy `V` icon (`icon-v.ico`) for Vietnamese mode and `E` icon (`icon-e.ico`) for English mode, toggles mode on left-click or configured global hotkey (`Ctrl+Shift` or `Alt+Z`), arbitrates start-order priority with running EVKey instances so whichever IME was started first handles `Ctrl+Shift` until closed, and exposes a popup context menu to switch input method, open the Settings app, or exit.

#### Scenario: Global hotkey toggles between Vietnamese and English modes
- **WHEN** the user presses the configured toggle hotkey (`Alt+Z` or `Ctrl+Shift` release without intervening keys)
- **THEN** the engine flips the active mode between `Vietnamese` and `English`, clears the syllable buffer, updates the System Tray icon between `icon-v.ico` and `icon-e.ico` immediately, and persists the per-app state if Smart Switch is enabled

#### Scenario: Start-order coexistence with EVKey on Ctrl+Shift and typing
- **WHEN** EVKey is running alongside `VietKey.Engine`
- **THEN** if EVKey started before VietKey, VietKey yields `Ctrl+Shift` and typing interception while EVKey remains running and automatically resumes handling `Ctrl+Shift` as soon as EVKey exits; if VietKey started before EVKey, VietKey retains priority on `Ctrl+Shift` and disarms the downstream EVKey chord

#### Scenario: Duplicate engine launch is prevented by single-instance mutex
- **WHEN** a second instance of `VietKey.Engine` is started while `Local\VietKeyEngineMutex` is already held
- **THEN** the second instance exits immediately with code `0` without registering a duplicate keyboard hook or tray icon

#### Scenario: System Tray mouse callback handles left-click toggle and right-click context menu
- **WHEN** the hidden tray message window receives `WM_APP_TRAY` with `WM_LBUTTONUP` or `WM_RBUTTONUP`
- **THEN** `WM_LBUTTONUP` toggles between `Vietnamese` and `English` modes immediately, while `WM_RBUTTONUP` opens the popup context menu with commands for toggling language, switching `Telex`/`VNI`, opening Settings, or exiting the message loop
