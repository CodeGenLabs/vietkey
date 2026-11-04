using System.Diagnostics;

namespace VietKey.Engine.Tray;

public sealed class SettingsProcessLauncher
{
    private readonly Func<string, bool> _processStarter;
    public string UiExecutablePath { get; }

    public SettingsProcessLauncher(string? uiExecutablePath = null, Func<string, bool>? processStarter = null)
    {
        UiExecutablePath = uiExecutablePath ?? ResolveDefaultUiExecutablePath();
        _processStarter = processStarter ?? DefaultStartProcess;
    }

    public bool LaunchSettingsUi() => _processStarter(UiExecutablePath);

    public static string ResolveDefaultUiExecutablePath()
    {
        string coLocated = Path.Combine(AppContext.BaseDirectory, "VietKey.UI.exe");
        if (File.Exists(coLocated))
        {
            return coLocated;
        }

        // Support Visual Studio multi-project bin/<Configuration>/<TargetFramework>/ layout
        string baseDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string? tfmDir = Path.GetFileName(baseDir);
        string? configDir = Path.GetFileName(Path.GetDirectoryName(baseDir));
        string? srcRoot = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(baseDir))));
        if (!string.IsNullOrEmpty(srcRoot) && !string.IsNullOrEmpty(configDir) && !string.IsNullOrEmpty(tfmDir))
        {
            string siblingCandidate = Path.Combine(srcRoot, "VietKey.UI", "bin", configDir, tfmDir, "VietKey.UI.exe");
            if (File.Exists(siblingCandidate))
            {
                return siblingCandidate;
            }
        }

        return coLocated;
    }

    private static bool DefaultStartProcess(string exePath)
    {
        string targetPath = File.Exists(exePath) ? exePath : ResolveDefaultUiExecutablePath();
        if (!File.Exists(targetPath))
        {
            return false;
        }

        using var proc = Process.Start(new ProcessStartInfo
        {
            FileName = targetPath,
            UseShellExecute = true,
            WorkingDirectory = Path.GetDirectoryName(targetPath) ?? AppContext.BaseDirectory
        });
        return proc is not null;
    }
}
