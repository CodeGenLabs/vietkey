namespace VietKey.Core.Injection;

public sealed class SmartSwitchTracker
{
    private readonly Dictionary<string, bool> _appLanguageMap = new(StringComparer.OrdinalIgnoreCase);

    public string CurrentExecutable { get; private set; } = string.Empty;

    public bool OnForegroundChanged(
        string executableName,
        bool currentGlobalVietnameseEnabled,
        bool smartSwitchEnabled,
        out bool bufferShouldClear)
    {
        string normalized = Path.GetFileName(executableName ?? string.Empty).ToLowerInvariant();
        if (string.Equals(normalized, CurrentExecutable, StringComparison.Ordinal))
        {
            bufferShouldClear = false;
            return currentGlobalVietnameseEnabled;
        }

        CurrentExecutable = normalized;
        bufferShouldClear = true;

        if (!smartSwitchEnabled || string.IsNullOrEmpty(normalized))
        {
            return currentGlobalVietnameseEnabled;
        }

        if (_appLanguageMap.TryGetValue(normalized, out bool rememberedMode))
        {
            return rememberedMode;
        }

        return currentGlobalVietnameseEnabled;
    }

    public void RecordUserToggle(string executableName, bool vietnameseEnabled)
    {
        string normalized = Path.GetFileName(executableName ?? string.Empty).ToLowerInvariant();
        if (!string.IsNullOrEmpty(normalized))
        {
            _appLanguageMap[normalized] = vietnameseEnabled;
        }
    }
}
