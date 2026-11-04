# os-integration

## Purpose

Low-latency Windows 11 keyboard interception and atomic Unicode character injection with per-application compatibility profiles for Chromium browsers, Electron apps, and Microsoft Word, alongside Smart Switch per-app input state tracking.

### Requirement: REQ-os-1 - Non-blocking low-level keyboard hook and single-batch Unicode injection
The system SHALL intercept physical keyboard events via `WH_KEYBOARD_LL`, ignore synthetic events bearing the `LLKHF_INJECTED` flag to prevent feedback loops, and emit all required backspace events (`VK_BACK`) and replacement UTF-16 characters (`KEYEVENTF_UNICODE`) in a single atomic `SendInput` batch while keeping per-keystroke processing overhead free of disk I/O or uncached process queries.

#### Scenario: Transformed character emits atomic backspace and Unicode replacement batch
- **WHEN** the buffer contains `a` and the user presses `s` in `Telex` mode
- **THEN** the engine suppresses the physical `s` keystroke and dispatches a single `SendInput` array containing 1 `VK_BACK` (down/up) followed by `á` (`U+00E1` Unicode down/up)

#### Scenario: Synthetic injected events are passed through untouched
- **WHEN** `LowLevelKeyboardProc` receives a key event with `LLKHF_INJECTED` set
- **THEN** the hook immediately passes the event to `CallNextHookEx` without mutating the syllable buffer

#### Scenario: Modifier shortcuts and navigation keys reset the syllable buffer
- **WHEN** the user presses a navigation key (`Left`, `Right`, `Home`, `End`, `Up`, `Down`) or a `Ctrl`/`Alt`/`Win` shortcut while a syllable buffer is active
- **THEN** the syllable buffer is cleared and the key event passes through unmodified

### Requirement: REQ-os-2 - Chromium, Google Search, Electron, and Word compatibility injection
The system SHALL detect the active foreground process profile using an in-memory HWND-to-process cache and SHALL apply an autocomplete-safe injection sequence for Chromium/browser processes (`chrome.exe`, `msedge.exe`, `brave.exe`, `firefox.exe`) so inline autocomplete suggestions in address bars and search boxes are not deleted instead of the target character, while using direct atomic Unicode batching for Electron (`Code.exe`, `ms-teams.exe`) and Office (`WINWORD.EXE`).

#### Scenario: Chromium address bar and Google Search inline autocomplete protection
- **WHEN** the active foreground window belongs to a Chromium browser profile (`chrome.exe` or `msedge.exe`) and a keystroke requires replacing `N >= 1` previous characters
- **THEN** the injector prepends an autocomplete-neutralizing sequence (injecting a non-autocomplete dummy Unicode character `U+200B`/`U+200C` plus an extra `VK_BACK` in the same `SendInput` batch) before the `N` backspaces and replacement text so the browser's highlighted suggestion is cleared before deleting the composed character

#### Scenario: VS Code, Teams, and Word receive direct atomic Unicode batches without extra dummy characters
- **WHEN** the active foreground window belongs to `Code.exe`, `ms-teams.exe`, or `WINWORD.EXE` and a keystroke requires replacing `N >= 1` previous characters
- **THEN** the injector emits exactly `N` `VK_BACK` key pairs followed by the UTF-16 replacement key pairs in one `SendInput` call with zero artificial sleep delay

### Requirement: REQ-os-3 - Smart Switch per-application input language state
When Smart Switch is enabled, the system SHALL remember the last user-selected input mode (`Vietnamese` vs `English`) per foreground executable name and SHALL automatically switch to that mode and clear the active syllable buffer whenever the foreground window changes to a different application.

#### Scenario: Switching from Chrome (Vietnamese) to VS Code (English) and back
- **WHEN** Smart Switch is enabled, the user sets `chrome.exe` to `Vietnamese`, switches focus to `Code.exe` and toggles input mode to `English`, and later switches focus back to `chrome.exe`
- **THEN** the engine automatically restores `Vietnamese` mode upon focusing `chrome.exe` and `English` mode upon focusing `Code.exe`

#### Scenario: Unknown application defaults to current global input mode
- **WHEN** Smart Switch is enabled and focus moves to an executable not yet recorded in the per-app state map
- **THEN** the engine retains the current active input mode and records that executable's state when the user next toggles it
