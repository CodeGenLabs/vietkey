using VietKey.Core.Config;
using VietKey.Core.Injection;
using VietKey.Engine.Hook;
using VietKey.Engine.Win32;
using Xunit;

namespace VietKey.Core.Tests.Hook;

public sealed class KeyboardSessionProcessorTests
{
    // @covers REQ-os-1
    [Fact]
    public void ProcessKeyDown_TransformsVietnameseAndDispatchesSingleBatch_WhileIgnoringInjectedKeys()
    {
        var dispatchedBatches = new List<SimulatedKeyEvent[]>();
        var processor = new KeyboardSessionProcessor(
            AppConfig.Default,
            new Dictionary<string, string>(),
            hwnd => hwnd == 10 ? "chrome.exe" : "code.exe",
            batch => dispatchedBatches.Add(batch));

        // 1. Injected key is ignored
        bool suppressedInjected = processor.ProcessKeyDown(
            'A',
            InputBatchBuilder.LlkhfInjected,
            shiftDown: false,
            capsLockOn: false,
            ctrlDown: false,
            altDown: false,
            winDown: false,
            foregroundHwnd: 20);
        Assert.False(suppressedInjected);
        Assert.Empty(dispatchedBatches);

        // 2. Type 'a' then 's' in VS Code (hwnd 20 -> code.exe)
        bool s1 = processor.ProcessKeyDown('A', 0, false, false, false, false, false, 20);
        Assert.False(s1);
        Assert.Empty(dispatchedBatches);

        bool s2 = processor.ProcessKeyDown('S', 0, false, false, false, false, false, 20);
        Assert.True(s2);
        Assert.Single(dispatchedBatches);
        Assert.Equal(4, dispatchedBatches[0].Length); // 1 Backspace + 'á'

        var win32Inputs = NativeMethods.ToWin32Inputs(dispatchedBatches[0]);
        Assert.Equal(4, win32Inputs.Length);
        Assert.Equal(NativeMethods.KEYEVENTF_UNICODE, win32Inputs[2].U.ki.dwFlags);
        Assert.Equal('á', (char)win32Inputs[2].U.ki.wScan);
    }

    // @covers REQ-os-1
    [Fact]
    public void ProcessKeyDown_RecoversVietnameseTyping_WhenModifierKeyUpWasLostDuringAppSwitch()
    {
        var dispatchedBatches = new List<SimulatedKeyEvent[]>();
        var processor = new KeyboardSessionProcessor(
            AppConfig.Default,
            new Dictionary<string, string>(),
            hwnd => hwnd == 10 ? "chrome.exe" : "code.exe",
            batch => dispatchedBatches.Add(batch));

        // Alt+Tab / Win+L into an elevated window or the secure desktop: key-downs seen, key-ups never delivered
        processor.ProcessKeyDown(0xA4, 0, false, false, false, true, false, 10); // VK_LMENU down
        processor.ProcessKeyDown(0x5B, 0, false, false, false, true, true, 10);  // VK_LWIN down

        // Without reconciliation the stuck flags turn every key into a shortcut
        Assert.False(processor.ProcessKeyDown('A', 0, false, false, false, false, false, 20));
        Assert.False(processor.ProcessKeyDown('S', 0, false, false, false, false, false, 20));
        Assert.Empty(dispatchedBatches);

        // The hook reconciles with the physical key state (all modifiers released) before each non-modifier key
        processor.ReleaseStaleModifiers(false, false, false, false);
        Assert.False(processor.ProcessKeyDown('A', 0, false, false, false, false, false, 20));
        processor.ReleaseStaleModifiers(false, false, false, false);
        Assert.True(processor.ProcessKeyDown('S', 0, false, false, false, false, false, 20));
        Assert.Single(dispatchedBatches);
    }

