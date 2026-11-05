# Verification: 0009-native-gdi-dpi-and-theme-aware-tray-icons

## Automated Tests

- `dotnet test --nologo`: **120 passed, 0 failed**.
- `SingleInstanceMutexAndGlobalHotkey_TogglesModeOnAltZAndPreventsDuplicateInstance` verifies:
  - `TrayIconHost.RenderedIconSize >= 16` (physical DPI icon size via `MulDiv(16, GetTrueScreenDpi(), 96)`).
  - Distinct non-zero native GDI `HICON` handles (`CreateDIBSection` + `CreateIconIndirect`) for `V` Dark (`#FAFAFA`), `V` Light (`#000000`), `E` Dark (`#FAFAFA`), and `E` Light (`#000000`).
  - Dynamic switching via `RefreshThemeIfNeeded` and `Win32TrayMessageWindow` handling of `WM_SETTINGCHANGE`, `WM_DWMCOLORIZATIONCOLORCHANGED`, `WM_SYSCOLORCHANGE`, `WM_THEMECHANGED`, `WM_DPICHANGED`, `TaskbarCreated`, and `WM_TIMER`.

## Claim-Touch Account

- None (`claims_touched: []`).
