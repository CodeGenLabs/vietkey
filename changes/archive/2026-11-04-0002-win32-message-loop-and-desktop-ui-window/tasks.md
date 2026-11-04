# Tasks: 0002-win32-message-loop-and-desktop-ui-window

## Wave 1: Win32 Message Loop & Tray Popup Menu in `VietKey.Engine`

- [x] **T1 — Implement Win32 message window, popup menu, and message loop in `VietKey.Engine`** (`REQ-settings-1`)
  - **Files:**
    - `src/VietKey.Engine/VietKey.Engine.csproj`
    - `src/VietKey.Engine/Tray/TrayIconHost.cs`
    - `src/VietKey.Engine/Tray/Win32TrayMessageWindow.cs`
    - `src/VietKey.Engine/Program.cs`
    - `src/VietKey.Engine/Properties/launchSettings.json`
    - `tests/VietKey.Core.Tests/VietKey.Core.Tests.csproj`
    - `tests/VietKey.Core.Tests/Hook/KeyboardSessionProcessorTests.cs`
  - **Done when:** `TrayIconHost` handles `WM_APP_TRAY` mouse messages (`WM_LBUTTONUP`, `WM_LBUTTONDBLCLK`, `WM_RBUTTONUP`), `Win32TrayMessageWindow` runs a native Win32 message pump (`GetMessageW` / `DispatchMessageW`) with popup menu commands, `Program.Main` launches `VietKey.UI` when `--show-ui` is passed, and unit tests in `KeyboardSessionProcessorTests` pass.
  - **Verify:** `dotnet test --filter FullyQualifiedName~KeyboardSessionProcessorTests`

## Wave 2: Interactive Windows 11 Desktop Settings Window in `VietKey.UI`

- [x] **T2 — Implement Windows 11 Fluent Desktop Window and Visual Studio Startup Integration in `VietKey.UI`** (`REQ-settings-3`)
  - **Files:**
    - `src/VietKey.UI/VietKey.UI.csproj`
    - `src/VietKey.UI/MainWindow.xaml`
    - `src/VietKey.UI/MainWindow.xaml.cs`
    - `src/VietKey.UI/App.xaml.cs`
    - `src/VietKey.UI/Program.cs`
    - `src/VietKey.UI/Properties/launchSettings.json`
    - `VietKey.slnx`
    - `tests/VietKey.Core.Tests/UI/SettingsViewModelTests.cs`
  - **Done when:** `VietKey.UI` compiles as a `net8.0-windows` `WinExe` desktop app (`UseWPF=true`), opens a real Windows 11 Fluent-styled Settings window bound to `SettingsViewModel` (with all toggles, combo boxes, macro manager, and live test input box), ensures `VietKey.Engine` runs in the background when launched interactively, reorder `VietKey.slnx` so `VietKey.UI` is the default Visual Studio startup project, and all unit tests in `SettingsViewModelTests` pass.
  - **Verify:** `dotnet test --filter FullyQualifiedName~SettingsViewModelTests`
