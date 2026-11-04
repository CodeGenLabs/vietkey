# System overview

**VietKey** is a lightweight, native Vietnamese Input Method (IME) for Windows 11 built on .NET 8/9 and WinUI 3. It is designed for daily Vietnamese typing across modern desktop, Electron, and browser applications (Chrome, Google Search, VS Code, Microsoft Word, Microsoft Teams) with sub-millisecond input latency, minimal background RAM consumption, and zero cloud/database dependencies.

## Core Architecture Facts

1. **Dual-Process Split for Low RAM (`VietKey.Engine` + `VietKey.UI`):**
   - `VietKey.Engine` is a headless, NativeAOT-ready Win32 background daemon (~3–8 MB working set) hosting the pure Vietnamese composition state machine (`VietKey.Core`), the Win32 low-level keyboard hook (`WH_KEYBOARD_LL`), the `SendInput` batch injector with per-application compatibility profiles, and the native Win32 System Tray icon (`Shell_NotifyIconW`).
   - `VietKey.UI` is a standalone WinUI 3 settings process launched on-demand when the user opens Settings from the tray menu or hotkey, and terminated completely on window close so XAML/DirectComposition memory is never held while idle.
2. **Pure, Deterministic Core (`VietKey.Core`):**
   - Zero OS/Win32 dependencies. Handles Telex/VNI syllable parsing, free-mark placement, old/new tone placement (`òa`/`oà`), Vietnamese phonotactic spellcheck with automatic English raw-key restoration, and text expansion macros.
3. **File-Only Local Persistence (`%AppData%\VietKey\config.json` & `macros.json`):**
   - No database, no background network service, and no COM/TSF system-wide DLL injection. Synchronised between `VietKey.UI` and `VietKey.Engine` via atomic file writes and a named Win32 event (`Local\VietKeyConfigReloadEvent`).
