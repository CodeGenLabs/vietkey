# Verification: 0016-rename-main-app-executable-to-vietkey-exe

- Verified `src/VietKey.UI/VietKey.UI.csproj` builds `VietKey.exe` (`<AssemblyName>VietKey</AssemblyName>`).
- Verified `SettingsProcessLauncher` resolves `VietKey.exe` and `dotnet test --nologo` passes all 121 unit tests.
