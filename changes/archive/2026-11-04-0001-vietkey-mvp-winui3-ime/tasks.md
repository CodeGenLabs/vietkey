## 1. Solution Scaffold & Configuration Domain

- [x] [BLOCKING] chore: scaffold `VietKey.sln` with `src/VietKey.Core`, `src/VietKey.Engine`, `src/VietKey.UI`, `tests/VietKey.Core.Tests`
- [x] Implement `AppConfig` and source-generated JSON `ConfigStore` with safe default fallback for `%AppData%\VietKey\config.json` (REQ-settings-2)
- [x] Implement live reload coordinator using named event signal `Local\VietKeyConfigReloadEvent` (REQ-settings-2)

## 2. Core Vietnamese Composition Engine (`VietKey.Core`)

- [x] [BLOCKING] Implement `SyllableBuffer` and `VietnameseTransformer` for `Telex` vowel modification (`aw`, `aa`, `ee`, `oo`, `ow`, `uw`, `dd`) with tone marks (`s`, `f`, `r`, `x`, `j`, `z`) (REQ-typing-1)
- [x] Add `VNI` input method key mapping (`1`-`9`, `0`) to `VietnameseTransformer` (REQ-typing-1)
- [x] Add double-key modifier undo (`aa` -> `â` -> `aa`, `dd` -> `đ` -> `dd`) with per-syllable suppression lock (REQ-typing-1)
- [x] Implement free diacritic marking anywhere in the active syllable with dynamic tone repositioning as vowels or final consonants arrive (REQ-typing-2)
- [x] Implement `Modern` (`hoà`, `thuý`) vs `Classic` (`hòa`, `thúy`) diphthong tone placement rules (REQ-typing-2)
- [x] Implement `VietnameseSpellChecker` phonotactic validator (initials, medials, vowel clusters, stop-final tone rules) with automatic English raw-key restoration (REQ-typing-3)
- [x] Implement `MacroTable` lookup and word-boundary (`Space`/`Enter`) text expansion in `VietKey.Core` (REQ-typing-4)

## 3. OS Integration, Batch Injection & Smart Switch (`VietKey.Core` & `VietKey.Engine`)

- [x] Implement `InputBatchBuilder` producing single-batch `VK_BACK` plus `KEYEVENTF_UNICODE` packet arrays while ignoring `LLKHF_INJECTED` synthetic events (REQ-os-1)
- [x] Reset active syllable buffer on navigation keys (`Left`, `Right`, `Up`, `Down`, `Home`, `End`), mouse clicks, or modifier shortcuts (REQ-os-1)
- [x] Implement `AppCompatibilityDetector` with HWND-to-process caching and Chromium (`chrome.exe`, `msedge.exe`, `brave.exe`) autocomplete-neutralizing dummy-char injection vs direct batching for `Code.exe`, `ms-teams.exe`, `WINWORD.EXE` (REQ-os-2)
- [x] Implement `SmartSwitchTracker` per-application language state memory on foreground window switches (REQ-os-3)
- [x] Wire `LowLevelKeyboardHook` (`WH_KEYBOARD_LL`) and Win32 `SendInput` P/Invoke dispatcher in `VietKey.Engine` (REQ-os-1)

## 4. System Tray Daemon & On-Demand WinUI 3 Settings (`VietKey.Engine` & `VietKey.UI`)

- [x] Implement single-instance mutex (`Local\VietKeyEngineMutex`), global hotkey toggle (`Alt+Z`, `Ctrl+Shift`), and `Shell_NotifyIconW` V/E System Tray host in `VietKey.Engine` (REQ-settings-1)
- [x] Implement `VietKey.UI` WinUI 3 Settings window for input method, tone style, spellcheck, Smart Switch, Chromium fix, Windows startup registry (`HKCU\...\Run`), and macro dictionary editor (REQ-settings-3)
- [x] Implement on-demand `VietKey.UI.exe` process launch from the tray menu and complete process termination (`Application.Current.Exit()`) upon window close after signaling `Local\VietKeyConfigReloadEvent` (REQ-settings-3)
