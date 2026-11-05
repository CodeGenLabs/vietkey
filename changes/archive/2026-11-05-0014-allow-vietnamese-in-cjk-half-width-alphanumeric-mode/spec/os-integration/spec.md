# os-integration

## MODIFIED Requirements

### Requirement: REQ-os-1 - Non-blocking low-level keyboard hook and single-batch Unicode injection
The system SHALL intercept physical keyboard events via `WH_KEYBOARD_LL`, ignore synthetic events bearing the `LLKHF_INJECTED` flag to prevent feedback loops, bypass Vietnamese transformation when the active foreground keyboard layout is a CJK IME (Japanese, Korean, or Chinese) with active native/kana/full-width composition (`あ` Hiragana, `カ`/`ｶ` Katakana, `Ａ` Full-width Alphanumeric) while allowing Vietnamese transformation when the CJK IME is in Half-width Alphanumeric (`A` / Direct Input) mode, and emit all required backspace events (`VK_BACK`) and replacement UTF-16 characters (`KEYEVENTF_UNICODE`) in a single atomic `SendInput` batch while keeping per-keystroke processing overhead free of disk I/O or uncached process queries.

#### Scenario: Transformed character emits atomic backspace and Unicode replacement batch
- **WHEN** the buffer contains `a` and the user presses `s` in `Telex` mode
- **THEN** the engine suppresses the physical `s` keystroke and dispatches a single `SendInput` array containing 1 `VK_BACK` (down/up) followed by `á` (`U+00E1` Unicode down/up)

#### Scenario: Synthetic injected events are passed through untouched
- **WHEN** `LowLevelKeyboardProc` receives a key event with `LLKHF_INJECTED` set
- **THEN** the hook immediately passes the event to `CallNextHookEx` without mutating the syllable buffer

#### Scenario: Active Japanese or CJK IME in Hiragana/Katakana/Full-width mode bypasses Vietnamese transformation while Half-width Alphanumeric ('A') mode allows Vietnamese typing
- **WHEN** the active foreground window thread uses a Japanese (`0x0411`), Korean (`0x0412`), or Chinese (`0x0404`/`0x0804`) IME keyboard layout
- **THEN** the engine bypasses Vietnamese transformation when the IME is in active composition mode (`あ` Hiragana, `カ`/`ｶ` Katakana, `Ａ` Full-width Alphanumeric, `IMC_GETOPENSTATUS != 0`) so `kanji` (`漢字`) is not altered, and enables normal Vietnamese transformation when the IME is in Half-width Alphanumeric (`A`) mode (`IMC_GETOPENSTATUS == 0` or alphanumeric conversion mode)

#### Scenario: Modifier shortcuts and navigation keys reset the syllable buffer
- **WHEN** the user presses a navigation key (`Left`, `Right`, `Home`, `End`, `Up`, `Down`) or a `Ctrl`/`Alt`/`Win` shortcut while a syllable buffer is active
- **THEN** the syllable buffer is cleared and the key event passes through unmodified
