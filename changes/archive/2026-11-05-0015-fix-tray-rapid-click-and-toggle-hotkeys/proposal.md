# Change 0015: Fix System Tray Rapid Clicks Opening Settings and Restore Global Toggle Hotkeys

## Intent & Problem Statement
The user reported two issues:
1. Clicking the System Tray icon rapidly to toggle `E -> V -> E` caused the Settings dialog (`Bảng điều khiển`) to pop open because Windows translates the second click of a fast click sequence into `WM_LBUTTONDBLCLK`, and `TrayIconHost.HandleMouseMessage` invoked `OpenSettingsRequested` on `WM_LBUTTONDBLCLK`.
2. Global language toggle hotkeys (`Ctrl + Shift` and `Alt + Z`) failed to toggle in real usage due to three interacting bugs:
   - `LowLevelKeyboardHook` used `GetKeyState` (which reads the background tray thread's own message-queue key state instead of global physical key state via `GetAsyncKeyState`), causing `altDown` to be `false` during `Alt + Z` in other windows and returning stale `shiftDown`/`ctrlDown` states during `WM_KEYUP`.
   - `KeyboardSessionProcessor` set `_otherKeyPressedDuringModifier = true` on `Alt` (e.g. `Alt + Tab`), `Win`, `Shift + Letter`, or `Ctrl + Key`, and failed to reset `_otherKeyPressedDuringModifier = false` on release because `ProcessKeyUp` ignored `Alt`/`Win` keyups and checked `!shiftDown && !ctrlDown` while the `WM_KEYUP` hook was still in flight. Once `_otherKeyPressedDuringModifier` became `true`, `Ctrl + Shift` could never arm again.
   - `SmartSwitchTracker.RecordUserToggle` recorded `"explorer.exe"` when toggling via the System Tray or failed to update `CurrentExecutable`, causing `SmartSwitch` to revert the mode when returning to the active application.

## Scope & Approach
1. **System Tray Left-Click Only Toggles Language (`src/VietKey.Engine/Tray/TrayIconHost.cs`):**
   - Remove `OpenSettingsRequested?.Invoke()` from `WM_LBUTTONDBLCLK` in `TrayIconHost.HandleMouseMessage` so rapid left-clicks only toggle `V <-> E` (Settings remains accessible via the right-click context menu).
2. **Global Modifier Tracking & Hotkey State Machine (`src/VietKey.Engine/Win32/NativeMethods.cs`, `src/VietKey.Engine/Hook/LowLevelKeyboardHook.cs`, `src/VietKey.Engine/Hook/KeyboardSessionProcessor.cs`, `src/VietKey.Core/Injection/SmartSwitchTracker.cs`):**
   - Use `GetAsyncKeyState` in `LowLevelKeyboardHook`.
   - Track `_shiftPressed`, `_ctrlPressed`, `_altPressed`, and `_winPressed` in `KeyboardSessionProcessor`, resetting `_otherKeyPressedDuringModifier` cleanly when starting a fresh `Ctrl`/`Shift` chord and when modifiers are released.
   - Update `SmartSwitchTracker` (`RecordUserToggle` and `SyncGlobalMode`) so toggling via Hotkey, System Tray (`explorer.exe`), or `VietKey.UI` updates the active application's remembered language state.
