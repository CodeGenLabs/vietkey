using System.Text.Json.Serialization;

namespace VietKey.Core.Config;

public enum InputMethod
{
    Telex = 0,
    Vni = 1
}

public enum ToggleHotkey
{
    AltZ = 0,
    CtrlShift = 1
}

public sealed record AppConfig
{
    public InputMethod InputMethod { get; init; } = InputMethod.Telex;
    public bool VietnameseEnabled { get; init; } = true;
    public bool ModernTone { get; init; } = true;
    public bool SpellCheck { get; init; } = true;
    public bool AutoRestoreEnglish { get; init; } = true;
    public bool SmartSwitch { get; init; } = true;
    public bool BrowserAutocompleteFix { get; init; } = true;
    public bool EnableMacros { get; init; } = true;
    public bool StartWithWindows { get; init; } = false;
    public ToggleHotkey ToggleHotkey { get; init; } = ToggleHotkey.AltZ;

    public static AppConfig Default { get; } = new();
}

[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(AppConfig))]
[JsonSerializable(typeof(Dictionary<string, string>))]
public partial class VietKeyJsonContext : JsonSerializerContext
{
}
