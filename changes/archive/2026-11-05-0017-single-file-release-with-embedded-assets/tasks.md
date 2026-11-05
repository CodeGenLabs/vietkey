# Tasks: 0017-single-file-release-with-embedded-assets

- [x] **Task 1**: Remove redundant `CopyToOutputDirectory` asset items from `src/VietKey.Engine/VietKey.Engine.csproj` and remove the redundant `VietKey.Engine` project reference from `src/VietKey.UI/VietKey.UI.csproj` so `PublishSingleFile=true` bundles all `.dll`, `.json`, and `Assets` into `VietKey.exe` and `VietKey.Engine.exe` (`REQ-settings-3`).
  - Files: `src/VietKey.Engine/VietKey.Engine.csproj`, `src/VietKey.UI/VietKey.UI.csproj`, `tests/VietKey.Core.Tests/UI/SettingsViewModelTests.cs`
