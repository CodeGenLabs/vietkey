# Tasks: 0014-allow-vietnamese-in-cjk-half-width-alphanumeric-mode

- [x] **Task 1**: Update `NativeMethods.IsForeignImeActive` and `IsCjkConversionActive` to query `ImmGetDefaultIMEWnd` + `WM_IME_CONTROL` (`IMC_GETOPENSTATUS` and `IMC_GETCONVERSIONMODE`) so CJK Half-width Alphanumeric (`A`) mode allows Vietnamese typing while Hiragana/Katakana/Full-width modes bypass it (`REQ-os-1`).
  - Files: `src/VietKey.Engine/Win32/NativeMethods.cs`, `src/VietKey.UI/MainWindow.xaml.cs`, `tests/VietKey.Core.Tests/Hook/KeyboardSessionProcessorTests.cs`
