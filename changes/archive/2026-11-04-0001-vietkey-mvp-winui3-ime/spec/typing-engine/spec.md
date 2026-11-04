## Purpose

Pure, deterministic Vietnamese syllable composition engine supporting Telex and VNI input methods, free diacritic marking, tone placement styles, phonotactic spellcheck with automatic English raw-key restoration, and word-boundary macro expansion.

## ADDED Requirements

### Requirement: REQ-typing-1 - Telex and VNI syllable composition
The system SHALL transform sequential ASCII keystrokes within an active syllable buffer into precomposed Unicode (NFC) Vietnamese characters according to the selected input method (`Telex` or `VNI`), and SHALL revert the transformation to the literal repeating character when the same modifier key is pressed twice consecutively.

#### Scenario: Telex vowel modification and tone mark
- **WHEN** the input method is `Telex` and the user types the sequence `t`, `i`, `e`, `e`, `n`, `g`, `s`
- **THEN** the syllable buffer produces `tiếng` and computes the minimal backspace-and-replace diff from each intermediate state

#### Scenario: VNI vowel modification and tone mark
- **WHEN** the input method is `VNI` and the user types the sequence `v`, `i`, `e`, `6`, `t`, `5`
- **THEN** the syllable buffer produces `việt`

#### Scenario: Double modifier key undo in Telex
- **WHEN** the input method is `Telex` and the user types `a`, `a`, `a`
- **THEN** `a` followed by `a` produces `â`, and the third `a` reverts the vowel to literal `aa` and disables further `â` transformation for that vowel within the current syllable

#### Scenario: Double crossbar d undo in Telex and VNI
- **WHEN** the user types `d`, `d`, `d` in `Telex` (or `d`, `9`, `9` in `VNI`)
- **THEN** the second keystroke produces `đ` and the third keystroke reverts the buffer to `dd` (or `d9`)

### Requirement: REQ-typing-2 - Free diacritic marking and automatic tone repositioning
The system SHALL allow tone marks and vowel modifiers to be typed at any position after the target base letter within the current syllable, and SHALL automatically reposition the tone mark to the canonical vowel when subsequent vowels or final consonants are appended, respecting the configured tone placement style (`Modern` `oà`/`uý` vs `Classic` `òa`/`úy`).

#### Scenario: Tone typed at the end of a syllable
- **WHEN** the user types `t`, `o`, `a`, `n`, `f` in `Telex`
- **THEN** the tone mark `huyền` is placed on `à` to yield `toàn`

#### Scenario: Tone automatically shifts when a final consonant arrives
- **WHEN** the user types `h`, `o`, `a`, `f` in `Classic` tone mode (`hòa`) and then appends `n`
- **THEN** the tone mark shifts from `ò` to `à` to yield `hoàn`

#### Scenario: Modern vs Classic tone placement on open diphthongs
- **WHEN** the user types `h`, `o`, `a`, `f` with `Modern` tone placement enabled
- **THEN** the buffer produces `hoà`, whereas with `Classic` tone placement enabled the same sequence produces `hòa`

### Requirement: REQ-typing-3 - Vietnamese phonotactic spellcheck and automatic English raw-key restoration
When spellcheck and auto-restore are enabled, the system SHALL validate each active syllable against Vietnamese phonotactic structure (initial consonant, medial vowel, nucleus vowel cluster, final consonant, and tone compatibility with stop finals `c`, `ch`, `p`, `t`) and SHALL automatically restore the syllable to its original raw ASCII keystrokes when the syllable becomes invalid or ends with a word delimiter in an invalid state.

#### Scenario: Typing English word containing Telex modifier keys
- **WHEN** spellcheck auto-restore is enabled and the user types `w`, `i`, `n`, `d`, `o`, `w` followed by Space
- **THEN** the engine restores the raw keystroke sequence so the final output is `window ` instead of `ưindoư `

#### Scenario: Stop final consonant with invalid tone mark
- **WHEN** spellcheck auto-restore is enabled and the user types `m`, `a`, `t`, `f` (`màt` — invalid because final `t` only permits `sắc` or `nặng`) followed by Space
- **THEN** the engine restores the raw ASCII sequence `matf `

#### Scenario: Valid Vietnamese word is preserved
- **WHEN** spellcheck auto-restore is enabled and the user types `n`, `g`, `h`, `i`, `e`, `e`, `n`, `g`, `s` (`nghiếng`) followed by Space
- **THEN** the composed Vietnamese syllable `nghiếng ` is preserved without restoration

### Requirement: REQ-typing-4 - Text expansion macros on word boundaries
When macro expansion is enabled, the system SHALL match the completed token against the configured macro dictionary upon receiving a word-boundary delimiter (`Space` or `Enter`) and SHALL replace the token with its expansion text before emitting the delimiter.

#### Scenario: Exact macro trigger on Space
- **WHEN** a macro mapping `vn` to `Việt Nam` is configured and the user types `v`, `n` followed by `Space`
- **THEN** the engine deletes the 2 characters `vn` and emits `Việt Nam `

#### Scenario: Non-matching word does not trigger macro
- **WHEN** a macro mapping `vn` to `Việt Nam` is configured and the user types `v`, `n`, `a` followed by `Space`
- **THEN** no macro replacement occurs and `vna ` is emitted as typed

## Reference parity - EVKey / UniKey typing engine

- Telex and VNI input methods with Unicode NFC output - ADDED, REQ-typing-1
- Double-key undo (`aa` -> `â` -> `aa`, `dd` -> `đ` -> `dd`) - ADDED, REQ-typing-1
- Free tone/mark placement anywhere in syllable & dynamic tone shifting - ADDED, REQ-typing-2
- Modern (`oà`/`uý`) and Classic (`òa`/`úy`) tone placement styles - ADDED, REQ-typing-2
- Phonotactic Vietnamese spellcheck & English raw-key auto-restore - ADDED, REQ-typing-3
- Text expansion macros on word boundary - ADDED, REQ-typing-4
- Legacy 8-bit/16-bit charsets (TCVN3, VNI Windows, VIQR) - deferred: Unicode NFC covers modern Windows 11 apps; legacy charsets are a declared non-goal in `product.md`
- Clipboard charset conversion tool - deferred: out of scope for core typing MVP per `product.md`
