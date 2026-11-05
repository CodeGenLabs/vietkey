# Tasks: 0011-fix-rapid-toggle-config-file-contention

- [x] **Task 1**: Synchronize `ConfigStore` reads and writes across processes using a named mutex (`Local\VietKeyConfigStoreMutex`), shared read streams (`FileShare.ReadWrite | FileShare.Delete`), unique temp files, and bounded retry/fallback logic (`REQ-settings-2`).
  - Files: `src/VietKey.Core/Config/ConfigStore.cs`, `tests/VietKey.Core.Tests/Config/ConfigStoreTests.cs`
- [x] **Task 2**: Track macro dirtiness in `SettingsViewModel` so `SaveAndSignalReload` only writes `macros.json` when macros changed, and remove the duplicate `SyncControlsToViewModel()` invocation in `QuickToggleModeButton.Click` (`REQ-settings-2`).
  - Files: `src/VietKey.UI/ViewModels/SettingsViewModel.cs`, `src/VietKey.UI/MainWindow.xaml.cs`, `tests/VietKey.Core.Tests/UI/SettingsViewModelTests.cs`
