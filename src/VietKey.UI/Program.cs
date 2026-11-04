using System.Diagnostics;
using System.IO;
using System.Windows;
using VietKey.Core.Config;

namespace VietKey.UI;

public static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        var store = new ConfigStore();
        string enginePath = ResolveEngineExecutablePath();

        if (args.Contains("--verify-close"))
        {
            var testApp = new App(store, processExitCallback: () => { });
            testApp.MainWindow.CloseAndFlush(enginePath);
            return 0;
        }

        EnsureBackgroundEngineStarted(enginePath);

        var wpfApp = new Application
        {
            ShutdownMode = ShutdownMode.OnMainWindowClose
        };

        var appController = new App(store, processExitCallback: () => wpfApp.Shutdown(0));
        var desktopWindow = new DesktopSettingsWindow(appController.MainWindow, enginePath);
        return wpfApp.Run(desktopWindow);
    }

    public static string ResolveEngineExecutablePath()
    {
        string coLocated = Path.Combine(AppContext.BaseDirectory, "VietKey.Engine.exe");
        if (File.Exists(coLocated))
        {
            return coLocated;
        }

        string baseDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string? tfmDir = Path.GetFileName(baseDir);
        string? configDir = Path.GetFileName(Path.GetDirectoryName(baseDir));
        string? srcRoot = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(baseDir))));
        if (!string.IsNullOrEmpty(srcRoot) && !string.IsNullOrEmpty(configDir) && !string.IsNullOrEmpty(tfmDir))
        {
            string siblingCandidate = Path.Combine(srcRoot, "VietKey.Engine", "bin", configDir, tfmDir, "VietKey.Engine.exe");
            if (File.Exists(siblingCandidate))
            {
                return siblingCandidate;
            }
        }

        return coLocated;
    }

    private static void EnsureBackgroundEngineStarted(string enginePath)
    {
        try
        {
            if (Mutex.TryOpenExisting(@"Local\VietKeyEngineMutex", out Mutex? existing))
            {
                existing.Dispose();
                return;
            }

            if (File.Exists(enginePath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = enginePath,
                    UseShellExecute = true,
                    WorkingDirectory = Path.GetDirectoryName(enginePath) ?? AppContext.BaseDirectory
                });
            }
        }
        catch
        {
            // Ignore background launch failure in restricted environments
        }
    }
}
