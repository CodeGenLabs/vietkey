# Tasks: 0007-implement-ctrl-shift-and-evkey-coexistence

## Wave 1: Implement Ctrl+Shift Hotkey State Machine and EVKey Start-Order Coexistence

- [x] **T1 — Implement `PeerImeArbitrator`, `Ctrl+Shift` release detection in `KeyboardSessionProcessor` & `LowLevelKeyboardHook`, and live UI sync** (`REQ-settings-1`)
  - **Files:**
    - `src/VietKey.Engine/Hook/PeerImeArbitrator.cs`
    - `src/VietKey.Engine/Hook/KeyboardSessionProcessor.cs`
    - `src/VietKey.Engine/Hook/LowLevelKeyboardHook.cs`
    - `src/VietKey.Engine/Program.cs`
    - `src/VietKey.UI/MainWindow.xaml.cs`
    - `tests/VietKey.Core.Tests/Hook/KeyboardSessionProcessorTests.cs`
  - **Done when:** Pressing and releasing `Ctrl+Shift` without intervening keys toggles Vietnamese/English mode, pressing `Ctrl+Shift+<key>` (e.g. `Ctrl+Shift+T`) does not toggle mode, `PeerImeArbitrator` yields when EVKey started before VietKey, takes over immediately when EVKey closes, and retains priority when VietKey started before EVKey, and all unit tests pass.
  - **Verify:** `dotnet test --filter FullyQualifiedName~KeyboardSessionProcessorTests`
