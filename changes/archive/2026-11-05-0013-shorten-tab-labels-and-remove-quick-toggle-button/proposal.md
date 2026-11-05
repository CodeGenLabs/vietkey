# Change 0013: Shorten Tab Labels and Remove Header Quick Toggle Button

## Intent & Problem Statement
In the tabbed Settings UI (`0012`), two visual issues were observed:
1. The header status text (`"Đang bật chế độ Tiếng Việt (V)"` and `"Kiểu gõ: Telex — Nhẹ, mượt, không độ trễ"`) was clipped on the right because the `Chuyển V / E` button (`QuickToggleModeButton`) occupied horizontal space in the header card.
2. The second tab title (`"Chính tả & Thông minh"`) was clipped inside the `UniformGrid` tab strip (`"Chính tả & Thông"`).

The user requested:
- Shortening the tab labels so no tab text is truncated.
- Removing the `Chuyển V / E` quick toggle button from the header card completely.

## Scope & Approach
1. **Header Card Cleanup (`src/VietKey.UI/MainWindow.xaml`, `src/VietKey.UI/MainWindow.xaml.cs`):**
   - Remove `QuickToggleModeButton` from the header card in `MainWindow.xaml` and its click handler in `MainWindow.xaml.cs`, keeping the `V`/`E` badge (`ActiveModeIconImage`) on the right and giving `StatusTitleText` / `StatusSubtitleText` ample width.
2. **Concise Tab Headers & Clean Focus Visual (`src/VietKey.UI/MainWindow.xaml`):**
   - Shorten the 4 tab headers to `Cài đặt chung`, `Chính tả`, `Gõ tắt`, and `Gõ thử`.
   - Set `FocusVisualStyle="{x:Null}"` on `TabItem` to eliminate the dotted Win32 focus rectangle.
