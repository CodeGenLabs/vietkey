# Tasks: 0006-integrate-custom-logo-and-tray-icons

## Wave 1: Integrate Custom VietKey Logo Banner and V/E System Tray Icons

- [x] **T1 — Wire cropped `logo-banner.png`, `icon-v.ico`/`png`, and `icon-e.ico`/`png` into `VietKey.Engine`, `VietKey.UI`, and `README.md`** (`REQ-settings-1`)
  - **Files:**
    - `assets/logo-banner.png`
    - `assets/icon-v.png`
    - `assets/icon-v.ico`
    - `assets/icon-e.png`
    - `assets/icon-e.ico`
    - `src/VietKey.Engine/VietKey.Engine.csproj`
    - `src/VietKey.Engine/Tray/TrayIconHost.cs`
    - `src/VietKey.UI/VietKey.UI.csproj`
    - `src/VietKey.UI/MainWindow.xaml`
    - `src/VietKey.UI/MainWindow.xaml.cs`
    - `README.md`
    - `tests/VietKey.Core.Tests/Hook/KeyboardSessionProcessorTests.cs`
  - **Done when:** `TrayIconHost` loads `icon-v.ico` and `icon-e.ico` handles and switches between them on mode toggle, `VietKey.UI` displays `logo-banner.png` and the active `icon-v.png`/`icon-e.png` badge in `MainWindow.xaml`, both executables use `icon-v.ico` as their `<ApplicationIcon>`, and all unit tests pass.
  - **Verify:** `dotnet test --filter FullyQualifiedName~KeyboardSessionProcessorTests`