    // @covers REQ-os-1
    [Fact]
    public void ProcessKeyUp_InjectedModifierRelease_ClearsTrackedModifier()
    {
        var dispatchedBatches = new List<SimulatedKeyEvent[]>();
        var processor = new KeyboardSessionProcessor(
            AppConfig.Default,
            new Dictionary<string, string>(),
            _ => "code.exe",
            batch => dispatchedBatches.Add(batch));

        processor.ProcessKeyDown(0xA4, 0, false, false, false, true, false, 20);
        // A peer IME re-injects the Alt release
        processor.ProcessKeyUp(0xA4, InputBatchBuilder.LlkhfInjected, false, false, false, false, 20);

        Assert.False(processor.ProcessKeyDown('A', 0, false, false, false, false, false, 20));
        Assert.True(processor.ProcessKeyDown('S', 0, false, false, false, false, false, 20));
        Assert.Single(dispatchedBatches);
    }

    // @covers REQ-os-1
    [Fact]
    public void ProcessKeyDown_WhenJapaneseOrCjkImeIsActive_BypassesHiraganaButAllowsHalfWidthAlphanumeric()
    {
        nint japaneseHkl = 0x04110411;
        Assert.True(NativeMethods.IsCjkKeyboardLayout(japaneseHkl)); // Japanese (0x0411)
        Assert.True(NativeMethods.IsCjkKeyboardLayout(0x04120412));  // Korean (0x0412)
        Assert.True(NativeMethods.IsCjkKeyboardLayout(0x08040804));  // Chinese Simplified (0x0804)
        Assert.False(NativeMethods.IsCjkKeyboardLayout(0x04090409)); // English US (0x0409)
        Assert.False(NativeMethods.IsCjkKeyboardLayout(0x042A042A)); // Vietnamese (0x042A)

        // Verify Japanese IME modes:
        // 1. Hiragana ('あ'): open=1, conv=IME_CMODE_NATIVE | IME_CMODE_FULLSHAPE | IME_CMODE_ROMAN (0x19) -> bypass (true)
        Assert.True(NativeMethods.IsCjkConversionActive(
            japaneseHkl,
            imeOpenStatus: 1,
            imeConversionMode: NativeMethods.IME_CMODE_NATIVE | NativeMethods.IME_CMODE_FULLSHAPE | NativeMethods.IME_CMODE_ROMAN));

        // 2. Full-width Katakana ('カ'): open=1, conv=0x1B -> bypass (true)
        Assert.True(NativeMethods.IsCjkConversionActive(
            japaneseHkl,
            imeOpenStatus: 1,
            imeConversionMode: NativeMethods.IME_CMODE_NATIVE | NativeMethods.IME_CMODE_KATAKANA | NativeMethods.IME_CMODE_FULLSHAPE));

        // 3. Full-width Alphanumeric ('Ａ'): open=1, conv=IME_CMODE_FULLSHAPE (0x08) -> bypass (true)
        Assert.True(NativeMethods.IsCjkConversionActive(
            japaneseHkl,
            imeOpenStatus: 1,
            imeConversionMode: NativeMethods.IME_CMODE_FULLSHAPE));

        // 4. Half-width Katakana ('ｶ'): open=1, conv=IME_CMODE_NATIVE | IME_CMODE_KATAKANA (0x03) -> bypass (true)
        Assert.True(NativeMethods.IsCjkConversionActive(
            japaneseHkl,
            imeOpenStatus: 1,
            imeConversionMode: NativeMethods.IME_CMODE_NATIVE | NativeMethods.IME_CMODE_KATAKANA));

        // 5. Half-width Alphanumeric ('A' / Direct Input): open=0 -> allow Vietnamese (false)
        Assert.False(NativeMethods.IsCjkConversionActive(
            japaneseHkl,
            imeOpenStatus: 0,
            imeConversionMode: 0));

        // 6. Half-width Alphanumeric ('A' with open=1, conv=IME_CMODE_ROMAN 0x10 only) -> allow Vietnamese (false)
        Assert.False(NativeMethods.IsCjkConversionActive(
            japaneseHkl,
            imeOpenStatus: 1,
            imeConversionMode: NativeMethods.IME_CMODE_ROMAN));

        var dispatchedBatches = new List<SimulatedKeyEvent[]>();
        nint currentImeOpenStatus = 1;
        nint currentImeConvMode = NativeMethods.IME_CMODE_NATIVE | NativeMethods.IME_CMODE_FULLSHAPE | NativeMethods.IME_CMODE_ROMAN;

        var processor = new KeyboardSessionProcessor(
            AppConfig.Default,
            new Dictionary<string, string>(),
            _ => "chrome.exe",
            batch => dispatchedBatches.Add(batch),
            _ => NativeMethods.IsCjkConversionActive(japaneseHkl, currentImeOpenStatus, currentImeConvMode));

        // Type "kanji" (K, A, N, J, I) while Japanese Hiragana ('あ') is active -> zero suppression, zero batches
        foreach (char key in "KANJI")
        {
            bool suppressed = processor.ProcessKeyDown(key, 0, false, false, false, false, false, 10);
            Assert.False(suppressed);
        }
        Assert.Empty(dispatchedBatches);

        // Switch Japanese IME to Half-width Alphanumeric ('A', open=0) -> typing "kanj" transforms to "kạn"
        currentImeOpenStatus = 0;
        Assert.False(processor.ProcessKeyDown('K', 0, false, false, false, false, false, 10));
        Assert.False(processor.ProcessKeyDown('A', 0, false, false, false, false, false, 10));
        Assert.False(processor.ProcessKeyDown('N', 0, false, false, false, false, false, 10));
        Assert.True(processor.ProcessKeyDown('J', 0, false, false, false, false, false, 10));
        Assert.Single(dispatchedBatches);
    }

