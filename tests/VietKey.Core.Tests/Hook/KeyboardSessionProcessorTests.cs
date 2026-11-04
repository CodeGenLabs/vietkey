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

        var tray = new VietKey.Engine.Tray.TrayIconHost(processor.CurrentConfig);
        processor.VietnameseModeChanged += enabled => tray.UpdateState(enabled, processor.CurrentConfig.InputMethod);

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
        Assert.Contains("Tiếng Anh (E)", tray.GetTooltipText());
    }

    // @covers REQ-settings-1
    [Fact]
    public void SystemTrayCallback_HandlesLeftClickToggleDoubleClickSettingsAndRightClickMenu()
    {
        var tray = new VietKey.Engine.Tray.TrayIconHost(AppConfig.Default);
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
