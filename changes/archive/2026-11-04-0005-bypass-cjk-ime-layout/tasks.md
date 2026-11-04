# Tasks: 0005-bypass-cjk-ime-layout

## Wave 1: Bypass Vietnamese Transformation When Japanese / CJK IME Is Active

- [x] **T1 — Detect CJK IME keyboard layout (`0x0411` Japanese, `0x0412` Korean, `0x0404`/`0x0804` Chinese) and bypass Vietnamese composition** (`REQ-os-1`)
  - **Files:**
    - `src/VietKey.Engine/Win32/NativeMethods.cs`
    - `src/VietKey.Engine/Hook/KeyboardSessionProcessor.cs`
    - `src/VietKey.Engine/Program.cs`
    - `src/VietKey.UI/MainWindow.xaml.cs`
    - `tests/VietKey.Core.Tests/Hook/KeyboardSessionProcessorTests.cs`
  - **Done when:** `NativeMethods.IsCjkKeyboardLayout` identifies Japanese (`0x0411`), Korean (`0x0412`), and Chinese (`0x0804`) HKLs, `KeyboardSessionProcessor.ProcessKeyDown` passes `k`, `a`, `n`, `j`, `i` through untouched with zero dispatched batches when Japanese IME is active on the foreground window, and resumes Vietnamese typing when switched back to English (`0x0409`).
  - **Verify:** `dotnet test --filter FullyQualifiedName~KeyboardSessionProcessorTests`