    // @covers REQ-settings-1
    [Fact]
    public void SingleInstanceMutexAndGlobalHotkey_TogglesModeOnAltZAndPreventsDuplicateInstance()
    {
        string mutexName = @"Local\VietKeyTestMutex_" + Guid.NewGuid().ToString("N");
        using var firstGuard = new VietKey.Engine.Tray.SingleInstanceGuard(mutexName);
        using var secondGuard = new VietKey.Engine.Tray.SingleInstanceGuard(mutexName);
        Assert.True(firstGuard.IsFirstInstance);
        Assert.False(secondGuard.IsFirstInstance);

        var processor = new KeyboardSessionProcessor(
            AppConfig.Default with { VietnameseEnabled = true, ToggleHotkey = ToggleHotkey.AltZ },
            new Dictionary<string, string>(),
            _ => "chrome.exe",
            _ => { });

        using var tray = new VietKey.Engine.Tray.TrayIconHost(processor.CurrentConfig);
        processor.VietnameseModeChanged += enabled => tray.UpdateState(enabled, processor.CurrentConfig.InputMethod);

        Assert.True(tray.RenderedIconSize >= 16);
        tray.RefreshThemeIfNeeded(forceLightTheme: false);
        Assert.False(tray.IsLightThemeActive);
        nint iconVDark = tray.ActiveIconHandle;
        Assert.NotEqual(0, iconVDark);

        Assert.True(tray.RefreshThemeIfNeeded(forceLightTheme: true));
        Assert.True(tray.IsLightThemeActive);
        nint iconVLight = tray.ActiveIconHandle;
        Assert.NotEqual(0, iconVLight);
        Assert.NotEqual(iconVDark, iconVLight);

        // Press Alt down (0xA4) then Z (with altDown=false to simulate background thread GetKeyState=0, relying on tracked _altPressed)
        processor.ProcessKeyDown(0xA4, 0, false, false, false, false, false, 10);
        bool handled = processor.ProcessKeyDown(
            'Z',
            llkhfFlags: 0,
            shiftDown: false,
            capsLockOn: false,
            ctrlDown: false,
            altDown: false,
            winDown: false,
            foregroundHwnd: 10);
        processor.ProcessKeyUp(0xA4, 0, false, false, false, false, 10);

        Assert.True(handled);
        Assert.False(processor.CurrentConfig.VietnameseEnabled);
        Assert.False(tray.IsVietnameseActive);
        nint iconELight = tray.ActiveIconHandle;
        Assert.NotEqual(0, iconELight);
        Assert.NotEqual(iconVLight, iconELight);

        Assert.True(tray.RefreshThemeIfNeeded(forceLightTheme: false));
        nint iconEDark = tray.ActiveIconHandle;
        Assert.NotEqual(0, iconEDark);
        Assert.NotEqual(iconELight, iconEDark);
        Assert.NotEqual(iconVDark, iconEDark);
        Assert.Contains("Tiếng Anh (E)", tray.GetTooltipText());
    }

