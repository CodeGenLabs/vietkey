using VietKey.Core.Config;
using VietKey.UI.ViewModels;

namespace VietKey.UI;

public sealed class MainWindow
{
    public SettingsViewModel ViewModel { get; }
    public event Action? Closed;

    public MainWindow(SettingsViewModel viewModel)
    {
        ViewModel = viewModel;
    }

    public void CloseAndFlush(string engineExecutablePath = "VietKey.Engine.exe")
    {
        ViewModel.SaveAndSignalReload(engineExecutablePath);
        Closed?.Invoke();
    }
}
