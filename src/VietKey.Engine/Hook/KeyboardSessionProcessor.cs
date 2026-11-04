using VietKey.Core.Config;
using VietKey.Core.Injection;
using VietKey.Core.Typing;

namespace VietKey.Engine.Hook;

public sealed class KeyboardSessionProcessor
{
    private readonly VietnameseTransformer _transformer = new();
    private readonly AppCompatibilityDetector _appDetector;
    private readonly SmartSwitchTracker _smartSwitch = new();
    private readonly Action<SimulatedKeyEvent[]> _batchDispatcher;
    private readonly Func<nint, bool>? _foreignImeChecker;
    private AppConfig _config;
    private IReadOnlyDictionary<string, string> _macros;

    public AppConfig CurrentConfig => _config;
    public event Action<bool>? VietnameseModeChanged;

    public KeyboardSessionProcessor(
        AppConfig initialConfig,
        IReadOnlyDictionary<string, string> initialMacros,
        Func<nint, string> processNameResolver,
        Action<SimulatedKeyEvent[]> batchDispatcher,
        Func<nint, bool>? foreignImeChecker = null)
    {
        _config = initialConfig;
        _macros = initialMacros;
        _appDetector = new AppCompatibilityDetector(processNameResolver);
        _batchDispatcher = batchDispatcher;
        _foreignImeChecker = foreignImeChecker;
    }

    public void UpdateConfig(AppConfig config, IReadOnlyDictionary<string, string> macros)
    {
        _config = config;
        _macros = macros;
        _transformer.Reset();
    }

    public void ToggleVietnameseMode(nint currentHwnd)
    {
        var (exeName, _) = _appDetector.ResolveForWindow(currentHwnd);
        bool nextMode = !_config.VietnameseEnabled;
        _config = _config with { VietnameseEnabled = nextMode };
        _smartSwitch.RecordUserToggle(exeName, nextMode);
        _transformer.Reset();
        VietnameseModeChanged?.Invoke(nextMode);
    }

    public bool ProcessKeyDown(
        int virtualKey,
        uint llkhfFlags,
        bool shiftDown,
        bool capsLockOn,
        bool ctrlDown,
        bool altDown,
        bool winDown,
        nint foregroundHwnd)
    {
        if (InputBatchBuilder.IsSyntheticInjected(llkhfFlags))
        {
            return false;
        }

        // Ignore standalone modifier key presses
        if (virtualKey is 0x10 or 0x11 or 0x12 or 0xA0 or 0xA1 or 0xA2 or 0xA3 or 0xA4 or 0xA5 or 0x5B or 0x5C)
        {
            return false;
        }

        var (exeName, appProfile) = _appDetector.ResolveForWindow(foregroundHwnd);
        bool effectiveMode = _smartSwitch.OnForegroundChanged(
            exeName,
            _config.VietnameseEnabled,
            _config.SmartSwitch,
            out bool shouldClear);

        if (shouldClear)
        {
            _transformer.Reset();
            if (effectiveMode != _config.VietnameseEnabled)
            {
                _config = _config with { VietnameseEnabled = effectiveMode };
                VietnameseModeChanged?.Invoke(effectiveMode);
            }
        }

        // Check Alt+Z global toggle hotkey
        if (_config.ToggleHotkey == ToggleHotkey.AltZ && altDown && !ctrlDown && !winDown && virtualKey == 'Z')
        {
            ToggleVietnameseMode(foregroundHwnd);
            return true;
        }

        // Bypass Vietnamese composition when OS keyboard layout is Japanese / Korean / Chinese IME (or VK_PROCESSKEY 0xE5)
        if (virtualKey == 0xE5 || (_foreignImeChecker is not null && _foreignImeChecker(foregroundHwnd)))
        {
            _transformer.Reset();
            return false;
        }

        if (InputBatchBuilder.IsNavigationOrResetKey(virtualKey, ctrlDown, altDown, winDown))
        {
            _transformer.Reset();
            return false;
        }

        if (virtualKey == InputBatchBuilder.VkBack)
        {
            var bsResult = _transformer.ProcessBackspace(_config);
            if (!bsResult.Handled)
            {
                return false;
            }

            var bsBatch = InputBatchBuilder.BuildBatch(bsResult, appProfile, _config.BrowserAutocompleteFix);
            _batchDispatcher(bsBatch);
            return true;
        }

        if (!TryMapVirtualKeyToChar(virtualKey, shiftDown, capsLockOn, out char ch))
        {
            return false;
        }

        var transformResult = _transformer.ProcessKey(ch, _config, _macros);
        if (!transformResult.Handled)
        {
            return false;
        }

        var batch = InputBatchBuilder.BuildBatch(transformResult, appProfile, _config.BrowserAutocompleteFix);
        _batchDispatcher(batch);
        return true;
    }

    private static bool TryMapVirtualKeyToChar(int vk, bool shift, bool capsLock, out char ch)
    {
        if (vk is >= 'A' and <= 'Z')
        {
            bool upper = shift ^ capsLock;
            ch = upper ? (char)vk : (char)(vk + 32);
            return true;
        }

        if (vk is >= '0' and <= '9' && !shift)
        {
            ch = (char)vk;
            return true;
        }

        if (vk == 0x20) // VK_SPACE
        {
            ch = ' ';
            return true;
        }

        if (vk == 0x0D) // VK_RETURN
        {
            ch = '\n';
            return true;
        }

        ch = '\0';
        return false;
    }
}
