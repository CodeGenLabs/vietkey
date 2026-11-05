# Tasks: 0016-rename-main-app-executable-to-vietkey-exe

- [x] **Task 1**: Set `<AssemblyName>VietKey</AssemblyName>` in `src/VietKey.UI/VietKey.UI.csproj` and update `SettingsProcessLauncher`, `SmartSwitchTracker`, and unit tests to resolve `VietKey.exe` (`REQ-settings-3`).
  - Files: `src/VietKey.UI/VietKey.UI.csproj`, `src/VietKey.Engine/Tray/SettingsProcessLauncher.cs`, `src/VietKey.Core/Injection/SmartSwitchTracker.cs`, `tests/VietKey.Core.Tests/UI/SettingsViewModelTests.cs`
