# Proposal: 0010-fixed-taskbar-app-logo-icon

## Why

Previously, `DesktopSettingsWindow.UpdateHeaderBadge` reassigned `Window.Icon = badgeBitmap` (`icon-v.png` / `icon-e.png`) whenever the typing mode changed, and `<ApplicationIcon>` pointed to `icon-v.ico`. As a result, the application icon on the Windows Taskbar synced with the System Tray `V`/`E` glyph instead of displaying the fixed blue VietKey app emblem.

## What Changes

- **Crop Full-Bleed Transparent App Logo Icon (`assets/app-logo.png`, `assets/app-logo.ico`):**
  - Extract the blue rounded-square emblem from the original high-resolution logo (`100..540`, `239..677`), make everything outside the blue rounded square 100% transparent, and crop with zero padding (`512x512` full-bleed bbox `(0, 0, 512, 512)`).
- **Use Fixed App Logo for Executable & Taskbar Window Icon (`src/VietKey.Engine/VietKey.Engine.csproj`, `src/VietKey.UI/VietKey.UI.csproj`, `src/VietKey.UI/MainWindow.xaml`, `src/VietKey.UI/MainWindow.xaml.cs`):**
  - Point `<ApplicationIcon>` in both `VietKey.UI.csproj` and `VietKey.Engine.csproj` to `..\..\assets\app-logo.ico`.
  - Set `DesktopSettingsWindow.Icon` fixed to `pack://application:,,,/Assets/app-logo.png` and remove `Icon = badgeBitmap;` from `UpdateHeaderBadge` so the Taskbar icon always shows the blue VietKey logo while the System Tray shows the `V`/`E` mode icon.

## Capabilities

- **Modified:**
  - `docs/system/specs/settings-and-tray/spec.md`

## Claims touched

- None.
