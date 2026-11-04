# typing-engine

## MODIFIED Requirements

### Requirement: REQ-typing-3 - Vietnamese phonotactic spellcheck and automatic English raw-key restoration
When spellcheck and auto-restore are enabled, the system SHALL validate each active syllable against Vietnamese phonotactic structure (initial consonant, medial vowel, nucleus vowel cluster, final consonant, and tone compatibility with stop finals `c`, `ch`, `p`, `t`), SHALL preserve common `đ`-initial consonant-only Vietnamese shorthand tokens (`đ`, `đc`, `đk`, `đt`, `đh`, `đv`, etc.) and active macro triggers, and SHALL automatically restore the syllable to its original raw ASCII keystrokes when the syllable becomes invalid or ends with a word delimiter in an invalid state.

#### Scenario: Typing English word containing Telex modifier keys
- **WHEN** spellcheck auto-restore is enabled and the user types `w`, `i`, `n`, `d`, `o`, `w` followed by Space
- **THEN** the engine restores the raw keystroke sequence so the final output is `window ` instead of `ưindoư `

#### Scenario: Stop final consonant with invalid tone mark
- **WHEN** spellcheck auto-restore is enabled and the user types `m`, `a`, `t`, `f` (`màt` — invalid because final `t` only permits `sắc` or `nặng`) followed by Space
- **THEN** the engine restores the raw ASCII sequence `matf `

#### Scenario: Valid Vietnamese word and crossbar-d shorthand are preserved
- **WHEN** spellcheck auto-restore is enabled and the user types `n`, `g`, `h`, `i`, `e`, `e`, `n`, `g`, `s` (`nghiếng`) or `d`, `d`, `c` (`đc`) followed by Space
- **THEN** the composed Vietnamese output `nghiếng ` and `đc ` are preserved without reverting `đc` to `ddc`
