using VietKey.Core.Config;
using VietKey.Core.Injection;
using VietKey.Core.Typing;

namespace VietKey.Engine.Hook;

public sealed class KeyboardSessionProcessor
{
    public const ushort VkNeutralChordBreaker = 0xFF;

    private readonly VietnameseTransformer _transformer = new();
    private readonly AppCompatibilityDetector _appDetector;
    private readonly SmartSwitchTracker _smartSwitch = new();
    private readonly Action<SimulatedKeyEvent[]> _batchDispatcher;
    private readonly Func<nint, bool>? _foreignImeChecker;
    private readonly PeerImeArbitrator? _peerArbitrator;

    private AppConfig _config;
    private IReadOnlyDictionary<string, string> _macros;
    private bool _shiftPressed;
    private bool _ctrlPressed;
    private bool _altPressed;
    private bool _winPressed;
    private bool _ctrlShiftChordArmed;
    private bool _otherKeyPressedDuringModifier;

    public AppConfig CurrentConfig => _config;
    public PeerImeArbitrator? PeerArbitrator => _peerArbitrator;
    public event Action<bool>? VietnameseModeChanged;

    public KeyboardSessionProcessor(
        AppConfig initialConfig,
        IReadOnlyDictionary<string, string> initialMacros,
        Func<nint, string> processNameResolver,
        Action<SimulatedKeyEvent[]> batchDispatcher,
        Func<nint, bool>? foreignImeChecker = null,
        PeerImeArbitrator? peerArbitrator = null)
    {
        _config = initialConfig;
        _macros = initialMacros;
        _appDetector = new AppCompatibilityDetector(processNameResolver);
        _batchDispatcher = batchDispatcher;
        _foreignImeChecker = foreignImeChecker;
        _peerArbitrator = peerArbitrator;
    }