    // @covers REQ-settings-1
    [Fact]
    public void CtrlShiftHotkeyAndEvKeyCoexistence_RespectsStartOrderPriorityAndWorksAfterAltTabAndCapitalLetters()
    {
        var vietKeyStartUtc = new DateTime(2026, 11, 4, 8, 0, 0, DateTimeKind.Utc);
        var activePeers = new List<PeerImeProcessInfo>
        {
            // Case 1: EVKey was opened BEFORE VietKey (07:55 UTC < 08:00 UTC)
            new(ProcessId: 1001, ProcessName: "EVKey64", StartTimeUtc: vietKeyStartUtc.AddMinutes(-5))
        };

        var dispatchedBatches = new List<SimulatedKeyEvent[]>();
        var arbitrator = new PeerImeArbitrator(vietKeyStartUtc, () => activePeers);
        var processor = new KeyboardSessionProcessor(
            AppConfig.Default with { VietnameseEnabled = true, ToggleHotkey = ToggleHotkey.CtrlShift },
            new Dictionary<string, string>(),
            hwnd => hwnd == 99 ? "explorer.exe" : "chrome.exe",
            batch => dispatchedBatches.Add(batch),
            peerArbitrator: arbitrator);

        // 1. While older EVKey is running, VietKey yields Ctrl+Shift to EVKey
        processor.ProcessKeyDown(0xA2, 0, false, false, false, false, false, 10); // VK_LCONTROL down
        processor.ProcessKeyDown(0xA0, 0, false, false, true, false, false, 10);  // VK_LSHIFT down
        bool toggledWhileOlderEvKeyRunning = processor.ProcessKeyUp(0xA0, 0, false, true, false, false, 10);
        processor.ProcessKeyUp(0xA2, 0, false, false, false, false, 10);
        Assert.False(toggledWhileOlderEvKeyRunning);
        Assert.True(processor.CurrentConfig.VietnameseEnabled);

        // 2. Older EVKey closes -> simulate prior Alt+Tab, Win+D, Shift+A, and Ctrl+C before pressing Ctrl+Shift
        activePeers.Clear();

        // Simulate Alt+Tab
        processor.ProcessKeyDown(0xA4, 0, false, false, false, true, false, 10);
        processor.ProcessKeyDown(0x09, 0, false, false, false, true, false, 10);
        processor.ProcessKeyUp(0xA4, 0, false, false, false, false, 10);

        // Simulate Win+D
        processor.ProcessKeyDown(0x5B, 0, false, false, false, false, true, 10);
        processor.ProcessKeyDown('D', 0, false, false, false, false, true, 10);
        processor.ProcessKeyUp(0x5B, 0, false, false, false, false, 10);

        // Simulate typing capital 'V' (Shift+V) where WM_KEYUP still reports shiftDown=true in hook pre-processing
        processor.ProcessKeyDown(0xA0, 0, true, false, false, false, false, 10);
        processor.ProcessKeyDown('V', 0, true, false, false, false, false, 10);
        processor.ProcessKeyUp(0xA0, 0, true, false, false, false, 10);

        // Now press Ctrl+Shift -> must toggle VietnameseEnabled from true to false!
        processor.ProcessKeyDown(0xA2, 0, false, false, false, false, false, 10); // VK_LCONTROL down
        processor.ProcessKeyDown(0xA0, 0, false, false, true, false, false, 10);  // VK_LSHIFT down
        bool toggledAfterEvKeyClosed = processor.ProcessKeyUp(0xA0, 0, false, true, false, false, 10);
        processor.ProcessKeyUp(0xA2, 0, false, false, false, false, 10);
        Assert.True(toggledAfterEvKeyClosed);
        Assert.False(processor.CurrentConfig.VietnameseEnabled);

        // 3. Pressing Ctrl+Shift+T (with intervening key 'T') must NOT toggle mode
        processor.ProcessKeyDown(0xA2, 0, false, false, false, false, false, 10);
        processor.ProcessKeyDown(0xA0, 0, false, false, true, false, false, 10);
        processor.ProcessKeyDown('T', 0, true, false, true, false, false, 10);
        bool toggledOnCtrlShiftT = processor.ProcessKeyUp(0xA0, 0, false, true, false, false, 10);
        processor.ProcessKeyUp(0xA2, 0, false, false, false, false, 10);
        Assert.False(toggledOnCtrlShiftT);
        Assert.False(processor.CurrentConfig.VietnameseEnabled);

        // 4. Newer EVKey starts AFTER VietKey (08:10 UTC > 08:00 UTC) -> VietKey retains priority and disarms EVKey chord
        activePeers.Add(new PeerImeProcessInfo(ProcessId: 2002, ProcessName: "EVKey64", StartTimeUtc: vietKeyStartUtc.AddMinutes(10)));
        var decision = arbitrator.Evaluate(bypassCache: true);
        Assert.False(decision.ShouldYieldToOlderPeer);
        Assert.True(decision.HasNewerPeer);
        Assert.Equal(2002, decision.NewerPeerToRehookPid);
        arbitrator.MarkRehooked(2002);

        processor.ProcessKeyDown(0xA0, 0, false, false, false, false, false, 10); // VK_LSHIFT down first
        processor.ProcessKeyDown(0xA2, 0, true, false, false, false, false, 10);  // VK_LCONTROL down second
        bool toggledOverNewerEvKey = processor.ProcessKeyUp(0xA2, 0, true, false, false, false, 10);
        processor.ProcessKeyUp(0xA0, 0, false, false, false, false, 10);
        Assert.True(toggledOverNewerEvKey);
        Assert.True(processor.CurrentConfig.VietnameseEnabled);
        Assert.Single(dispatchedBatches);
        Assert.Equal(KeyboardSessionProcessor.VkNeutralChordBreaker, dispatchedBatches[0][0].VirtualKey);
    }

