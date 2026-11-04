# typing-engine

## MODIFIED Requirements

### Requirement: REQ-typing-1 - Telex and VNI syllable composition
The system SHALL transform sequential ASCII keystrokes within an active syllable buffer into precomposed Unicode (NFC) Vietnamese characters according to the selected input method (`Telex` or `VNI`), and SHALL revert the transformation to the literal repeating character when the same modifier key is pressed twice consecutively without duplicating the undo escape keystroke when spellcheck auto-restore is active.

#### Scenario: Telex vowel modification and tone mark
- **WHEN** the input method is `Telex` and the user types the sequence `t`, `i`, `e`, `e`, `n`, `g`, `s`
- **THEN** the syllable buffer produces `tiếng` and computes the minimal backspace-and-replace diff from each intermediate state

#### Scenario: VNI vowel modification and tone mark
- **WHEN** the input method is `VNI` and the user types the sequence `v`, `i`, `e`, `6`, `t`, `5`
- **THEN** the syllable buffer produces `việt`

#### Scenario: Double modifier key undo in Telex with spellcheck enabled
- **WHEN** the input method is `Telex` with spellcheck auto-restore enabled and the user types `t`, `e`, `s`, `s`, `t` (or `a`, `a`, `a`)
- **THEN** `t`, `e`, `s` produces `té`, the second `s` reverts `té` to literal `tes` (not `tess`), and appending `t` produces `test` (not `tesst`)

#### Scenario: Double crossbar d undo in Telex and VNI
- **WHEN** the user types `d`, `d`, `d` in `Telex` (or `d`, `9`, `9` in `VNI`)
- **THEN** the second keystroke produces `đ` and the third keystroke reverts the buffer to `dd` (or `d9`)
