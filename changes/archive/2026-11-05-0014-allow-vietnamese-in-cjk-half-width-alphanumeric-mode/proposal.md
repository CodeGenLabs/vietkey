# Change 0014: Allow Vietnamese Typing in CJK Half-Width Alphanumeric ('A') Mode

## Intent & Problem Statement
Previously, `NativeMethods.IsForeignImeActive` bypassed Vietnamese transformation whenever the foreground window's keyboard layout (`HKL`) was a CJK language (`0x0411` Japanese, `0x0412` Korean, `0x0404`/`0x0804` Chinese), regardless of the IME's active conversion/open state. As a result, when a user kept the Windows Japanese IME selected on the taskbar in **Half-width Alphanumeric (`A`)** mode (which acts as direct English/Latin input), `VietKey` still bypassed Vietnamese composition and prevented typing Vietnamese.

## Scope & Approach
1. **Query IME Open Status & Conversion Mode (`src/VietKey.Engine/Win32/NativeMethods.cs`):**
   - Add `ImmGetDefaultIMEWnd` (`imm32.dll`) and `SendMessageTimeoutW` (`WM_IME_CONTROL` `0x0283` with `IMC_GETOPENSTATUS = 0x0005` and `IMC_GETCONVERSIONMODE = 0x0001`).
   - Add `NativeMethods.IsCjkConversionActive(nint hkl, nint imeOpenStatus, nint imeConversionMode = 0)` so that when a CJK layout is in **Half-width Alphanumeric (`A`)** mode (`imeOpenStatus == 0`, or `imeConversionMode != 0` with none of `IME_CMODE_NATIVE | IME_CMODE_KATAKANA | IME_CMODE_FULLSHAPE` set), `IsForeignImeActive` returns `false` and allows normal Vietnamese typing.
   - Continue bypassing Vietnamese typing when the CJK IME is in active Kana/Kanji/Full-width mode (`あ` Hiragana, `カ` Full-width Katakana, `Ａ` Full-width Alphanumeric, `ｶ` Half-width Katakana, `中`, `가`).
2. **Sync `VietKey.UI` Sandbox Check (`src/VietKey.UI/MainWindow.xaml.cs`):**
   - Update `IsCurrentThreadForeignIme()` to inspect `System.Windows.Input.InputMethod.Current.ImeState` and `ImeConversionMode` so the Live Test sandbox also allows Vietnamese typing in Half-width Alphanumeric (`A`) mode.
