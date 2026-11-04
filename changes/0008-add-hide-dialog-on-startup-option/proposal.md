# Proposal: 0008-add-hide-dialog-on-startup-option

## Why

The user requested:
1. Renaming the startup checkbox text from `"Khởi động VietKey cùng Windows 11"` to `"Khởi động cùng Windows"`.
2. Adding a `"Không hiển thị hộp thoại khi khởi động"` (`HideDialogOnStartup`) checkbox so that when checked, starting with Windows only launches `VietKey.Engine.exe` silently in the background (`System Tray`), and when unchecked, starting with Windows launches `VietKey.Engine.exe --show-ui` so the Settings dialog is displayed on boot.

## What Changes

- **Configuration Model (`src/VietKey.Core/Config/AppConfig.cs`):**
  - Add `HideDialogOnStartup` boolean property to `AppConfig`.
- **Startup Registry Command & UI Controls (`src/VietKey.UI/ViewModels/SettingsViewModel.cs`, `src/VietKey.UI/MainWindow.xaml`, `src/VietKey.UI/MainWindow.xaml.cs`, `src/VietKey.Engine/Program.cs`):**
  - Rename `StartupToggle` label to `"Khởi động cùng Windows"` and add `HideDialogOnStartupToggle` (`"Không hiển thị hộp thoại khi khởi động"`).
  - Add `SettingsViewModel.BuildStartupRegistryCommand(string engineExecutablePath, bool hideDialogOnStartup)` so `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\VietKey` registers `"\"<path>\""` when `HideDialogOnStartup` is `true` (background only) and `"\"<path>\" --show-ui"` when `HideDialogOnStartup` is `false` (display dialog on boot).

## Capabilities

- **Modified:**
  - `docs/system/specs/settings-and-tray/spec.md`

## Claims touched

- None.
