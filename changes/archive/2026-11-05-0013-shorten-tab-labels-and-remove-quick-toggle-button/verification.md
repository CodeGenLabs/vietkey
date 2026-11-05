# Verification: 0013-shorten-tab-labels-and-remove-quick-toggle-button

- Verified `QuickToggleModeButton` is removed from `src/VietKey.UI/MainWindow.xaml` and `src/VietKey.UI/MainWindow.xaml.cs`, giving full width to `StatusTitleText` and `StatusSubtitleText`.
- Verified tab headers are shortened to `Cài đặt chung`, `Chính tả`, `Gõ tắt`, and `Gõ thử` with `FocusVisualStyle="{x:Null}"`, and all 121 unit tests pass.