    // @covers REQ-settings-1
    [Fact]
    public void SystemTrayCallback_RapidLeftClicksOnlyToggleLanguageWithoutOpeningSettingsAndRightClickShowsMenu()
    {
        using var tray = new VietKey.Engine.Tray.TrayIconHost(AppConfig.Default);
        int toggleCount = 0;
        bool openSettingsCalled = false;
        bool contextMenuCalled = false;

        tray.ToggleRequested += () => toggleCount++;
        tray.OpenSettingsRequested += () => openSettingsCalled = true;
        tray.ContextMenuRequested += () => contextMenuCalled = true;

        // Simulate rapid left clicks (WM_LBUTTONUP -> WM_LBUTTONDBLCLK -> WM_LBUTTONUP)
        tray.HandleMouseMessage(VietKey.Engine.Tray.TrayIconHost.WM_LBUTTONUP);
        tray.HandleMouseMessage(VietKey.Engine.Tray.TrayIconHost.WM_LBUTTONDBLCLK);
        tray.HandleMouseMessage(VietKey.Engine.Tray.TrayIconHost.WM_LBUTTONUP);

        Assert.Equal(2, toggleCount);
        Assert.False(openSettingsCalled);

        tray.HandleMouseMessage(VietKey.Engine.Tray.TrayIconHost.WM_RBUTTONUP);
        Assert.True(contextMenuCalled);

        tray.HandleMenuCommand(VietKey.Engine.Tray.TrayIconHost.CmdOpenSettings);
        Assert.True(openSettingsCalled);
    }

