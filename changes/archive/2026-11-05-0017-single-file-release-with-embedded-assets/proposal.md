# Change 0017: Single-File Release Packaging with Embedded Assets (Zero `.dll`, `.json`, or `Assets/` Clutter)

## Intent & Problem Statement
When publishing the application, standard .NET output created multiple auxiliary files (`VietKey.dll`, `VietKey.Core.dll`, `VietKey.Engine.dll`, `*.deps.json`, `*.runtimeconfig.json`, and an external `Assets/` folder). Furthermore, enabling `-p:PublishSingleFile=true` previously failed with `NETSDK1151` because `VietKey.UI.csproj` had a redundant `<ProjectReference>` to `VietKey.Engine.csproj`.

## Scope & Approach
1. **Embed All Assets & Remove External `Assets/` Copy (`src/VietKey.Engine/VietKey.Engine.csproj`):**
   - Remove the `<None Include="..\..\assets\..." CopyToOutputDirectory="PreserveNewest" />` items from `VietKey.Engine.csproj` since `EVFont.ttf` is already an `<EmbeddedResource>` inside `VietKey.Engine.exe` and `app-logo.ico` is embedded via `<ApplicationIcon>`.
2. **Decouple `VietKey.UI.csproj` from `VietKey.Engine.csproj` to Enable Single-File Publish (`src/VietKey.UI/VietKey.UI.csproj`):**
   - Remove the unused `<ProjectReference Include="..\VietKey.Engine\VietKey.Engine.csproj" />` from `VietKey.UI.csproj` so both `VietKey.Engine` and `VietKey.UI` can be published with `-p:PublishSingleFile=true`, bundling all `.dll`, `.deps.json`, `.runtimeconfig.json`, and WPF resources directly inside `VietKey.exe` and `VietKey.Engine.exe`.
