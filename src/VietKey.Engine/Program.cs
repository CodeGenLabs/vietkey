using VietKey.Core.Config;
using VietKey.Engine.Hook;
using VietKey.Engine.Tray;
using VietKey.Engine.Win32;

namespace VietKey.Engine;

public static class Program
{
    public static int Main(string[] args)
    {
        using var instanceGuard = new SingleInstanceGuard();
        if (!instanceGuard.IsFirstInstance)
        {
            return 0;
        }

        var store = new ConfigStore();
        using var reloadCoordinator = new ConfigReloadCoordinator(store);
        var processor = new KeyboardSessionProcessor(
            reloadCoordinator.CurrentConfig,
            reloadCoordinator.CurrentMacros,
            NativeMethods.ResolveProcessImageName,
            batch => NativeMethods.DispatchBatch(batch));

        using var tray = new TrayIconHost(reloadCoordinator.CurrentConfig);
        processor.VietnameseModeChanged += enabled =>
            tray.UpdateState(enabled, processor.CurrentConfig.InputMethod);

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

        using var hook = new LowLevelKeyboardHook(processor);
        hook.Start();
        return 0;
    }
}
