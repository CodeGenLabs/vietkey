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
    public void ProcessKeyDown_WhenJapaneseOrCjkImeIsActive_BypassesVietnameseTransformationForKanji()
    {
        Assert.True(NativeMethods.IsCjkKeyboardLayout(0x04110411)); // Japanese (0x0411)
        Assert.True(NativeMethods.IsCjkKeyboardLayout(0x04120412)); // Korean (0x0412)
        Assert.True(NativeMethods.IsCjkKeyboardLayout(0x08040804)); // Chinese Simplified (0x0804)
        Assert.False(NativeMethods.IsCjkKeyboardLayout(0x04090409)); // English US (0x0409)
        Assert.False(NativeMethods.IsCjkKeyboardLayout(0x042A042A)); // Vietnamese (0x042A)

        var dispatchedBatches = new List<SimulatedKeyEvent[]>();
        bool isJapaneseImeActive = true;

        var processor = new KeyboardSessionProcessor(
            AppConfig.Default,
            new Dictionary<string, string>(),
            _ => "chrome.exe",
            batch => dispatchedBatches.Add(batch),
            _ => isJapaneseImeActive);

        // Type "kanji" (K, A, N, J, I) while Japanese IME is active -> zero suppression, zero batches
        foreach (char key in "KANJI")
        {
            bool suppressed = processor.ProcessKeyDown(key, 0, false, false, false, false, false, 10);
            Assert.False(suppressed);
        }
        Assert.Empty(dispatchedBatches);

        // Switch back to English layout (0x0409) -> typing "kanj" transforms to "kạn"
        isJapaneseImeActive = false;
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

        nint iconHandleV = tray.ActiveIconHandle;
        Assert.NotEqual(0, iconHandleV);

        // Press Alt+Z -> flips from Vietnamese (true) to English (false)
        bool handled = processor.ProcessKeyDown(
            'Z',
            llkhfFlags: 0,
            shiftDown: false,
            capsLockOn: false,
            ctrlDown: false,
            altDown: true,
            winDown: false,
            foregroundHwnd: 10);

        Assert.True(handled);
        Assert.False(processor.CurrentConfig.VietnameseEnabled);
        Assert.False(tray.IsVietnameseActive);
        Assert.NotEqual(iconHandleV, tray.ActiveIconHandle);
        Assert.Contains("Tiếng Anh (E)", tray.GetTooltipText());
    }

    // @covers REQ-settings-1
    [Fact]
    public void CtrlShiftHotkeyAndEvKeyCoexistence_RespectsStartOrderPriorityAndInterveningKeys()
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
            _ => "chrome.exe",
            batch => dispatchedBatches.Add(batch),
            peerArbitrator: arbitrator);

        // 1. While older EVKey is running, VietKey yields Ctrl+Shift to EVKey
        processor.ProcessKeyDown(0xA2, 0, false, false, false, false, false, 10); // VK_LCONTROL down
        processor.ProcessKeyDown(0xA0, 0, false, false, true, false, false, 10);  // VK_LSHIFT down
        bool toggledWhileOlderEvKeyRunning = processor.ProcessKeyUp(0xA0, 0, false, true, false, false, 10);
        processor.ProcessKeyUp(0xA2, 0, false, false, false, false, 10);
        Assert.False(toggledWhileOlderEvKeyRunning);
        Assert.True(processor.CurrentConfig.VietnameseEnabled);

        // 2. Older EVKey closes -> VietKey immediately takes over Ctrl+Shift without restart
        activePeers.Clear();
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
    public void SystemTrayCallback_HandlesLeftClickToggleDoubleClickSettingsAndRightClickMenu()
    {
        using var tray = new VietKey.Engine.Tray.TrayIconHost(AppConfig.Default);
        bool toggleCalled = false;
        bool openSettingsCalled = false;
        bool contextMenuCalled = false;

        tray.ToggleRequested += () => toggleCalled = true;
        tray.OpenSettingsRequested += () => openSettingsCalled = true;
        tray.ContextMenuRequested += () => contextMenuCalled = true;

        tray.HandleMouseMessage(VietKey.Engine.Tray.TrayIconHost.WM_LBUTTONUP);
        Assert.True(toggleCalled);

        tray.HandleMouseMessage(VietKey.Engine.Tray.TrayIconHost.WM_LBUTTONDBLCLK);
        Assert.True(openSettingsCalled);

        tray.HandleMouseMessage(VietKey.Engine.Tray.TrayIconHost.WM_RBUTTONUP);
        Assert.True(contextMenuCalled);
    }
}
