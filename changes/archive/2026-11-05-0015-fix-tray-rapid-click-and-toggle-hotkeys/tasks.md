# Tasks: 0015-fix-tray-rapid-click-and-toggle-hotkeys

- [x] **Task 1**: Ignore `WM_LBUTTONDBLCLK` in `TrayIconHost.HandleMouseMessage` so rapid tray left-clicks only toggle `V <-> E` without opening Settings (`REQ-settings-1`).
  - Files: `src/VietKey.Engine/Tray/TrayIconHost.cs`, `tests/VietKey.Core.Tests/Hook/KeyboardSessionProcessorTests.cs`
- [x] **Task 2**: Use `GetAsyncKeyState` in `LowLevelKeyboardHook`, fix `_otherKeyPressedDuringModifier` and `Alt`/`Win` tracking in `KeyboardSessionProcessor`, and sync `SmartSwitchTracker` on user/global toggles so `Ctrl+Shift` and `Alt+Z` work reliably at all times (`REQ-settings-1`).
  - Files: `src/VietKey.Engine/Win32/NativeMethods.cs`, `src/VietKey.Engine/Hook/LowLevelKeyboardHook.cs`, `src/VietKey.Engine/Hook/KeyboardSessionProcessor.cs`, `src/VietKey.Core/Injection/SmartSwitchTracker.cs`, `tests/VietKey.Core.Tests/Hook/KeyboardSessionProcessorTests.cs`
