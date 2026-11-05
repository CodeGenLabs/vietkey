using System.IO;
using Microsoft.Win32;
using VietKey.Core.Config;

namespace VietKey.UI.ViewModels;

public sealed class SettingsViewModel
{
    public const string RunRegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    public const string RunRegistryValueName = "VietKey";

    private readonly ConfigStore _store;
    private readonly string _reloadEventName;
    private readonly Action<bool, string>? _startupRegistrar;
    private bool _macrosDirty;

    public AppConfig Config { get; set; }
    public Dictionary<string, string> Macros { get; }
    public bool WasResetToDefault { get; }

    public SettingsViewModel(
        ConfigStore store,
        string reloadEventName = ConfigReloadCoordinator.DefaultEventName,
        Action<bool, string>? startupRegistrar = null)
    {
        _store = store;
        _reloadEventName = reloadEventName;
        _startupRegistrar = startupRegistrar;
        Config = _store.Load(out bool wasReset);
        WasResetToDefault = wasReset;
        Macros = _store.LoadMacros();
    }

    public bool TryAddOrUpdateMacro(string trigger, string expansion)
    {
        if (string.IsNullOrWhiteSpace(trigger) || string.IsNullOrEmpty(expansion))
        {
            return false;
        }

        string trimmed = trigger.Trim();
        foreach (char c in trimmed)
        {
            if (char.IsWhiteSpace(c))
            {
                return false;
            }
        }

        Macros[trimmed] = expansion;
        _macrosDirty = true;
        return true;
    }

    public bool RemoveMacro(string trigger)
    {
        if (string.IsNullOrWhiteSpace(trigger))
        {
            return false;
        }

        bool removed = Macros.Remove(trigger.Trim());
        if (removed)
        {
            _macrosDirty = true;
        }
        return removed;
    }

    public void SaveAndSignalReload(string engineExecutablePath = "VietKey.Engine.exe")
    {
        _store.Save(Config);
        if (_macrosDirty || !File.Exists(_store.MacrosFilePath))
        {
            _store.SaveMacros(Macros);
            _macrosDirty = false;
        }

        if (_startupRegistrar is not null)
        {
            _startupRegistrar(Config.StartWithWindows, engineExecutablePath);
        }
        else if (OperatingSystem.IsWindows())
        {
            UpdateWindowsStartupRegistry(Config.StartWithWindows, engineExecutablePath, Config.HideDialogOnStartup);
        }

        ConfigReloadCoordinator.SignalReload(_reloadEventName);
    }

    public static string BuildStartupRegistryCommand(string engineExecutablePath, bool hideDialogOnStartup) =>
        hideDialogOnStartup
            ? $"\"{engineExecutablePath}\""
            : $"\"{engineExecutablePath}\" --show-ui";

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public static void UpdateWindowsStartupRegistry(bool enable, string engineExecutablePath, bool hideDialogOnStartup = true)
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunRegistryKeyPath, writable: true);
            if (key is null)
            {
                return;
            }

            if (enable)
            {
                key.SetValue(RunRegistryValueName, BuildStartupRegistryCommand(engineExecutablePath, hideDialogOnStartup));
            }
            else
            {
                key.DeleteValue(RunRegistryValueName, throwOnMissingValue: false);
            }
        }
        catch
        {
            // Ignore registry permission errors in restricted environments
        }
    }
}
