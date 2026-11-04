# Tasks: 0004-preserve-vietnamese-crossbar-d-shorthand

## Wave 1: Preserve `đ` Consonant Shorthand (`đc`, `đk`, `đt`, `đ`) and Macro Triggers

- [x] **T1 — Preserve `đ`-initial consonant shorthand in `VietnameseSpellChecker` and macro prefixes in `VietnameseTransformer`** (`REQ-typing-3`)
  - **Files:**
    - `src/VietKey.Core/Typing/VietnameseSpellChecker.cs`
    - `src/VietKey.Core/Typing/VietnameseTransformer.cs`
    - `tests/VietKey.Core.Tests/Typing/VietnameseSpellCheckerTests.cs`
    - `tests/VietKey.Core.Tests/Typing/MacroTableTests.cs`
  - **Done when:** Typing `"ddc "` or `"dcd "` with `SpellCheck = true, AutoRestoreEnglish = true` yields `"đc "` (and expands `"đc"` macro if configured), while `"add "` and `"address "` still auto-restore to English.
  - **Verify:** `dotnet test --filter "FullyQualifiedName~VietnameseSpellCheckerTests|FullyQualifiedName~MacroTableTests"`
