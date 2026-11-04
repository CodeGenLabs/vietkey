using System.Diagnostics;

namespace VietKey.Engine.Tray;

public sealed class SettingsProcessLauncher
{
    private readonly Func<string, bool> _processStarter;
    public string UiExecutablePath { get; }

    public SettingsProcessLauncher(string? uiExecutablePath = null, Func<string, bool>? processStarter = null)
    {
        UiExecutablePath = uiExecutablePath ?? Path.Combine(AppContext.BaseDirectory, "VietKey.UI.exe");
        _processStarter = processStarter ?? DefaultStartProcess;
    }

    public bool LaunchSettingsUi() => _processStarter(UiExecutablePath);

    private static bool DefaultStartProcess(string exePath)
    {
        if (!File.Exists(exePath))
        {
            return false;
        }

        using var proc = Process.Start(new ProcessStartInfo
        {
            FileName = exePath,
            UseShellExecute = true,
            WorkingDirectory = Path.GetDirectoryName(exePath) ?? AppContext.BaseDirectory
        });
        return proc is not null;
    }
}
