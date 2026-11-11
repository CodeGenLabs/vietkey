using VietKey.Core.Config;
using VietKey.Engine.Hook;
using VietKey.Engine.Tray;
using VietKey.Engine.Win32;

namespace VietKey.Engine;

public static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        TrayIconHost.EnsurePerMonitorDpiAwareness();

        var settingsLauncher = new SettingsProcessLauncher();
        using var instanceGuard = new SingleInstanceGuard();
        if (!instanceGuard.IsFirstInstance)
        {
            if (args.Contains("--show-ui"))
            {
                settingsLauncher.LaunchSettingsUi();
            }
            return 0;
        }

        var store = new ConfigStore();
        using var reloadCoordinator = new ConfigReloadCoordinator(store);
        var peerArbitrator = new PeerImeArbitrator();
        var processor = new KeyboardSessionProcessor(
            reloadCoordinator.CurrentConfig,
            reloadCoordinator.CurrentMacros,
            NativeMethods.ResolveProcessImageName,
            batch => NativeMethods.DispatchBatch(batch),
            NativeMethods.IsForeignImeActive,
            peerArbitrator);

        var configSaver = new BackgroundConfigSaver(store);
        using var tray = new TrayIconHost(reloadCoordinator.CurrentConfig);
        processor.VietnameseModeChanged += enabled =>
        {
            tray.UpdateState(enabled, processor.CurrentConfig.InputMethod);
            configSaver.Enqueue(processor.CurrentConfig);
        };

        tray.ToggleRequested += () =>
        {
            processor.ToggleVietnameseMode(NativeMethods.GetForegroundWindow());
        };

        tray.InputMethodChangeRequested += method =>
        {
            var updated = processor.CurrentConfig with { InputMethod = method };
            processor.UpdateConfig(updated, reloadCoordinator.CurrentMacros);
            tray.UpdateState(updated.VietnameseEnabled, updated.InputMethod);
            configSaver.Enqueue(updated);
        };

        tray.OpenSettingsRequested += () => settingsLauncher.LaunchSettingsUi();

        reloadCoordinator.ConfigReloaded += (cfg, macros) =>
        {
            processor.UpdateConfig(cfg, macros);
            tray.UpdateState(cfg.VietnameseEnabled, cfg.InputMethod);
        };
        reloadCoordinator.StartListening();

        if (args.Contains("--verify-startup"))
        {
            return 0;
        }

        using var messageWindow = new Win32TrayMessageWindow(tray);
        messageWindow.CreateAndAttach();

        using var hook = new LowLevelKeyboardHook(processor);
        hook.Start();

        bool shouldShowUi = args.Contains("--show-ui")
            || (args.Contains("--startup") && !reloadCoordinator.CurrentConfig.HideDialogOnStartup);
        if (shouldShowUi)
        {
            settingsLauncher.LaunchSettingsUi();
        }

        messageWindow.RunMessageLoop();
        return 0;
    }
}
