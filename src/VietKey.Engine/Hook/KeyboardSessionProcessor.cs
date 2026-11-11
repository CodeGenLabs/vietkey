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
    private nint _lastForegroundHwnd;

    // Alt pressed and released on its own (or F10) opens menu / Office ribbon KeyTips: the letters that
    // follow are commands, not text, so composing them would send Backspace + Unicode into the ribbon.
    private bool _altTapCandidate;
    private bool _menuKeyTipsActive;

    // Excel: a cell started with '=' or '+' is a formula; "=IF(" must not become "=Ì(".
    private bool _caretAtFreshPosition = true;
    private bool _excelFormulaActive;

    public bool IsMenuKeyTipsActive => _menuKeyTipsActive;
    public bool IsExcelFormulaActive => _excelFormulaActive;

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

    /// <summary>
    /// Clears tracked modifier flags whose physical key is reported as released.
    /// Key-up events can be lost when focus moves to an elevated window, the secure desktop
    /// (Win+L, UAC) or when a peer IME re-injects them; without this, a stuck Alt/Win/Ctrl flag
    /// makes every following key look like a shortcut and Vietnamese composition stops.
    /// Must only be called with authoritative physical state (GetAsyncKeyState) before a non-modifier key.
    /// </summary>
    public void ReleaseStaleModifiers(bool shiftDown, bool ctrlDown, bool altDown, bool winDown)
    {
        if (!shiftDown)
        {
            _shiftPressed = false;
        }
        if (!ctrlDown)
        {
            _ctrlPressed = false;
        }
        if (!altDown)
        {
            _altPressed = false;
        }
        if (!winDown)
        {
            _winPressed = false;
        }
        if (!_shiftPressed && !_ctrlPressed && !_altPressed && !_winPressed)
        {
            _ctrlShiftChordArmed = false;
            _otherKeyPressedDuringModifier = false;
        }
    }

    /// <summary>
    /// The caret may have moved without a keystroke (mouse click, touch, pen): the composing word and
    /// the remembered previous words no longer describe the text around the caret.
    /// </summary>
    public void ResetComposition()
    {
        _transformer.Reset();
        _menuKeyTipsActive = false;
        // Clicking cells while building an Excel formula inserts references, so formula mode is kept
        _caretAtFreshPosition = true;
    }

    private void ResetCaretContext(bool keepFormula = false)
    {
        _transformer.Reset();
        _menuKeyTipsActive = false;
        _caretAtFreshPosition = true;
        if (!keepFormula)
        {
            _excelFormulaActive = false;
        }
    }

    public static bool IsModifierVirtualKey(int vk) =>
        IsShiftVirtualKey(vk) || IsCtrlVirtualKey(vk) || IsAltVirtualKey(vk) || IsWinVirtualKey(vk);

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

        if (!IsAltVirtualKey(virtualKey))
        {
            // Alt+<anything> is a shortcut, not a lone Alt tap
            _altTapCandidate = false;
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
            if (!_altPressed)
            {
                _altTapCandidate = !(shiftDown || ctrlDown || winDown || _shiftPressed || _ctrlPressed || _winPressed);
            }
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

        if (foregroundHwnd != _lastForegroundHwnd)
        {
            // Different window (even within the same app): its caret has nothing to do with our buffer
            _lastForegroundHwnd = foregroundHwnd;
            ResetCaretContext();
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

        if (_menuKeyTipsActive)
        {
            // Letters/digits drive the KeyTips; anything else means the user is back to editing text
            bool keyTipKey = virtualKey is >= 'A' and <= 'Z' or >= '0' and <= '9' or InputBatchBuilder.VkBack
                && !isCtrlActive && !isAltActive && !isWinActive;
            if (keyTipKey)
            {
                _transformer.Reset();
                return false;
            }
            _menuKeyTipsActive = false;
        }

        bool isPlainKey = !isCtrlActive && !isAltActive && !isWinActive;
        if (virtualKey == VkF10 && isPlainKey && !(shiftDown || _shiftPressed))
        {
            ResetCaretContext(keepFormula: _excelFormulaActive);
            _menuKeyTipsActive = true;
            return false;
        }

        if (virtualKey is VkEscape or VkTab && isPlainKey)
        {
            // Esc / Tab finish (or cancel) the cell in Excel, move focus elsewhere: the next key starts fresh text
            ResetCaretContext();
            return false;
        }

        if (InputBatchBuilder.IsNavigationOrResetKey(virtualKey, isCtrlActive, isAltActive, isWinActive))
        {
            // In an Excel formula, arrows / F4 / Ctrl+... point at cells and the formula continues
            ResetCaretContext(keepFormula: _excelFormulaActive && appProfile == AppCompatibilityProfile.MicrosoftExcel);
            return false;
        }

        bool isShiftActive = shiftDown || _shiftPressed;
        if (appProfile == AppCompatibilityProfile.MicrosoftExcel)
        {
            if (_caretAtFreshPosition && IsExcelFormulaStartKey(virtualKey, isShiftActive))
            {
                _excelFormulaActive = true;
            }

            if (_excelFormulaActive)
            {
                if (virtualKey == VkReturn)
                {
                    ResetCaretContext(); // Enter commits the formula
                }
                else
                {
                    _caretAtFreshPosition = false;
                    _transformer.Reset();
                }
                return false;
            }
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

        _caretAtFreshPosition = false;
        if (!TryMapVirtualKeyToChar(virtualKey, isShiftActive, capsLockOn, out char ch))
        {
            if (IsPrintableSeparatorKey(virtualKey, isShiftActive))
            {
                // Punctuation ends the word; without this the next tone key would edit the word
                // before the punctuation (and send backspaces that eat the punctuation itself)
                _transformer.ProcessUnmappedSeparator();
            }
            return false;
        }

        var transformResult = _transformer.ProcessKey(ch, _config, _macros);
        if (virtualKey == VkReturn)
        {
            // Enter still ran through the transformer (macro expansion / English restore on the last word),
            // then it commits the line / Excel cell: the next key starts fresh text
            ResetCaretContext();
        }

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
            // A peer IME may re-inject modifier releases; still honour them so tracked state can't get stuck
            if (IsModifierVirtualKey(virtualKey))
            {
                ReleaseTrackedModifier(virtualKey);
            }
            return false;
        }

        if (IsAltVirtualKey(virtualKey))
        {
            _altPressed = false;
            if (!_shiftPressed && !_ctrlPressed && !_altPressed && !_winPressed)
            {
                _otherKeyPressedDuringModifier = false;
            }

            if (_altTapCandidate)
            {
                // Lone Alt tap toggles the menu bar / Office ribbon KeyTips
                _altTapCandidate = false;
                bool sameWindow = foregroundHwnd == _lastForegroundHwnd;
                bool enterKeyTips = !(_menuKeyTipsActive && sameWindow);
                _lastForegroundHwnd = foregroundHwnd; // first key in a newly focused window may be this Alt tap
                ResetCaretContext(keepFormula: _excelFormulaActive && sameWindow);
                _menuKeyTipsActive = enterKeyTips;
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

    private void ReleaseTrackedModifier(int virtualKey)
    {
        if (IsShiftVirtualKey(virtualKey))
        {
            _shiftPressed = false;
        }
        else if (IsCtrlVirtualKey(virtualKey))
        {
            _ctrlPressed = false;
        }
        else if (IsAltVirtualKey(virtualKey))
        {
            _altPressed = false;
        }
        else if (IsWinVirtualKey(virtualKey))
        {
            _winPressed = false;
        }

        _ctrlShiftChordArmed = false;
        if (!_shiftPressed && !_ctrlPressed && !_altPressed && !_winPressed)
        {
            _otherKeyPressedDuringModifier = false;
        }
    }

    private const int VkTab = 0x09;
    private const int VkReturn = 0x0D;
    private const int VkEscape = 0x1B;
    private const int VkF10 = 0x79;

    private static bool IsExcelFormulaStartKey(int vk, bool shift) =>
        vk == 0xBB                    // '=' or '+' (VK_OEM_PLUS)
        || vk == 0x6B;                // numpad '+'

    private static bool IsPrintableSeparatorKey(int vk, bool shift) =>
        (vk is >= '0' and <= '9' && shift) // ! @ # $ % ^ & * ( )
        || vk is >= 0x60 and <= 0x6F       // numpad digits and operators
        || vk is >= 0xBA and <= 0xC0       // ; = , - . / `
        || vk is >= 0xDB and <= 0xDF       // [ \ ] '
        || vk == 0xE2;                     // OEM_102 (< > on ISO keyboards)

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
