# Verification: 0017-single-file-release-with-embedded-assets

- Verified `PublishSingleFile=true` produces only `VietKey.exe` (`1.66 MB`) and `VietKey.Engine.exe` (`523 KB`) in `publish/VietKey` with zero `.dll`, `.json`, `.pdb`, or external `Assets/` files.
- Verified `dotnet test --nologo` passes all 121 unit tests.
