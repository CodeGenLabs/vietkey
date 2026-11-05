# Change 0016: Rename Main Application Executable from `VietKey.UI.exe` to `VietKey.exe`

## Intent & Problem Statement
The user requested changing the main Settings application executable name from `VietKey.UI.exe` to `VietKey.exe` so users launch and see `VietKey.exe` directly.

## Scope & Approach
1. **Set `<AssemblyName>VietKey</AssemblyName>` (`src/VietKey.UI/VietKey.UI.csproj`):**
   - Configure `VietKey.UI.csproj` to produce `VietKey.exe` as its output executable.
2. **Update `SettingsProcessLauncher` (`src/VietKey.Engine/Tray/SettingsProcessLauncher.cs`) and `SmartSwitchTracker` (`src/VietKey.Core/Injection/SmartSwitchTracker.cs`):**
   - Resolve `VietKey.exe` (both co-located and in the sibling `VietKey.UI/bin/...` directory) when opening Settings from the System Tray or `--show-ui`.
   - Treat `vietkey.exe` as the Settings UI process in `SmartSwitchTracker.RecordUserToggle`.
