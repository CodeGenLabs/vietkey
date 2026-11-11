namespace VietKey.Core.Injection;

public enum AppCompatibilityProfile
{
    Standard = 0,
    ChromiumBrowser = 1,
    ElectronApp = 2,
    MicrosoftOffice = 3,
    MicrosoftExcel = 4
}

public readonly record struct SimulatedKeyEvent(
    ushort VirtualKey,
    ushort ScanCode,
    bool IsUnicode,
    bool IsKeyUp);
