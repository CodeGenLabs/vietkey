# Proposal: 0005-bypass-cjk-ime-layout

## Why

When the user switches the Windows 11 input method (`Win + Space` / `Alt + Shift`) to Japanese IME (`0x0411`, or Korean `0x0412` / Chinese `0x0404`, `0x0804`) and types Romaji such as `kanji` (`漢字`), `VietKey`'s `WH_KEYBOARD_LL` hook intercepted `k`, `a`, `n`, `j` before the Japanese IME completed composition. Upon pressing `j` (Telex `nặng`), `VietKey` emitted 2 backspaces (deleting `かん`) and injected `"ạn"`. We need `VietKey` to detect when a CJK IME keyboard layout is active on the focused/foreground thread and bypass Vietnamese composition completely.

## What Changes

- **Detect Active CJK IME Keyboard Layout (`src/VietKey.Engine/Win32/NativeMethods.cs`):**
  - Add `GetGUIThreadInfo` and `GetKeyboardLayout` P/Invoke bindings plus `IsCjkKeyboardLayout(nint hkl)` and `IsForeignImeActive(nint foregroundHwnd)` to detect Japanese (`LANG_JAPANESE = 0x11`), Korean (`LANG_KOREAN = 0x12`), and Chinese (`LANG_CHINESE = 0x04`) keyboard layouts on the focused/foreground window thread.
- **Bypass Composition When Foreign CJK IME Is Active (`src/VietKey.Engine/Hook/KeyboardSessionProcessor.cs`, `src/VietKey.Engine/Program.cs`, `src/VietKey.UI/MainWindow.xaml.cs`):**
  - Inject `Func<nint, bool>? foreignImeChecker` into `KeyboardSessionProcessor` (wired to `NativeMethods.IsForeignImeActive` in `Program.cs`). When `true` (or when `virtualKey == 0xE5` `VK_PROCESSKEY`), clear the syllable buffer and pass all keystrokes through unmodified so Japanese (`漢字`), Korean, and Chinese IME input works natively.

## Capabilities

- **Modified:**
  - `docs/system/specs/os-integration/spec.md`

## Claims touched

- None.