    public void UpdateConfig(AppConfig config, IReadOnlyDictionary<string, string> macros)
    {
        _config = config;
        _macros = macros;
        _smartSwitch.SyncGlobalMode(config.VietnameseEnabled);
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

    public static bool IsShiftVirtualKey(int vk) => vk is 0x10 or 0xA0 or 0xA1;
    public static bool IsCtrlVirtualKey(int vk) => vk is 0x11 or 0xA2 or 0xA3;
    public static bool IsAltVirtualKey(int vk) => vk is 0x12 or 0xA4 or 0xA5;
    public static bool IsWinVirtualKey(int vk) => vk is 0x5B or 0x5C;

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

        if (IsShiftVirtualKey(virtualKey))
        {
            if (!_shiftPressed && !_ctrlPressed && !ctrlDown)
            {
                _otherKeyPressedDuringModifier = false;
            }

            _shiftPressed = true;
            bool effectiveCtrl = ctrlDown || _ctrlPressed;
            bool effectiveAlt = altDown || _altPressed;
            bool effectiveWin = winDown || _winPressed;
            if (effectiveCtrl && !effectiveAlt && !effectiveWin && !_otherKeyPressedDuringModifier)
            {
                _ctrlShiftChordArmed = true;
            }
            return false;
        }

        if (IsCtrlVirtualKey(virtualKey))
        {
            if (!_ctrlPressed && !_shiftPressed && !shiftDown)
            {
                _otherKeyPressedDuringModifier = false;
            }

            _ctrlPressed = true;
            bool effectiveShift = shiftDown || _shiftPressed;
            bool effectiveAlt = altDown || _altPressed;
            bool effectiveWin = winDown || _winPressed;
            if (effectiveShift && !effectiveAlt && !effectiveWin && !_otherKeyPressedDuringModifier)
            {
                _ctrlShiftChordArmed = true;
            }
            return false;
        }

        if (IsAltVirtualKey(virtualKey))
        {
            _altPressed = true;
            _ctrlShiftChordArmed = false;
            _otherKeyPressedDuringModifier = true;
            return false;
        }

        if (IsWinVirtualKey(virtualKey))
        {
            _winPressed = true;
            _ctrlShiftChordArmed = false;
            _otherKeyPressedDuringModifier = true;
            return false;
        }

        // Any non-modifier key cancels an armed Ctrl+Shift toggle chord
        _ctrlShiftChordArmed = false;
        if (shiftDown || ctrlDown || altDown || winDown || _shiftPressed || _ctrlPressed || _altPressed || _winPressed)
        {
            _otherKeyPressedDuringModifier = true;
        }

        if (_peerArbitrator is not null && _peerArbitrator.Evaluate(bypassCache: false).ShouldYieldToOlderPeer)
        {
            _transformer.Reset();
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
        bool isAltActive = altDown || _altPressed;
        bool isCtrlActive = ctrlDown || _ctrlPressed;
        bool isWinActive = winDown || _winPressed;
        if (_config.ToggleHotkey == ToggleHotkey.AltZ && isAltActive && !isCtrlActive && !isWinActive && virtualKey == 'Z')
        {
            if (_peerArbitrator is not null && _peerArbitrator.Evaluate(bypassCache: true).ShouldYieldToOlderPeer)
            {
                return false;
            }

            ToggleVietnameseMode(foregroundHwnd);
            return true;
        }

        // Bypass Vietnamese composition when OS keyboard layout is Japanese / Korean / Chinese IME (or VK_PROCESSKEY 0xE5)
        if (virtualKey == 0xE5 || (_foreignImeChecker is not null && _foreignImeChecker(foregroundHwnd)))
        {
            _transformer.Reset();
            return false;
        }

        if (InputBatchBuilder.IsNavigationOrResetKey(virtualKey, isCtrlActive, isAltActive, isWinActive))
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

        bool isShiftActive = shiftDown || _shiftPressed;
        if (!TryMapVirtualKeyToChar(virtualKey, isShiftActive, capsLockOn, out char ch))
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

    public bool ProcessKeyUp(
        int virtualKey,
        uint llkhfFlags,
        bool shiftDown,
        bool ctrlDown,
        bool altDown,
        bool winDown,
        nint foregroundHwnd)
    {
        if (InputBatchBuilder.IsSyntheticInjected(llkhfFlags))
        {
            return false;
        }

        if (IsAltVirtualKey(virtualKey))
        {
            _altPressed = false;
            if (!_shiftPressed && !_ctrlPressed && !_altPressed && !_winPressed)
            {
                _otherKeyPressedDuringModifier = false;
            }
            return false;
        }

        if (IsWinVirtualKey(virtualKey))
        {
            _winPressed = false;
            if (!_shiftPressed && !_ctrlPressed && !_altPressed && !_winPressed)
            {
                _otherKeyPressedDuringModifier = false;
            }
            return false;
        }

        bool isShift = IsShiftVirtualKey(virtualKey);
        bool isCtrl = IsCtrlVirtualKey(virtualKey);
        if (!isShift && !isCtrl)
        {
            return false;
        }

        if (isShift)
        {
            _shiftPressed = false;
        }

        if (isCtrl)
        {
            _ctrlPressed = false;
        }

        bool wasArmed = _ctrlShiftChordArmed;
        bool hadOtherKey = _otherKeyPressedDuringModifier;
        _ctrlShiftChordArmed = false;

        if (!_shiftPressed && !_ctrlPressed)
        {
            _otherKeyPressedDuringModifier = false;
        }

        bool effectiveAlt = altDown || _altPressed;
        bool effectiveWin = winDown || _winPressed;
        if (!wasArmed || hadOtherKey || _config.ToggleHotkey != ToggleHotkey.CtrlShift || effectiveAlt || effectiveWin)
        {
            return false;
        }

        if (_peerArbitrator is not null)
        {
            var decision = _peerArbitrator.Evaluate(bypassCache: true);
            if (decision.ShouldYieldToOlderPeer)
            {
                return false;
            }

            if (decision.HasNewerPeer)
            {
                // Disarm downstream peer IME's Ctrl+Shift chord so only VietKey toggles
                _batchDispatcher(
                [
                    new SimulatedKeyEvent(VkNeutralChordBreaker, 0, IsUnicode: false, IsKeyUp: false),
                    new SimulatedKeyEvent(VkNeutralChordBreaker, 0, IsUnicode: false, IsKeyUp: true)
                ]);
            }
        }

        ToggleVietnameseMode(foregroundHwnd);
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
