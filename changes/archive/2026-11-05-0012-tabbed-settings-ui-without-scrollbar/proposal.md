# Change 0012: Optimize Settings UI into Tabs Without Vertical Scrollbar

## Intent & Problem Statement
Previously, `DesktopSettingsWindow` (`src/VietKey.UI/MainWindow.xaml`) stacked all four settings cards inside a tall (`Height="810"`) single-column `ScrollViewer`, requiring a vertical scrollbar to reach the Macros and Live Test sections. The user requested refactoring the Settings UI into a tabbed layout so that the vertical scrollbar is completely eliminated.

## Scope & Approach
1. **Replace `ScrollViewer` with a Modern Windows 11 `TabControl` (`src/VietKey.UI/MainWindow.xaml`):**
   - Remove the outer `ScrollViewer` and reduce the window height to a compact `545px` (`MinHeight="510"`).
   - Organize settings into 4 focused tabs with custom Windows 11 pill/card tab styling:
     1. **`Thiết lập chung`**: Input mode (`VietnameseToggle`), input method (`InputMethodCombo`), toggle hotkey (`HotkeyCombo`), and Windows startup options (`StartupToggle`, `HideDialogOnStartupToggle`).
     2. **`Chính tả & Thông minh`**: Modern tone placement (`ModernToneToggle`), spellcheck (`SpellCheckToggle`), English auto-restore (`AutoRestoreEnglishToggle`), Smart Switch (`SmartSwitchToggle`), and browser autocomplete fix (`BrowserFixToggle`).
     3. **`Bảng gõ tắt (Macros)`**: Macro toggle (`MacrosToggle`), trigger/expansion editor (`MacroTriggerInput`, `MacroExpansionInput`, `AddMacroButton`, `RemoveMacroButton`), and a responsive `MacrosListBox`.
     4. **`Gõ thử nghiệm`**: Multi-line `LiveSandboxTextBox` for testing Vietnamese typing immediately.
2. **Preserve All Control Names & Bindings:**
   - Retain all existing control `x:Name`s and event handlers in `DesktopSettingsWindow` (`src/VietKey.UI/MainWindow.xaml.cs`).
