# Tasks: 0003-fix-double-key-undo-auto-restore

## Wave 1: Fix Double-Key Undo with SpellCheck & AutoRestoreEnglish

- [x] **T1 — Collapse undo escape key in `SyllableBuffer` and `VietnameseTransformer` so `té` + `s` + `t` yields `test`** (`REQ-typing-1`)
  - **Files:**
    - `src/VietKey.Core/Typing/SyllableBuffer.cs`
    - `src/VietKey.Core/Typing/VietnameseTransformer.cs`
    - `tests/VietKey.Core.Tests/Typing/VietnameseTransformerTests.cs`
  - **Done when:** Typing `"tess"` yields `"tes"`, `"tesst"` yields `"test"`, `"goood"` yields `"good"`, `"towwn"` yields `"town"`, and `"dddr"` yields `"ddr"` with default `AppConfig.Default` (`SpellCheck = true, AutoRestoreEnglish = true`).
  - **Verify:** `dotnet test --filter FullyQualifiedName~VietnameseTransformerTests`
