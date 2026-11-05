# settings-and-tray

## MODIFIED Requirements

### Requirement: REQ-settings-2 - Local JSON configuration persistence and zero-downtime live reload
The system SHALL store all user settings (`config.json`) and macros (`macros.json`) as human-readable UTF-8 JSON files under `%AppData%\VietKey\` without any database or network dependency, SHALL synchronize cross-process reads and writes so rapid consecutive saves and concurrent live reloads never throw `UnauthorizedAccessException` or `IOException`, and `VietKey.Engine` SHALL reload configuration in memory whenever the named event `Local\VietKeyConfigReloadEvent` is signaled.

#### Scenario: Missing or corrupted configuration file falls back to safe defaults
- **WHEN** `VietKey.Engine` starts and `%AppData%\VietKey\config.json` does not exist or contains malformed JSON
- **THEN** the engine initializes with default settings (`InputMethod=Telex`, `VietnameseEnabled=true`, `ModernTone=true`, `SpellCheck=true`, `AutoRestoreEnglish=true`, `SmartSwitch=true`, `BrowserAutocompleteFix=true`, `ToggleHotkey=AltZ`) and writes a valid default `config.json`

#### Scenario: Live reload upon named event signal
- **WHEN** `VietKey.UI` saves updated settings to `%AppData%\VietKey\config.json` and signals `Local\VietKeyConfigReloadEvent`
- **THEN** `VietKey.Engine` reloads the new settings into memory within 50 ms without dropping the keyboard hook or restarting the process

#### Scenario: Rapid consecutive E/V mode toggles and concurrent reload reads complete without file lock contention
- **WHEN** the user rapidly toggles `E -> V -> E` in `VietKey.UI` while `VietKey.Engine` (`ConfigReloadCoordinator`) concurrently reloads `config.json` and `macros.json`
- **THEN** `QuickToggleModeButton` saves once per click, `ConfigStore` serializes cross-process file access safely without throwing `UnauthorizedAccessException` or `IOException`, and `VietKey.Engine` converges on the latest configuration state
