# Proposal: 0007-implement-ctrl-shift-and-evkey-coexistence

## Why

1. When the user selects `Ctrl + Shift` as the Vietnamese/English toggle hotkey, pressing `Ctrl + Shift` does not toggle the language mode because `KeyboardSessionProcessor` and `LowLevelKeyboardHook` only handled `WM_KEYDOWN` for `Alt + Z` and ignored modifier key release chords (`Ctrl + Shift` on `WM_KEYUP`).
2. Furthermore, when EVKey (`EVKey64.exe`, `EVKey32.exe`, `EVKey.exe`) is used alongside VietKey on the same machine, the two IMEs conflict on `Ctrl + Shift`. The user requested start-order priority coexistence:
   - If VietKey is opened **after** EVKey (while EVKey is still running), EVKey retains priority and handles `Ctrl + Shift` and typing while VietKey yields.
   - If VietKey is opened **before** EVKey, VietKey retains priority (refreshing its `WH_KEYBOARD_LL` hook to the front of the chain and disarming downstream `Ctrl + Shift` chords) so VietKey handles `Ctrl + Shift`.
   - If VietKey was opened **after** EVKey, once EVKey is closed/exited, VietKey automatically takes over and handles `Ctrl + Shift` and typing without needing a restart.

## What Changes

- **Peer IME Coexistence Arbitrator (`src/VietKey.Engine/Hook/PeerImeArbitrator.cs`):**
  - Track running EVKey processes (`EVKey64`, `EVKey32`, `EVKey`) relative to `VietKey.Engine` startup order (`StartTimeUtc` and initial PID snapshot).
  - Yield when an older EVKey instance is active, automatically resume when the older EVKey instance exits, and assert priority (re-hooking `WH_KEYBOARD_LL` to the front and dispatching a neutral `0xFF` chord-breaker) when VietKey was started before EVKey.
- **`Ctrl + Shift` Chord State Machine (`src/VietKey.Engine/Hook/KeyboardSessionProcessor.cs`, `LowLevelKeyboardHook.cs`, `Program.cs`):**
  - Track `Ctrl` and `Shift` press/release state across `ProcessKeyDown` and `ProcessKeyUp`, arming the chord when both `Ctrl` and `Shift` are held without `Alt`/`Win` or any intervening non-modifier key, and toggling mode on modifier `WM_KEYUP`.
  - Persist `ConfigStore.Save` whenever the hotkey toggles Vietnamese/English mode.
- **Immediate Live Sync in Settings Window (`src/VietKey.UI/MainWindow.xaml.cs`):**
  - Auto-save and signal `VietKey.Engine` immediately when any control (such as the `Ctrl + Shift` hotkey dropdown) changes in `DesktopSettingsWindow`, and poll `ConfigStore` while the window is open so pressing `Ctrl + Shift` updates the window's `V`/`E` badge and toggle checkbox in real time.

## Capabilities

- **Modified:**
  - `docs/system/specs/settings-and-tray/spec.md`

## Claims touched

- None.
