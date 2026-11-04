using VietKey.Core.Config;
using VietKey.UI.ViewModels;

namespace VietKey.UI;

public sealed class App
{
    private readonly Action _processExitCallback;
    public MainWindow MainWindow { get; }
    public bool HasExited { get; private set; }

    public App(
        ConfigStore store,
        string reloadEventName = ConfigReloadCoordinator.DefaultEventName,
        Action? processExitCallback = null)
    {
        _processExitCallback = processExitCallback ?? (() => Environment.Exit(0));
        var vm = new SettingsViewModel(store, reloadEventName, (_, _) => { });
        MainWindow = new MainWindow(vm);
        MainWindow.Closed += Exit;
    }

    public void Exit()
    {
        if (HasExited)
        {
            return;
        }
        HasExited = true;
        _processExitCallback();
    }
}