    // @covers REQ-os-1
    [Fact]
    public void ResetCompositionAndPunctuation_StopToneKeysFromRewritingThePreviousWord()
    {
        var dispatchedBatches = new List<SimulatedKeyEvent[]>();
        var processor = new KeyboardSessionProcessor(
            AppConfig.Default,
            new Dictionary<string, string>(),
            _ => "code.exe",
            batch => dispatchedBatches.Add(batch));

        void Type(string keys)
        {
            foreach (char c in keys)
            {
                processor.ProcessKeyDown(char.ToUpperInvariant(c), 0, false, false, false, false, false, 20);
            }
        }

        // Mouse click moves the caret into older text
        Type("duowng");
        dispatchedBatches.Clear();
        processor.ResetComposition();
        Assert.False(processor.ProcessKeyDown('F', 0, false, false, false, false, false, 20));
        Assert.Empty(dispatchedBatches);

        // Punctuation (VK_OEM_COMMA) ends the word
        processor.ResetComposition();
        Type("duowng");
        dispatchedBatches.Clear();
        Assert.False(processor.ProcessKeyDown(0xBC, 0, false, false, false, false, false, 20));
        Assert.False(processor.ProcessKeyDown('F', 0, false, false, false, false, false, 20));
        Assert.Empty(dispatchedBatches);

        // Switching to another window of the same app also drops the buffer
        processor.ResetComposition();
        Type("duowng");
        dispatchedBatches.Clear();
        Assert.False(processor.ProcessKeyDown('F', 0, false, false, false, false, false, 21));
        Assert.Empty(dispatchedBatches);
    }

    private static (KeyboardSessionProcessor Processor, List<SimulatedKeyEvent[]> Batches) CreateExcelProcessor()
    {
        var batches = new List<SimulatedKeyEvent[]>();
        var processor = new KeyboardSessionProcessor(
            AppConfig.Default,
            new Dictionary<string, string>(),
            _ => @"C:\Program Files\Microsoft Office\root\Office16\EXCEL.EXE",
            batch => batches.Add(batch));
        return (processor, batches);
    }

    private static void TypeLetters(KeyboardSessionProcessor processor, string keys)
    {
        foreach (char c in keys)
        {
            processor.ProcessKeyDown(char.ToUpperInvariant(c), 0, false, false, false, false, false, 30);
            processor.ProcessKeyUp(char.ToUpperInvariant(c), 0, false, false, false, false, 30);
        }
    }

    private static void Tap(KeyboardSessionProcessor processor, int vk, bool shift = false, bool ctrl = false)
    {
        processor.ProcessKeyDown(vk, 0, shift, false, ctrl, false, false, 30);
        processor.ProcessKeyUp(vk, 0, shift, ctrl, false, false, 30);
    }

    // @covers REQ-os-1
    [Fact]
    public void Excel_LoneAltTapKeyTips_AreNeverComposed_UntilUserLeavesTheRibbon()
    {
        var (processor, batches) = CreateExcelProcessor();

        // Alt, H, O, W (ribbon KeyTips) - "ow" must not become "ơ" (Backspace + Unicode sent to the ribbon)
        processor.ProcessKeyDown(0xA4, 0, false, false, false, false, false, 30);
        processor.ProcessKeyUp(0xA4, 0, false, false, false, false, 30);
        Assert.True(processor.IsMenuKeyTipsActive);
        TypeLetters(processor, "how");
        TypeLetters(processor, "es"); // Alt E S (legacy Paste Special)
        Assert.Empty(batches);

        // Esc / arrow / click returns to the grid: typing composes again
        Tap(processor, 0x1B);
        Assert.False(processor.IsMenuKeyTipsActive);
        TypeLetters(processor, "duowng");
        Assert.NotEmpty(batches);

        // F10 also opens KeyTips
        batches.Clear();
        Tap(processor, 0x79);
        Assert.True(processor.IsMenuKeyTipsActive);
        TypeLetters(processor, "aw");
        Assert.Empty(batches);
        processor.ResetComposition(); // mouse click
        Assert.False(processor.IsMenuKeyTipsActive);
    }

