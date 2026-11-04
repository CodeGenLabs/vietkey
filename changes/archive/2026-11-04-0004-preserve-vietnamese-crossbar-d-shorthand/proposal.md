# Proposal: 0004-preserve-vietnamese-crossbar-d-shorthand

## Why

When typing common Vietnamese shorthand words starting with `đ` such as `đc` (`d` + `d` + `c` or `d` + `c` + `d`), `đk`, `đt`, `đh`, `đv`, or standalone `đ`, `VietnameseSpellChecker.IsValidSyllable` treated the entire consonant-only sequence `"đc"` as an invalid initial consonant cluster (`!ValidInitials.Contains("đc")`) and rejected tokens with `vowel.Length == 0` on word boundaries, causing `AutoRestoreEnglish` to revert `"đc"` to `"ddc"`. Additionally, `ShouldAutoRestoreOnKeystroke` did not exempt active macro prefixes when macros are enabled.

## What Changes

- **Preserve `đ`-prefixed Vietnamese Consonant Shorthand (`src/VietKey.Core/Typing/VietnameseSpellChecker.cs`):**
  - Treat consonant-only tokens starting with crossbar `đ` (`slots[0].CrossbarD && vowel.Length == 0 && buffer.Tone == ToneMark.None`, e.g., `đ`, `đc`, `đk`, `đt`, `đh`, `đv`, `đn`, `đm`, `đg`, `đb`, `đl`, `đp`, `đs`) as valid Vietnamese shorthand both during keystroke composition and at word boundaries, while still restoring to raw ASCII if a vowel follows an invalid `đ+consonant` cluster (e.g., `ddca` or `ddram`).
- **Exempt Macro Prefixes from Premature Auto-Restore (`src/VietKey.Core/Typing/VietnameseTransformer.cs`):**
  - Check whether `newDisplayed` or `_buffer.GetRawString()` matches or is a prefix of a configured macro in `ShouldAutoRestoreOnKeystroke` so diacritic macro triggers (`đc` -> `được`) are never reverted before reaching the word boundary.

## Capabilities

- **Modified:**
  - `docs/system/specs/typing-engine/spec.md`

## Claims touched

- None.
