# Tasks: 0008-add-hide-dialog-on-startup-option

## Wave 1: Update Startup Checkbox Text and Add HideDialogOnStartup Option

- [x] **T1 — Rename `"Khởi động cùng Windows"` and add `"Không hiển thị hộp thoại khi khởi động"` (`HideDialogOnStartup`) in `AppConfig`, `SettingsViewModel`, `MainWindow`, and `VietKey.Engine`** (`REQ-settings-3`)
  - **Files:**
    - `src/VietKey.Core/Config/AppConfig.cs`
    - `src/VietKey.UI/ViewModels/SettingsViewModel.cs`
    - `src/VietKey.UI/MainWindow.xaml`
    - `src/VietKey.UI/MainWindow.xaml.cs`
    - `src/VietKey.Engine/Program.cs`
    - `tests/VietKey.Core.Tests/UI/SettingsViewModelTests.cs`
  - **Done when:** `MainWindow.xaml` displays `"Khởi động cùng Windows"` and `"Không hiển thị hộp thoại khi khởi động"`, `AppConfig.HideDialogOnStartup` persists to `config.json`, `SettingsViewModel.BuildStartupRegistryCommand` omits `--show-ui` when `HideDialogOnStartup` is `true` and appends `--show-ui` when `false`, and all unit tests pass.
  - **Verify:** `dotnet test --filter FullyQualifiedName~SettingsViewModelTests`