    // @covers REQ-os-1
    [Fact]
    public void Excel_AltChordShortcuts_DoNotEnterKeyTipsMode()
    {
        var (processor, batches) = CreateExcelProcessor();

        // Alt+Enter (new line in cell), Alt+= (AutoSum), Alt+Z (toggle) are chords, not lone taps
        processor.ProcessKeyDown(0xA4, 0, false, false, false, false, false, 30);
        processor.ProcessKeyDown(0x0D, 0, false, false, false, true, false, 30);
        processor.ProcessKeyUp(0x0D, 0, false, false, true, false, 30);
        processor.ProcessKeyUp(0xA4, 0, false, false, false, false, 30);
        Assert.False(processor.IsMenuKeyTipsActive);

        TypeLetters(processor, "vieetj");
        Assert.NotEmpty(batches);
    }

    // @covers REQ-os-1
    [Fact]
    public void Excel_FormulaCells_AreNotComposed_ButTextCellsAre()
    {
        var (processor, batches) = CreateExcelProcessor();

        // =IF(A1;"x") : 'f' after 'i' must not become "ì"
        Tap(processor, 0xBB); // '='
        Assert.True(processor.IsExcelFormulaActive);
        TypeLetters(processor, "if");
        Tap(processor, '9', shift: true); // '('
        TypeLetters(processor, "a");
        Tap(processor, 0x25); // Left arrow points at cells; still the same formula
        TypeLetters(processor, "s");
        Assert.True(processor.IsExcelFormulaActive);
        Assert.Empty(batches);

        // Enter commits the formula; next cell is normal Vietnamese text
        Tap(processor, 0x0D);
        Assert.False(processor.IsExcelFormulaActive);
        TypeLetters(processor, "tieengs");
        Assert.NotEmpty(batches);

        // '=' typed in the middle of text is not a formula
        batches.Clear();
        Tap(processor, 0x0D);
        TypeLetters(processor, "a");
        Tap(processor, 0xBB);
        Assert.False(processor.IsExcelFormulaActive);
        TypeLetters(processor, "as");
        Assert.NotEmpty(batches);

        // Esc cancels a formula
        Tap(processor, 0x0D);
        Tap(processor, 0x6B); // numpad '+'
        Assert.True(processor.IsExcelFormulaActive);
        Tap(processor, 0x1B);
        Assert.False(processor.IsExcelFormulaActive);
    }

    // @covers REQ-os-1
    [Fact]
    public void Excel_ShortcutsAndNavigation_DropTheBufferSoNoOldWordIsRewritten()
    {
        var (processor, batches) = CreateExcelProcessor();

        foreach (var (vk, ctrl) in new (int, bool)[]
        {
            (0x25, false), (0x28, false), (0x09, false), (0x71, false), // Left, Down, Tab, F2
            ('C', true), ('Z', true), (0x24, true)                      // Ctrl+C, Ctrl+Z, Ctrl+Home
        })
        {
            processor.ResetComposition();
            TypeLetters(processor, "duowng");
            batches.Clear();

            if (ctrl)
            {
                processor.ProcessKeyDown(0xA2, 0, false, false, false, false, false, 30);
                processor.ProcessKeyDown(vk, 0, false, false, true, false, false, 30);
                processor.ProcessKeyUp(vk, 0, false, true, false, false, 30);
                processor.ProcessKeyUp(0xA2, 0, false, false, false, false, 30);
            }
            else
            {
                Tap(processor, vk);
            }

            Assert.False(processor.ProcessKeyDown('F', 0, false, false, false, false, false, 30));
            Assert.Empty(batches);
        }
    }
}
