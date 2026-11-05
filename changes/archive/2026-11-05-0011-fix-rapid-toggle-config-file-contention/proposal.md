# Change 0011: Fix Rapid E/V Toggle Config File Contention (`UnauthorizedAccessException`)

## Intent & Problem Statement
When a user rapidly clicks the `Chuyển V / E` button (`QuickToggleModeButton`) or toggles `VietnameseToggle` in `VietKey.UI`, `System.UnauthorizedAccessException: Access to the path is denied` is thrown at `ConfigStore.SaveMacros` (`File.Move(tempPath, MacrosFilePath, overwrite: true)`):
1. `QuickToggleModeButton.Click` sets `VietnameseToggle.IsChecked = !(VietnameseToggle.IsChecked ?? true)`, which synchronously invokes the `Checked`/`Unchecked` handler (`SyncControlsToViewModel()` -> `SaveAndSignalReload()`), and then immediately calls `SyncControlsToViewModel()` a second time back-to-back.
2. When the first `SaveAndSignalReload()` signals `Local\VietKeyConfigReloadEvent`, `VietKey.Engine` (`ConfigReloadCoordinator`) immediately reads `config.json` and `macros.json` via `File.ReadAllText` (which opens with `FileShare.Read` only, without `FileShare.Delete` or `FileShare.Write`). Microseconds later, the second `SaveAndSignalReload()` calls `File.Move(tempPath, MacrosFilePath, overwrite: true)` while `VietKey.Engine` has `macros.json` open, causing Windows NT `MoveFileExW` to throw `UnauthorizedAccessException` (`0x80070005`).
3. `SettingsViewModel.SaveAndSignalReload()` unconditionally rewrites `macros.json` on every `V`/`E` mode toggle even when macros were not modified.

## Scope & Approach
1. **Cross-Process Synchronized & Contention-Safe `ConfigStore` (`src/VietKey.Core/Config/ConfigStore.cs`):**
   - Synchronize `Load`, `Save`, `LoadMacros`, and `SaveMacros` using a named cross-process `Mutex` (`Local\VietKeyConfigStoreMutex`).
   - Read files using `FileShare.ReadWrite | FileShare.Delete` so concurrent readers never block atomic replacement.
   - Write files via a unique per-write temporary file (`$"{targetPath}.{Environment.ProcessId}.{Guid.NewGuid():N}.tmp"`) with a bounded retry loop and shared-stream fallback on `UnauthorizedAccessException` / `IOException`.
2. **Avoid Unnecessary `SaveMacros` on Mode Toggle (`src/VietKey.UI/ViewModels/SettingsViewModel.cs`):**
   - Track `_macrosDirty` in `SettingsViewModel` so `SaveAndSignalReload` only writes `macros.json` when macros have been added/updated/removed or when `macros.json` does not yet exist.
3. **Eliminate Duplicate `SyncControlsToViewModel()` Call (`src/VietKey.UI/MainWindow.xaml.cs`):**
   - Remove the redundant `SyncControlsToViewModel()` call inside `QuickToggleModeButton.Click` because flipping `VietnameseToggle.IsChecked` already triggers `checkChanged` -> `SyncControlsToViewModel()`.
