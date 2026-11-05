# Tasks: 0009-native-gdi-dpi-and-theme-aware-tray-icons

## Wave 1: Native GDI DPI-Aware and Theme-Aware System Tray Icons

- [x] **T1 — Render pixel-sharp DPI-native hollow `V` and `E` icons via GDI (`EVFont.ttf`) and auto-switch outline color between Dark (`#FAFAFA`) and Light (`#000000`) taskbar themes** (`REQ-settings-1`)
  - **Files:**
    - `assets/EVFont.ttf`
    - `assets/icon-v.png`
    - `assets/icon-v.ico`
    - `assets/icon-e.png`
    - `assets/icon-e.ico`
    - `src/VietKey.Engine/VietKey.Engine.csproj`
    - `src/VietKey.Engine/Program.cs`
    - `src/VietKey.Engine/Tray/TrayIconHost.cs`
    - `src/VietKey.Engine/Tray/Win32TrayMessageWindow.cs`
    - `tests/VietKey.Core.Tests/Hook/KeyboardSessionProcessorTests.cs`
  - **Done when:** `TrayIconHost` renders native GDI `HICON` handles at physical DPI size (`MulDiv(16, dpi, 96)`), exposes distinct non-zero `ActiveIconHandle` values across `V`/`E` and Dark/Light themes (`RefreshThemeIfNeeded`), and `Win32TrayMessageWindow` refreshes theme/DPI on `WM_SETTINGCHANGE`, `WM_DWMCOLORIZATIONCOLORCHANGED`, `WM_DPICHANGED`, and timer tick.
  - **Verify:** `dotnet test --filter FullyQualifiedName~KeyboardSessionProcessorTests`
