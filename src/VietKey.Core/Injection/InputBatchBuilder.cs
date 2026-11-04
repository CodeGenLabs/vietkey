using VietKey.Core.Typing;

namespace VietKey.Core.Injection;

public static class InputBatchBuilder
{
    public const ushort VkBack = 0x08;
    public const ushort ZeroWidthNonJoiner = 0x200C;
    public const uint LlkhfInjected = 0x00000010;

    public static bool IsSyntheticInjected(uint llkhfFlags) =>
        (llkhfFlags & LlkhfInjected) != 0;

    public static bool IsNavigationOrResetKey(int virtualKey, bool ctrlDown, bool altDown, bool winDown)
    {
        if (ctrlDown || altDown || winDown)
        {
            return true;
        }

        return virtualKey is
            0x09 or // VK_TAB
            0x1B or // VK_ESCAPE
            0x21 or // VK_PRIOR (PageUp)
            0x22 or // VK_NEXT (PageDown)
            0x23 or // VK_END
            0x24 or // VK_HOME
            0x25 or // VK_LEFT
            0x26 or // VK_UP
            0x27 or // VK_RIGHT
            0x28 or // VK_DOWN
            0x2D or // VK_INSERT
            0x2E or // VK_DELETE
            (>= 0x70 and <= 0x87); // F1-F24
    }

    public static SimulatedKeyEvent[] BuildBatch(
        KeyTransformResult transformResult,
        AppCompatibilityProfile appProfile,
        bool browserAutocompleteFix)
    {
        if (!transformResult.Handled)
        {
            return Array.Empty<SimulatedKeyEvent>();
        }

        bool prependBrowserFix = browserAutocompleteFix &&
            appProfile == AppCompatibilityProfile.ChromiumBrowser &&
            transformResult.BackspaceCount > 0;

        int totalBackspaces = transformResult.BackspaceCount + (prependBrowserFix ? 1 : 0);
        int totalEvents = (prependBrowserFix ? 2 : 0) + (totalBackspaces * 2) + (transformResult.ReplacementText.Length * 2);

        var events = new SimulatedKeyEvent[totalEvents];
        int idx = 0;

        if (prependBrowserFix)
        {
            events[idx++] = new SimulatedKeyEvent(0, ZeroWidthNonJoiner, IsUnicode: true, IsKeyUp: false);
            events[idx++] = new SimulatedKeyEvent(0, ZeroWidthNonJoiner, IsUnicode: true, IsKeyUp: true);
        }

        for (int i = 0; i < totalBackspaces; i++)
        {
            events[idx++] = new SimulatedKeyEvent(VkBack, 0, IsUnicode: false, IsKeyUp: false);
            events[idx++] = new SimulatedKeyEvent(VkBack, 0, IsUnicode: false, IsKeyUp: true);
        }

        foreach (char ch in transformResult.ReplacementText)
        {
            events[idx++] = new SimulatedKeyEvent(0, ch, IsUnicode: true, IsKeyUp: false);
            events[idx++] = new SimulatedKeyEvent(0, ch, IsUnicode: true, IsKeyUp: true);
        }

        return events;
    }
}
