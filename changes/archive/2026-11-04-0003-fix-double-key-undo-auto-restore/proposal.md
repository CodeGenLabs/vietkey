# Proposal: 0003-fix-double-key-undo-auto-restore

## Why

When typing `t`, `e`, `s` (`té`) and pressing `s` again to undo the acute accent (e.g., to type `tes` or `test`) with `SpellCheck` and `AutoRestoreEnglish` enabled, `SyllableBuffer.RawKeys` retained both `s` keystrokes (`"tess"`). Because `"tes"` is not a valid Vietnamese syllable, `ShouldAutoRestoreOnKeystroke` immediately replaced `"tes"` with `_buffer.GetRawString()` (`"tess"`), causing `t` + `e` + `s` + `s` + `t` to become `"tesst"` instead of `"test"`.

## What Changes

- **Collapse Undo Escape Key in `SyllableBuffer` & `VietnameseTransformer` (`src/VietKey.Core/Typing/`):**
  - Add `RemoveLastRawKey()` to `SyllableBuffer` and invoke it whenever a double-key undo (`ss/ff/rr/xx/jj`, `aa/ee/oo`, `dd`, `ww`, or VNI `11..99`) reverts a modifier back to its literal character, ensuring `_buffer.GetRawString()` matches the intended literal text (`"tes"`, `"test"`, `"good"`, `"town"`, `"ddr"`) without duplicating the escape keystroke.
  - Suppress further composition in the syllable when a tone, horn/breve `w`, or crossbar `d` double-key undo occurs so subsequent letters (`t` in `test`, `s` in `tests`) pass through cleanly as literal characters.

## Capabilities

- **Modified:**
  - `docs/system/specs/typing-engine/spec.md`

## Claims touched

- None.
