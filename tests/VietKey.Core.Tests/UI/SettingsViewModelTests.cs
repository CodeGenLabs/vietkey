using VietKey.Core.Config;
using VietKey.UI.ViewModels;
using Xunit;

namespace VietKey.Core.Tests.UI;

public sealed class SettingsViewModelTests : IDisposable
{
    private readonly string _tempDir;

    public SettingsViewModelTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "VietKeyUITests_" + Guid.NewGuid().ToString("N"));
    }

    // @covers REQ-settings-3
    [Fact]
    public async Task SaveAndMacroEditor_PersistsConfigSignalsLiveReloadAndUpdatesStartupRegistry()
    {
        var store = new ConfigStore(_tempDir);
        string eventName = @"Local\VietKeyUITestReload_" + Guid.NewGuid().ToString("N");
        using var coordinator = new ConfigReloadCoordinator(store, eventName);
        var reloadTcs = new TaskCompletionSource<AppConfig>(TaskCreationOptions.RunContinuationsAsynchronously);
        coordinator.ConfigReloaded += (cfg, _) => reloadTcs.TrySetResult(cfg);
        coordinator.StartListening();

        bool? registeredStartup = null;
        string? registeredExe = null;
        var vm = new SettingsViewModel(store, eventName, (enable, exePath) =>
        {
            registeredStartup = enable;
            registeredExe = exePath;
        });

        Assert.False(vm.TryAddOrUpdateMacro("   ", "invalid"));
        Assert.False(vm.TryAddOrUpdateMacro("has space", "invalid"));
        Assert.True(vm.TryAddOrUpdateMacro("vn", "Việt Nam"));
        Assert.True(vm.TryAddOrUpdateMacro("hn", "Hà Nội"));
        Assert.True(vm.RemoveMacro("hn"));
        Assert.Single(vm.Macros);

        vm.Config = vm.Config with
        {
            InputMethod = InputMethod.Vni,
            ModernTone = false,
            StartWithWindows = true,
            HideDialogOnStartup = true
        };

        vm.SaveAndSignalReload(@"C:\VietKey\VietKey.Engine.exe");

        var reloaded = await reloadTcs.Task.WaitAsync(TimeSpan.FromMilliseconds(500));
        Assert.Equal(InputMethod.Vni, reloaded.InputMethod);
        Assert.False(reloaded.ModernTone);
        Assert.True(reloaded.StartWithWindows);
        Assert.True(reloaded.HideDialogOnStartup);
        Assert.Equal("Việt Nam", coordinator.CurrentMacros["vn"]);
        Assert.True(registeredStartup);
        Assert.Equal(@"C:\VietKey\VietKey.Engine.exe", registeredExe);

        // Verify startup command generation with HideDialogOnStartup checked (background only) vs unchecked (--show-ui)
        Assert.Equal(
            "\"C:\\VietKey\\VietKey.Engine.exe\"",
            SettingsViewModel.BuildStartupRegistryCommand(@"C:\VietKey\VietKey.Engine.exe", hideDialogOnStartup: true));
        Assert.Equal(
            "\"C:\\VietKey\\VietKey.Engine.exe\" --show-ui",
            SettingsViewModel.BuildStartupRegistryCommand(@"C:\VietKey\VietKey.Engine.exe", hideDialogOnStartup: false));
    }

    // @covers REQ-settings-3
    [Fact]
    public void OnDemandLauncherAndWindowClose_SpawnsUiFromTrayMenuAndTerminatesProcessOnClose()
    {
        string? launchedPath = null;
        var launcher = new VietKey.Engine.Tray.SettingsProcessLauncher(
            @"C:\VietKey\VietKey.UI.exe",
            path =>
            {
                launchedPath = path;
                return true;
            });

        var tray = new VietKey.Engine.Tray.TrayIconHost(AppConfig.Default);
        tray.OpenSettingsRequested += () => launcher.LaunchSettingsUi();

        tray.HandleMenuCommand(VietKey.Engine.Tray.TrayIconHost.CmdOpenSettings);
        Assert.Equal(@"C:\VietKey\VietKey.UI.exe", launchedPath);

        var store = new ConfigStore(_tempDir);
        string eventName = @"Local\VietKeyUICloseReload_" + Guid.NewGuid().ToString("N");
        bool exitCalled = false;
        var app = new VietKey.UI.App(store, eventName, () => exitCalled = true);

        app.MainWindow.ApplyUiSelections(
            vietnameseEnabled: false,
            inputMethod: InputMethod.Vni,
            toggleHotkey: ToggleHotkey.CtrlShift,
            modernTone: false,
            spellCheck: true,
            autoRestoreEnglish: false,
            smartSwitch: false,
            browserAutocompleteFix: true,
            enableMacros: false,
            startWithWindows: true,
            hideDialogOnStartup: true);

        app.MainWindow.CloseAndFlush(@"C:\VietKey\VietKey.Engine.exe");
        Assert.True(app.HasExited);
        Assert.True(exitCalled);

        var savedConfig = store.Load(out _);
        Assert.False(savedConfig.VietnameseEnabled);
        Assert.Equal(InputMethod.Vni, savedConfig.InputMethod);
        Assert.Equal(ToggleHotkey.CtrlShift, savedConfig.ToggleHotkey);
        Assert.False(savedConfig.ModernTone);
        Assert.False(savedConfig.AutoRestoreEnglish);
        Assert.False(savedConfig.SmartSwitch);
        Assert.False(savedConfig.EnableMacros);
        Assert.True(savedConfig.StartWithWindows);
        Assert.True(savedConfig.HideDialogOnStartup);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }
}
