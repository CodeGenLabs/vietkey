# Proposal: 0006-integrate-custom-logo-and-tray-icons

## Why

We now have custom brand artwork for VietKey: a logo banner with the slogan `"VietKey — Gõ tiếng Việt dễ dàng"` and glossy rounded-square `"V"` (Vietnamese) and `"E"` (English) mode icons. We need to crop and package these assets into `.png` and multi-resolution `.ico` files (`assets/logo-banner.png`, `assets/icon-v.png`, `assets/icon-v.ico`, `assets/icon-e.png`, `assets/icon-e.ico`) and integrate them into the executable `<ApplicationIcon>`, the Win32 System Tray (`TrayIconHost`), the `VietKey.UI` control panel window, and the GitHub `README.md`.

## What Changes

- **Brand & Icon Assets (`assets/`):**
  - Add cropped transparent `assets/logo-banner.png` (`1396x530`), `assets/icon-v.png` & `assets/icon-v.ico` (Vietnamese `V` mode), and `assets/icon-e.png` & `assets/icon-e.ico` (English `E` mode).
- **System Tray V/E Icon Switching (`src/VietKey.Engine/`):**
  - Configure `<ApplicationIcon>..\..\assets\icon-v.ico</ApplicationIcon>` and copy `icon-v.ico` / `icon-e.ico` to `Assets\` in `VietKey.Engine.csproj`.
  - Load `icon-v.ico` and `icon-e.ico` via Win32 `LoadImageW` in `TrayIconHost` so the System Tray displays the glossy `V` icon in Vietnamese mode and the glossy `E` icon in English mode.
- **UI Header Logo & Dynamic Mode Icon (`src/VietKey.UI/`, `README.md`):**
  - Embed `logo-banner.png`, `icon-v.png`, `icon-e.png`, and `icon-v.ico` as WPF resources in `VietKey.UI.csproj`, display the logo banner and active mode icon in `MainWindow.xaml` / `MainWindow.xaml.cs`, and embed the logo banner at the top of `README.md`.

## Capabilities

- **Modified:**
  - `docs/system/specs/settings-and-tray/spec.md`

## Claims touched

- None.
