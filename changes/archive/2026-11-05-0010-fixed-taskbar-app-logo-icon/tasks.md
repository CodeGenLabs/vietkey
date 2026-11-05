# Tasks: 0010-fixed-taskbar-app-logo-icon

## Wave 1: Fixed Full-Bleed Blue App Logo on Taskbar and Executables

- [x] **T1 — Crop zero-padding transparent blue app logo (`app-logo.png`, `app-logo.ico`) and keep Taskbar/Window icon fixed to `app-logo`** (`REQ-settings-3`)
  - **Files:**
    - `assets/app-logo.png`
    - `assets/app-logo.ico`
    - `src/VietKey.Engine/VietKey.Engine.csproj`
    - `src/VietKey.UI/VietKey.UI.csproj`
    - `src/VietKey.UI/MainWindow.xaml`
    - `src/VietKey.UI/MainWindow.xaml.cs`
  - **Done when:** `assets/app-logo.png` and `assets/app-logo.ico` contain the full-bleed zero-padding transparent blue rounded-square logo, `VietKey.UI.csproj` and `VietKey.Engine.csproj` use `app-logo.ico` as `<ApplicationIcon>`, and `DesktopSettingsWindow` keeps its window/taskbar `Icon` fixed to `app-logo.png` instead of changing to `icon-v.png`/`icon-e.png`.
  - **Verify:** `dotnet test --nologo`
