# Verification: 0010-fixed-taskbar-app-logo-icon

## Automated Tests

- `dotnet test --nologo`: **120 passed, 0 failed**.
- `assets/app-logo.png` (`512x512`, bounding box `(0, 0, 512, 512)`, zero padding, 100% transparent outside the blue rounded square) and multi-size `assets/app-logo.ico` (`16..256`) are used as `<ApplicationIcon>` for `VietKey.UI.exe` and `VietKey.Engine.exe` and as the fixed `DesktopSettingsWindow.Icon`.

## Claim-Touch Account

- None (`claims_touched: []`).
