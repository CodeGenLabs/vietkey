# Verification: 0014-allow-vietnamese-in-cjk-half-width-alphanumeric-mode

- Verified `NativeMethods.IsCjkConversionActive` and `IsForeignImeActive` allow Vietnamese typing when a Japanese/CJK IME is in Half-width Alphanumeric (`A`) mode (`IMC_GETOPENSTATUS == 0` or `IME_CMODE_ROMAN` without Native/Katakana/FullShape), while bypassing Vietnamese transformation when in Hiragana (`あ`), Full-width Katakana (`カ`), Full-width Alphanumeric (`Ａ`), or Half-width Katakana (`ｶ`) mode.
- Verified `dotnet test --nologo` passes all 121 unit tests (`ProcessKeyDown_WhenJapaneseOrCjkImeIsActive_BypassesHiraganaButAllowsHalfWidthAlphanumeric`).
