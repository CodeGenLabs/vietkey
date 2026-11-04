using VietKey.Core.Injection;
using Xunit;

namespace VietKey.Core.Tests.Injection;

public sealed class SmartSwitchTrackerTests
{
    // @covers REQ-os-3
    [Fact]
    public void OnForegroundChanged_RestoresRememberedLanguagePerExecutable_AndClearsBuffer()
    {
        var tracker = new SmartSwitchTracker();

        // 1. Focus Chrome (unknown yet -> defaults to current global Vietnamese = true)
        bool mode1 = tracker.OnForegroundChanged("chrome.exe", currentGlobalVietnameseEnabled: true, smartSwitchEnabled: true, out bool clear1);
        Assert.True(mode1);
        Assert.True(clear1);
        tracker.RecordUserToggle("chrome.exe", vietnameseEnabled: true);

        // 2. Focus VS Code (unknown yet -> defaults to current global true), then user toggles to English (false)
        bool mode2 = tracker.OnForegroundChanged("code.exe", currentGlobalVietnameseEnabled: mode1, smartSwitchEnabled: true, out bool clear2);
        Assert.True(mode2);
        Assert.True(clear2);
        tracker.RecordUserToggle("code.exe", vietnameseEnabled: false);

        // 3. Switch back to Chrome -> automatically restores Vietnamese (true)
        bool mode3 = tracker.OnForegroundChanged("chrome.exe", currentGlobalVietnameseEnabled: false, smartSwitchEnabled: true, out bool clear3);
        Assert.True(mode3);
        Assert.True(clear3);

        // 4. Switch back to VS Code -> automatically restores English (false)
        bool mode4 = tracker.OnForegroundChanged("code.exe", currentGlobalVietnameseEnabled: mode3, smartSwitchEnabled: true, out bool clear4);
        Assert.False(mode4);
        Assert.True(clear4);

        // 5. Staying on the same executable does not clear buffer
        bool mode5 = tracker.OnForegroundChanged("code.exe", currentGlobalVietnameseEnabled: mode4, smartSwitchEnabled: true, out bool clear5);
        Assert.False(mode5);
        Assert.False(clear5);
    }
}
