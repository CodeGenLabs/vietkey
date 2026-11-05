# Proposal: 0009-native-gdi-dpi-and-theme-aware-tray-icons

## Why

The static `.ico` System Tray icons (`icon-v.ico` / `icon-e.ico`) suffered from two visual problems on Windows 11:
1. **DWM Scaling Blur:** `VietKey.Engine` was not Per-Monitor DPI v2 aware, causing `GetSystemMetrics(SM_CXSMICON)` to return `16` at 125%/150% display scaling and Windows DWM to bitmap-stretch `16x16` to `20x20`/`24x24`.
2. **No Light/Dark Theme Adaptation:** When switching Windows themes/wallpapers to a light taskbar (`SystemUsesLightTheme == 1`), the white-outlined `V`/`E` icon stayed white and became nearly invisible against the light taskbar.

By embedding `EVFont.ttf` and rendering native 32-bpp ARGB `HICON` handles via GDI `AddFontMemResourceEx` + `CreateFontW` + `CreateDIBSection` + `CreateIconIndirect` at the exact physical DPI tray icon size (`MulDiv(16, trueDpi, 96)`), and monitoring `SystemUsesLightTheme` across `WM_SETTINGCHANGE`, `WM_DWMCOLORIZATIONCOLORCHANGED`, `WM_DPICHANGED`, and a periodic theme check, the `V` and `E` tray icons render razor-sharp 1:1 on any DPI and automatically switch between a white outline (`#FAFAFA`) on dark taskbars and a black outline (`#000000`) on light taskbars.

## What Changes

- **Embed `EVFont.ttf` and Update Static Icon Assets (`assets/EVFont.ttf`, `assets/icon-v.ico`, `assets/icon-e.ico`, `src/VietKey.Engine/VietKey.Engine.csproj`):**
  - Include `assets/EVFont.ttf` as both an embedded resource and output asset, and regenerate `assets/icon-v.ico`, `assets/icon-e.ico`, `assets/icon-v.png`, and `assets/icon-e.png`.
- **Per-Monitor DPI v2 Awareness & Native GDI Hollow Glyph Icon Renderer (`src/VietKey.Engine/Program.cs`, `src/VietKey.Engine/Tray/TrayIconHost.cs`, `src/VietKey.Engine/Tray/Win32TrayMessageWindow.cs`):**
  - Enable `DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2` (`-4`) on startup and compute true physical DPI (`LOGPIXELSX * DESKTOPHORZRES / HORZRES`) so `MulDiv(16, dpi, 96)` returns the exact physical pixel size (`20x20` at 125%, `24x24` at 150%).
  - Render 4 native GDI hollow-glyph `HICON` handles (`_iconVDark`, `_iconEDark`, `_iconVLight`, `_iconELight`) using `EVFont.ttf` (`fontHeight = (size - 2 * (size / 16)) - 1`), 10-offset border pass, center punch-out pass, and pure `#FAFAFA` / `#000000` border RGB with alpha `max(0, avgMask - 10)`.
  - Detect light vs. dark taskbar via `HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize\SystemUsesLightTheme` (with `HKLM` fallback) and automatically refresh the tray icon on `WM_SETTINGCHANGE`, `WM_DWMCOLORIZATIONCOLORCHANGED`, `WM_SYSCOLORCHANGE`, `WM_THEMECHANGED`, `WM_DPICHANGED`, `TaskbarCreated`, and a `1000ms` timer.

## Capabilities

- **Modified:**
  - `docs/system/specs/settings-and-tray/spec.md`

## Claims touched

- None.
