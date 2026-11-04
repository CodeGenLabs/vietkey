namespace VietKey.Core.Injection;

public sealed class AppCompatibilityDetector
{
    private readonly Func<nint, string> _processNameResolver;
    private readonly Dictionary<nint, (string ExecutableName, AppCompatibilityProfile Profile)> _hwndCache = new(64);
    private nint _lastHwnd;
    private (string ExecutableName, AppCompatibilityProfile Profile) _lastResult = ("", AppCompatibilityProfile.Standard);

    public AppCompatibilityDetector(Func<nint, string> processNameResolver)
    {
        _processNameResolver = processNameResolver;
    }

    public (string ExecutableName, AppCompatibilityProfile Profile) ResolveForWindow(nint hwnd)
    {
        if (hwnd == 0)
        {
            return ("", AppCompatibilityProfile.Standard);
        }

        if (hwnd == _lastHwnd && _lastResult.ExecutableName.Length > 0)
        {
            return _lastResult;
        }

        if (_hwndCache.TryGetValue(hwnd, out var cached))
        {
            _lastHwnd = hwnd;
            _lastResult = cached;
            return cached;
        }

        string rawPath = _processNameResolver(hwnd) ?? string.Empty;
        string exeName = Path.GetFileName(rawPath).ToLowerInvariant();
        if (string.IsNullOrEmpty(exeName))
        {
            exeName = "unknown.exe";
        }

        var profile = ClassifyExecutable(exeName);
        var entry = (exeName, profile);

        if (_hwndCache.Count >= 256)
        {
            _hwndCache.Clear();
        }

        _hwndCache[hwnd] = entry;
        _lastHwnd = hwnd;
        _lastResult = entry;
        return entry;
    }

    public static AppCompatibilityProfile ClassifyExecutable(string executableName)
    {
        string lower = Path.GetFileName(executableName).ToLowerInvariant();
        return lower switch
        {
            "chrome.exe" or "msedge.exe" or "brave.exe" or "opera.exe" or "vivaldi.exe" or "firefox.exe"
                => AppCompatibilityProfile.ChromiumBrowser,
            "code.exe" or "ms-teams.exe" or "teams.exe" or "slack.exe" or "discord.exe" or "obsidian.exe"
                => AppCompatibilityProfile.ElectronApp,
            "winword.exe" or "excel.exe" or "powerpnt.exe" or "outlook.exe"
                => AppCompatibilityProfile.MicrosoftOffice,
            _ => AppCompatibilityProfile.Standard
        };
    }
}
