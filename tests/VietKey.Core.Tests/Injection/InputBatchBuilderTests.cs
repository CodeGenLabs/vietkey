using VietKey.Core.Injection;
using VietKey.Core.Typing;
using Xunit;

namespace VietKey.Core.Tests.Injection;

public sealed class InputBatchBuilderTests
{
    // @covers REQ-os-1
    [Fact]
    public void BuildBatch_ProducesAtomicBackspacesAndUnicodeKeyEvents_AndIgnoresInjectedEvents()
    {
        Assert.True(InputBatchBuilder.IsSyntheticInjected(InputBatchBuilder.LlkhfInjected));
        Assert.False(InputBatchBuilder.IsSyntheticInjected(0));

        var transform = new KeyTransformResult(
            Handled: true,
            BackspaceCount: 1,
            ReplacementText: "á",
            ComposedWord: "á");

        var batch = InputBatchBuilder.BuildBatch(
            transform,
            AppCompatibilityProfile.Standard,
            browserAutocompleteFix: false);

        // 1 backspace (down + up) + 1 Unicode char 'á' (down + up) = 4 packets in one batch
        Assert.Equal(4, batch.Length);
        Assert.Equal(new SimulatedKeyEvent(InputBatchBuilder.VkBack, 0, IsUnicode: false, IsKeyUp: false), batch[0]);
        Assert.Equal(new SimulatedKeyEvent(InputBatchBuilder.VkBack, 0, IsUnicode: false, IsKeyUp: true), batch[1]);
        Assert.Equal(new SimulatedKeyEvent(0, 'á', IsUnicode: true, IsKeyUp: false), batch[2]);
        Assert.Equal(new SimulatedKeyEvent(0, 'á', IsUnicode: true, IsKeyUp: true), batch[3]);
    }

    // @covers REQ-os-1
    [Theory]
    [InlineData(0x25, false, false, false, true)] // VK_LEFT
    [InlineData(0x26, false, false, false, true)] // VK_UP
    [InlineData(0x27, false, false, false, true)] // VK_RIGHT
    [InlineData(0x28, false, false, false, true)] // VK_DOWN
    [InlineData(0x24, false, false, false, true)] // VK_HOME
    [InlineData(0x23, false, false, false, true)] // VK_END
    [InlineData(0x21, false, false, false, true)] // VK_PRIOR (PageUp)
    [InlineData(0x22, false, false, false, true)] // VK_NEXT (PageDown)
    [InlineData(0x2E, false, false, false, true)] // VK_DELETE
    [InlineData(0x1B, false, false, false, true)] // VK_ESCAPE
    [InlineData(0x09, false, false, false, true)] // VK_TAB
    [InlineData(0x41, true, false, false, true)]  // Ctrl+A
    [InlineData(0x43, false, true, false, true)]  // Alt+C
    [InlineData(0x41, false, false, true, true)]  // Win+A
    [InlineData(0x41, false, false, false, false)] // Plain 'A'
    public void IsNavigationOrResetKey_DetectsNavigationAndModifierShortcuts(
        int vk, bool ctrl, bool alt, bool win, bool expectedReset)
    {
        bool actual = InputBatchBuilder.IsNavigationOrResetKey(vk, ctrl, alt, win);
        Assert.Equal(expectedReset, actual);
    }
}
