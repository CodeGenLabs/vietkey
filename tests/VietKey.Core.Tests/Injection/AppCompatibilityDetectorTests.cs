using VietKey.Core.Injection;
using VietKey.Core.Typing;
using Xunit;

namespace VietKey.Core.Tests.Injection;

public sealed class AppCompatibilityDetectorTests
{
    // @covers REQ-os-2
    [Fact]
    public void ResolveForWindow_CachesHwndAndClassifiesChromiumElectronAndOffice()
    {
        int resolverCallCount = 0;
        var detector = new AppCompatibilityDetector(hwnd =>
        {
            resolverCallCount++;
            return hwnd switch
            {
                100 => @"C:\Program Files\Google\Chrome\Application\chrome.exe",
                200 => @"C:\Users\User\AppData\Local\Programs\Microsoft VS Code\Code.exe",
                300 => @"C:\Program Files\WindowsApps\MSTeams\ms-teams.exe",
                400 => @"C:\Program Files\Microsoft Office\root\Office16\WINWORD.EXE",
                _ => "notepad.exe"
            };
        });

        var firstChrome = detector.ResolveForWindow(100);
        var secondChrome = detector.ResolveForWindow(100);
        Assert.Equal("chrome.exe", firstChrome.ExecutableName);
        Assert.Equal(AppCompatibilityProfile.ChromiumBrowser, firstChrome.Profile);
        Assert.Equal(firstChrome, secondChrome);
        Assert.Equal(1, resolverCallCount); // Cached on second call for same HWND

        var vsCode = detector.ResolveForWindow(200);
        Assert.Equal("code.exe", vsCode.ExecutableName);
        Assert.Equal(AppCompatibilityProfile.ElectronApp, vsCode.Profile);

        var teams = detector.ResolveForWindow(300);
        Assert.Equal("ms-teams.exe", teams.ExecutableName);
        Assert.Equal(AppCompatibilityProfile.ElectronApp, teams.Profile);

        var word = detector.ResolveForWindow(400);
        Assert.Equal("winword.exe", word.ExecutableName);
        Assert.Equal(AppCompatibilityProfile.MicrosoftOffice, word.Profile);
    }

    // @covers REQ-os-2
    [Fact]
    public void BuildBatch_ChromiumPrependsAutocompleteFix_WhileVsCodeTeamsWordEmitDirectBatch()
    {
        var transform = new KeyTransformResult(
            Handled: true,
            BackspaceCount: 1,
            ReplacementText: "đ",
            ComposedWord: "đ");

        var chromeBatch = InputBatchBuilder.BuildBatch(
            transform,
            AppCompatibilityProfile.ChromiumBrowser,
            browserAutocompleteFix: true);

        // Dummy ZWNJ (down+up) + 2 VK_BACK (down+up each) + 'đ' (down+up) = 8 events
        Assert.Equal(8, chromeBatch.Length);
        Assert.Equal(InputBatchBuilder.ZeroWidthNonJoiner, chromeBatch[0].ScanCode);
        Assert.True(chromeBatch[0].IsUnicode);
        Assert.Equal(InputBatchBuilder.VkBack, chromeBatch[2].VirtualKey);
        Assert.Equal(InputBatchBuilder.VkBack, chromeBatch[4].VirtualKey);
        Assert.Equal('đ', (char)chromeBatch[6].ScanCode);

        foreach (var profile in new[]
        {
            AppCompatibilityProfile.ElectronApp,
            AppCompatibilityProfile.MicrosoftOffice,
            AppCompatibilityProfile.Standard
        })
        {
            var directBatch = InputBatchBuilder.BuildBatch(
                transform,
                profile,
                browserAutocompleteFix: true);

            // Exactly 1 VK_BACK (down+up) + 'đ' (down+up) = 4 events
            Assert.Equal(4, directBatch.Length);
            Assert.Equal(InputBatchBuilder.VkBack, directBatch[0].VirtualKey);
            Assert.Equal('đ', (char)directBatch[2].ScanCode);
        }
    }
}
