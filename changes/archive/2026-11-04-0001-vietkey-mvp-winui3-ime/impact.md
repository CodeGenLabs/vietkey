# Impact — 0001-vietkey-mvp-winui3-ime

## Blast radius

- `docs/system/OVERVIEW.md` (updated system overview)
- `docs/system/product.md` (updated product intent and reference products)
- `VietKey.sln` (new solution)
- `src/VietKey.Core/**` (new pure C# typing engine, spellchecker, macros, config store)
- `src/VietKey.Engine/**` (new Win32 background hook, `SendInput` batch injector, foreground app tracker, tray icon host)
- `src/VietKey.UI/**` (new on-demand WinUI 3 settings app)
- `tests/VietKey.Core.Tests/**` (new xUnit test suite covering `REQ-typing-*`, `REQ-os-*`, `REQ-settings-*`)

## Claims touched

### Unaffected
- None (no pre-existing ratified claims in the repository).

### Updated
- None.

### New
- `ARC-dual-process-isolation`: `VietKey.Engine` runs the `WH_KEYBOARD_LL` hook and Win32 system tray without loading WinUI 3 / XAML DLLs, while `VietKey.UI` runs on-demand in a separate process and exits completely on window close.
- `INV-hook-nonblocking`: `LowLevelKeyboardProc` ignores synthetic `LLKHF_INJECTED` keystrokes and never performs disk I/O or uncached process path lookups on the hot keystroke path.
- `INV-atomic-sendinput`: Character replacements emit all `VK_BACK` and `KEYEVENTF_UNICODE` events in a single atomic `SendInput` array call, prepending an autocomplete-neutralizing sequence when the foreground window matches a Chromium browser profile.

### Superseded
- None.

### At risk
- None.
